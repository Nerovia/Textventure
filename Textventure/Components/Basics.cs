using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Textventure.Resources;

namespace Textventure.Components
{
    /// <summary>
    /// Defines the sequential correlation of a descripor to its predecessor.
    /// </summary>
    public enum Correlation
    {
        None,
        Else,
    }

    /// <summary>
    /// Provides an interface for referencable game objects, that can be manipulated with tags.
    /// </summary>
    public interface ITaggable
    {
        string Key { get; }

        SortedSet<string> Tags { get; }

        /// <summary>
        /// Adds or removes the provided tag from this object, corresponding to the given state.
        /// </summary>
        void SetTag(string tag, Manipulation manipulation);

        void AddTag(string tag);

        void RemoveTag(string tag);

        /// <summary>
        /// Evaluates if this object contains the provided tag.
        /// </summary>
        bool HasTag(string tag);

        event TagEventHandler TagAdded;
        event TagEventHandler TagRemoved;
        event TagEventHandler TagChanged;
    }

    /// <summary>
    /// Provides an interface for all game objects, that can be selected as an option by the player.
    /// </summary>
    public interface IOption
    {
        string Name { get; }

        bool IsAvailable();

        string Evaluate(bool count = true);
    }

    public interface ICombinable
    {
        string Key { get; }

        Combination GetAvailableCombinationWith(ICombinable combinable, bool checkTwoWay = true);
    }

    public interface IComponent
    {
        World World { get; }
        IComponent Parent { get; }
        IComponent Source { get; }

        void Init(World world, IComponent parent);
        void UnInit();
    }

    public abstract class Component : IComponent
    {
        [XmlIgnore]
        public World World { get; protected set; }

        [XmlIgnore]
        public IComponent Parent { get; protected set; }

        [XmlIgnore]
        public virtual IComponent Source { get => Parent; }

        protected bool isInitialized = false;

        public virtual void Init(World world, IComponent parent)
        {
            if (isInitialized)
                throw new Exception("Component is already initialized");
            isInitialized = true;
            World = world;
            Parent = parent;
        }

        public virtual void UnInit()
        {
            isInitialized = false;
            World = null;
            Parent = null;
        }

        protected void Set<T>(T value, ref T field) where T : IComponent
        {
            if (!isInitialized)
            {
                field = value;
            }
            else if ((IComponent)field != (IComponent)value)
            {
                field?.UnInit();
                field = value;
                field?.Init(World, Source);
            }
        }
    }

    public class ComponentCollection<T> : ObservableCollection<T>, IComponent where T : IComponent
    {
        [XmlIgnore]
        public World World { get; protected set; }

        [XmlIgnore]
        public IComponent Parent { get; protected set; }

        [XmlIgnore]
        public IComponent Source { get => Parent; }

        protected bool isInitialized = false;

        public void Init(World world, IComponent parent)
        {
            if (isInitialized)
                throw new Exception("Component is already initialized");
            isInitialized = true;
            World = world;
            Parent = parent;
            InitComponents(Items);
        }

        public void UnInit()
        {
            isInitialized = false;
            World = null;
            Parent = null;
            UnInitComponents(Items);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            if (!isInitialized)
                return;
            if (e.OldItems != null)
                UnInitComponents(e.OldItems.Cast<T>());
            if (e.NewItems != null)
                InitComponents(e.NewItems.Cast<T>());
        }

        protected virtual void InitComponents(IEnumerable<T> components)
        {
            components.ForEach(x => x.Init(World, Source));
        }

        protected virtual void UnInitComponents(IEnumerable<T> components)
        {
            components.ForEach(x => x.UnInit());
        }
    }


    /// <summary>
    /// A base class for all describable object that defines most core features. 
    /// </summary>
    public abstract class Describable : Component
    {
        #region Serializable Properties


        [XmlAttribute("Requires")]
        public string RequriementExpression
        {
            get => Requirement.ToString();
            set => Requirement = new Requirement(value);
        }

        [XmlAttribute("Impact")]
        public string ImpactsExpression
        {
            get => string.Join(", ", Impacts);
            set => Impacts = Impact.ParseCollection(value);
        }

        [XmlAttribute("Description")]
        public string Description { get; set; }

        [XmlAttribute("Evalutations")]
        public int EvaluationCount { get; set; }

        [XmlAttribute("Limit")]
        public int EvaluationLimit { get; set; }

        [XmlElement("Descriptor")]
        public ComponentCollection<Descriptor> Descriptors
        { 
            get => _descriptor;
            set => Set(value, ref _descriptor);
        }
        private ComponentCollection<Descriptor> _descriptor;


        public override void Init(World world, IComponent parent)
        {
            base.Init(world, parent);
            Descriptors.Init(World, Source);
        }

        public override void UnInit()
        {
            base.UnInit();
            Descriptors.UnInit();
        }

        #endregion


        [XmlIgnore]
        public Requirement Requirement { get; set; } = null;

        [XmlIgnore]
        public List<Impact> Impacts { get; set; } = null;



        /// <summary>
        /// Evaluates the description's text script instructions that correspond to this object and returns the formated copy.
        /// If <see cref="Description"/> is <see cref="null"/> this methode returns an empty string.
        /// </summary>
        string FormatDescription()
        {
            if (Description != null)
                return $"[p]{{{EvaluationCount}}}{Description}";
            return "";
        }

        /// <summary>
        /// Evaluates whether the specified requirement is fulfilled and the evaluation count is within limits.
        /// </summary>
        public virtual bool IsAvailable()
        {
            if (EvaluationLimit > 0 && EvaluationCount >= EvaluationLimit)
                return false;
            else if (Requirement == null)
                return true;
            else
                return Requirement.Evaluate(EvaluateReference);
        }

        /// <summary>
        /// Evaluates this object's full description for the world's current state and executes the corresponding impacts. 
        /// </summary>
        /// <param name="count">Specifies whether the evaluation counter should be incremented.</param>
        public string Evaluate(bool count = true)
        {
            var description = "";
            var impacts = new List<Impact>();
            Accumulate(count, ref description, ref impacts);
            World.Execute(Parent, impacts);
            return description;
        }

        private bool Accumulate(bool count, ref string description, ref List<Impact> impacts)
        {
            if (!IsAvailable())
                return false;

            bool previousResult = false;
            description += FormatDescription();

            if (count)
                EvaluationCount++;

            if (Impacts != null)
                foreach (var impact in Impacts)
                    impacts.Add(impact);

            foreach (var descriptor in Descriptors)
            {
                 if (descriptor.Correlation == Correlation.Else && previousResult)
                    continue;
                else
                    previousResult = false;

                previousResult = descriptor.Accumulate(count, ref description, ref impacts);
            }

            return true;
        }

        /// <summary>  
        /// Evaluates the state of the provided reference, by calling the world's reference evaluator with this object's parent as its source.
        /// </summary>
        protected bool EvaluateReference(Reference reference)
        {
            return World.EvaluateReference(Source as ITaggable, reference);
        }
    }

    /// <summary>
    /// A class to structure and script an objects description, providing requirements and sequential correlation. 
    /// </summary>
    [XmlType("Descriptor")]
    public class Descriptor : Describable
    {
        [XmlAttribute("Correlation")]
        public Correlation Correlation { get; set; }
    }  

    /// <summary>
    /// A class that represents an interaction the player can execute.
    /// </summary>
    [XmlType("Interaction")]
    public class Interaction : Describable, IOption
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        public override string ToString() => Name;
    }

    [XmlType("Combination")]
    public class Combination : Describable
    {
        [XmlAttribute("ItemKey")]
        public string ItemKey { get; set; }
    }

    
}
     