using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// A base class for all describable object that defines most core features. 
    /// </summary>
    public abstract class Describable
    {
        public Describable(World world, Element parent, DescribableModel model)
        {
            World = world;
            Parent = parent;
            Description = model.Description;
            EvaluationLimit = model.Limit;
            Requirement = Requirement.Parse(model.Requires);
            Impacts = Impact.ParseCollection(model.Impact);
            Descriptors = model.Descriptors.Convert(x => new Descriptor(Source, x)).ToArray();
        }

        public World World { get; }
          
        public Element Parent { get; }

        public virtual Element Source { get => Parent; } 

        public Requirement Requirement { get; }

        public Impact[] Impacts { get; }

        public Descriptor[] Descriptors { get; }

        public string Description { get; }

        public int EvaluationLimit { get; }

        public int EvaluationCount { get; set; }

        /// <summary>
        /// Evaluates the description's text script instructions that correspond to this object and returns the formated copy.
        /// If <see cref="Description"/> is <see cref="null"/> this methode returns an empty string.
        /// </summary>
        string GetFormatedDescription()
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
            World.Execute(Source, impacts);
            return description;
        }

        private bool Accumulate(bool count, ref string description, ref List<Impact> impacts)
        {
            if (!IsAvailable())
                return false;

            bool previousResult = false;
            description += GetFormatedDescription();

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
            return World.EvaluateReference(Source, reference);
        }
    }

    /// <summary>
    /// A class to structure and script an objects description, providing requirements and sequential correlation. 
    /// </summary>
    public class Descriptor : Describable
    {
        public Descriptor(Element parent, DescriptorModel model) : base(parent.World, parent, model)
        {
            Correlation = model.Correlation;
        }

        public Correlation Correlation { get; }
    }  

    /// <summary>
    /// A base class for map elements. Containing interactions and subitems, as well as implementing <see cref="ITaggable"/>.
    /// </summary>
    public class Element : Describable, IOption, ITaggable
    {
        public Element(World world, Element parent, ElementModel model) : base(world, parent, model)
        {
            Name = model.Name;
            Key = model.Key;
            Tags = Tag.ParseCollection(model.Tags);
            Items = model.Items.Convert(x => new Element(Source, x)).ToArray();
            Interactions = model.Interactions.Convert(x => new Interaction(Source, x)).ToArray();
            world.AddReference(this);
        }

        public Element(Element parent, ElementModel model) : this(parent.World, parent, model)
        {

        }

        public Element(World world, ElementModel model) : this(world, null, model)
        {

        }

        public override Element Source { get => this; }

        public string Name { get; }

        public string Key { get; }

        public Element[] Items { get; }

        public Interaction[] Interactions { get; }

        public Interaction Exit { get; }

        public override string ToString() => Name;

        /// <summary>
        /// Evaluates whether the specified requirement is fulfilled and the evaluation count is within limits. If the element has no parent it is automatically available.
        /// </summary>
        public override bool IsAvailable()
        {
            if (Parent == null)
                return true;
            return base.IsAvailable();
        }

        /// <summary>
        /// Evaluates which items are available for the world's current state and returns them as a collection.
        /// </summary>
        public Element[] GetAvailableItems()
        {
            return Items.Where(x => x.IsAvailable()).ToArray();
        }

        /// <summary>
        /// Evaluates which interactions are available for the world's current state and returns them as a collection.
        /// </summary>
        public Interaction[] GetAvailableInteractions()
        {
            return Interactions.Where(x => x.IsAvailable()).ToArray();
        }

        public SortedSet<string> Tags { get; }

        public bool HasTag(string tag)
        {
            return Tags.Contains(tag);
        }

        public void SetTag(string tag, Manipulation manipulation)
        {
            switch (manipulation)
            {
                case Manipulation.Add:
                    AddTag(tag);
                    break;
                case Manipulation.Remove:
                    RemoveTag(tag);
                    break;
                case Manipulation.Invert:
                    if (HasTag(tag))
                        RemoveTag(tag);
                    else
                        AddTag(tag);
                    break;
            }
        }

        public void AddTag(string tag)
        {
            if (Tags.Add(tag))
            {
                var args = new TagEventArgs(tag, true);
                OnTagAdded(args);
                OnTagChanged(args);
            }
        }

        public void RemoveTag(string tag)
        {
            if (Tags.Remove(tag))
            {
                var args = new TagEventArgs(tag, false);
                OnTagRemoved(args);
                OnTagChanged(args);
            }
        }


        public event FocusEventHandler GotFocus;
        internal void _OnGotFocus(FocusEventArgs args) => OnGotFocus(args);
        protected virtual void OnGotFocus(FocusEventArgs args) => GotFocus?.Invoke(this, args);

        public event FocusEventHandler LostFocus;
        internal void _OnLostFocus(FocusEventArgs args) => OnLostFocus(args);
        protected virtual void OnLostFocus(FocusEventArgs args) => LostFocus?.Invoke(this, args);

        public event ImpactEventEventHandler ImpactEvent;

        internal void _OnImpactEvent(ImpactEventEventArgs args) => OnImpactEvent(args);
        protected virtual void OnImpactEvent(ImpactEventEventArgs args) => ImpactEvent?.Invoke(this, args);

        public event TagEventHandler TagAdded;
        protected virtual void OnTagAdded(TagEventArgs args) => TagAdded?.Invoke(this, args);
        public event TagEventHandler TagRemoved;
        protected virtual void OnTagRemoved(TagEventArgs args) => TagRemoved?.Invoke(this, args);
        public event TagEventHandler TagChanged;
        protected virtual void OnTagChanged(TagEventArgs args) => TagChanged?.Invoke(this, args);
    }

    /// <summary>
    /// A class that represents an interaction the player can execute.
    /// </summary>
    public class Interaction : Describable, IOption
    {
        public Interaction(Element parent, InteractionModel model) : base(parent.World, parent, model)
        {
            Name = model.Name;
        }

        public string Name { get; }

        public override string ToString() => Name;
    }

    /// <summary>
    /// A class that represents the player. Providing an inventory system and implementing <see cref="ITaggable"/>.
    /// </summary>
    public class Player : ITaggable
    {
        public Player(World world, PlayerModel model)
        {
            World = world;
            Key = "Player";
            Tags = Tag.ParseCollection(model.Tags);
            Inventory = RetreiveInventory(model.Inventory);
        }

        public World World { get; }

        public string Key { get; }

        public Dictionary<string, Element> Inventory { get; }

        /// <summary>
        /// Evaluates if the player's inventory contains the item with the specified key.
        /// </summary>
        public bool HasItem(string key)
        {
            return (Inventory.ContainsKey(key));
        }

        public bool HasItem(Element item)
        {
            return HasItem(item.Key);
        }

        /// <summary>
        /// Add or removes the provided item from the inventory, corresponding to the given state. 
        /// </summary>
        public void SetItem(string key, Manipulation manipulation)
        {
            SetItem(World.GetReference<Element>(key), manipulation);
        }

        public void SetItem(Element item, Manipulation manipulation)
        {
            switch (manipulation)
            {
                case Manipulation.Add:
                    AddItem(item);
                    break;
                case Manipulation.Remove:
                    RemoveItem(item);
                    break;
                case Manipulation.Invert:
                    if (HasItem(item))
                        RemoveItem(item);
                    else
                        AddItem(item);
                    break;
            }
        }

        public void AddItem(string key)
        {
            AddItem(World.GetReference<Element>(key));
        }

        public void AddItem(Element item)
        {
            if (HasItem(item.Key))
                return;
            Inventory.Add(item.Key, item);
            var args = new InventoryEventArgs(item, true);
            OnItemAdded(args);
            OnItemChanged(args);
        }

        public void RemoveItem(string key)
        {
            RemoveItem(World.GetReference<Element>(key));
        }

        public void RemoveItem(Element item)
        {
            if (Inventory.Remove(item.Key))
            {
                var args = new InventoryEventArgs(item, false);
                OnItemRemoved(args);
                OnItemChanged(args);
            }
        }

        public event InventoryEventHandler ItemAdded;
        protected virtual void OnItemAdded(InventoryEventArgs args) => ItemAdded?.Invoke(this, args);
        public event InventoryEventHandler ItemRemoved;
        protected virtual void OnItemRemoved(InventoryEventArgs args) => ItemRemoved?.Invoke(this, args);
        public event InventoryEventHandler ItemChanged;
        protected virtual void OnItemChanged(InventoryEventArgs args) => ItemChanged?.Invoke(this, args);


        /// <summary>
        /// Evaluates which items in the inventory are available for the world's current state and returns them as a collection.
        /// </summary>
        public Element[] GetInvenotry()
        {
            return Inventory.Values.Where(x => x.IsAvailable()).ToArray();
        }

        /// <summary>
        /// A helper methode to retreive the model's referenced items from the world.
        /// </summary>
        private Dictionary<string, Element> RetreiveInventory(string expression)
        {
            var keys = Tag.ParseCollection(expression);
            var inventory = new Dictionary<string, Element>();
            foreach (var key in keys)
            {
                var item = World.GetReference<Element>(key);
                if (item is null)
                    continue;
                inventory.Add(key, item);
            }
            return inventory;
        }

        public SortedSet<string> Tags { get; }

        public bool HasTag(string tag)
        {
            return Tags.Contains(tag);
        }

        public void SetTag(string tag, Manipulation manipulation)
        {
            switch (manipulation)
            {
                case Manipulation.Add:
                    AddTag(tag);
                    break;
                case Manipulation.Remove:
                    RemoveTag(tag);
                    break;
                case Manipulation.Invert:
                    if (HasTag(tag))
                        RemoveTag(tag);
                    else
                        AddTag(tag);
                    break;
            }
        }

        public void AddTag(string tag)
        {
            if (Tags.Add(tag))
            {
                var args = new TagEventArgs(tag, true);
                OnTagAdded(args);
                OnTagChanged(args);
            }
        }

        public void RemoveTag(string tag)
        {
            if (Tags.Remove(tag))
            {
                var args = new TagEventArgs(tag, false);
                OnTagRemoved(args);
                OnTagChanged(args);
            }
        }

        public event TagEventHandler TagAdded;
        protected virtual void OnTagAdded(TagEventArgs args) => TagAdded?.Invoke(this, args);
        public event TagEventHandler TagRemoved;
        protected virtual void OnTagRemoved(TagEventArgs args) => TagRemoved?.Invoke(this, args);
        public event TagEventHandler TagChanged;
        protected virtual void OnTagChanged(TagEventArgs args) => TagChanged?.Invoke(this, args);

        public event ImpactEventEventHandler ImpactEvent;
        internal void _OnImpactEvent(ImpactEventEventArgs args) => OnImpactEvent(args);
        protected virtual void OnImpactEvent(ImpactEventEventArgs args) => ImpactEvent?.Invoke(this, args);
    }

    /// <summary>
    /// A class that represents the game's world. Holds all objects and provides functionality to manipulate the world's state.
    /// </summary>
    public class World
    {
        private World(WorldModel model)
        {
            if (string.IsNullOrEmpty(model.Start))
                throw new Exception("World must define the attribute 'Start' to locate the initial element.");

            Rooms = model.Rooms.Convert(x => new Element(this, x)).ToArray();
            Items = model.Items.Convert(x => new Element(this, x)).ToArray();
            Player = new Player(this, model.Player);
            Focus = GetReference<Element>(model.Start);
            if (Focus == null)
                throw new Exception($"Unable to locate start element with reference key '{model.Start}'");   
        }

        public static World Load(FileStream stream)
        {
            try
            {
                return new World(WorldModel.Deserialize(stream));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public Element Focus
        { 
            get => _focus; 
            set
            {
                if (value != _focus)
                {
                    var args = new FocusEventArgs(_focus, value);
                    _focus?._OnLostFocus(args);
                    _focus = value;
                    OnFocusChanged(args);
                    _focus?._OnGotFocus(args);
                }
            }
        }
        private Element _focus;

        public Player Player { get; }

        public Dictionary<string, ITaggable> References { get; } = new Dictionary<string, ITaggable>();

        public Element[] Rooms { get; }

        public Element[] Items { get; }






        public void AddReference(ITaggable taggable)
        {
            try
            {
                if (!string.IsNullOrEmpty(taggable.Key))
                    References.Add(taggable.Key, taggable);
            }
            catch
            {
                throw new Exception($"Element with Key \"{taggable.Key}\" is ambiguous with another element.");
            }
        }

        public ITaggable GetReference(string key)
        {
            ITaggable reference;
            if (!string.IsNullOrEmpty(key))
                if (References.TryGetValue(key, out reference))
                    return reference;
            return default;
            //throw new Exception($"Unable to find reference with key '{key}'.");
        }

        public T GetReference<T>(string key) where T : ITaggable
        {
            ITaggable reference = GetReference(key);
            if (reference != null)
                if (typeof(T).IsAssignableFrom(reference.GetType()))
                    return (T)reference;
            return default;
        }



        public void Execute(Element source, IEnumerable<Impact> impacts)
        {
            foreach (var impact in impacts)
                Execute(source, impact);
        }

        public void Execute(Element source, Impact impact)
        {
            if (impact is ManipulateImpact)
                Execute(source, impact as ManipulateImpact);
            else if (impact is FocusImpact)
                Execute(source, impact as FocusImpact);
            else if (impact is EventImpact)
                Execute(source, impact as EventImpact);
        }

        private void Execute(Element source, ManipulateImpact impact)
        {
            var reference = impact.Reference;

            if (reference.IsInventory)
                Player.SetItem(reference.Property, impact.Manipulation);
            else if (reference.IsPlayer)
                Player.SetTag(reference.Property, impact.Manipulation);
            else if (reference.IsLocal)
                source.SetTag(reference.Property, impact.Manipulation);
            else
                GetReference(reference.Element).SetTag(reference.Property, impact.Manipulation);
                
        }

        private void Execute(Element source, FocusImpact impact)
        {
            Focus = GetReference<Element>(impact.Element);
        }

        private void Execute(Element source, EventImpact impact)
        {
            var reference = impact.Reference;
            var args = new ImpactEventEventArgs(source, reference.Property);
            if (reference.IsInventory)
                throw new Exception("Cannot delegate impact event to inventory reference.");
            else if (reference.IsPlayer)
                Player._OnImpactEvent(args);
            else if (reference.IsLocal)
                source._OnImpactEvent(args);
            else
                GetReference<Element>(reference.Element)._OnImpactEvent(args);
        }




        public bool EvaluateReference(Element source, Reference reference)
        {
            if (reference.IsInventory)
                return Player.HasItem(reference.Property);
            else if (reference.IsPlayer)
                return Player.HasTag(reference.Property);
            else if (reference.IsLocal)
                return source.HasTag(reference.Property);
            else
                return GetReference(reference.Element).HasTag(reference.Property);
        }


        protected virtual void OnFocusChanged(FocusEventArgs args) => FocusChanged?.Invoke(this, args);
        public event FocusEventHandler FocusChanged;
    }
}
     