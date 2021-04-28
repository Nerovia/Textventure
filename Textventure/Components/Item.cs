using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Textventure.Components
{
    [XmlType("Item")]
    public class Item : Describable, IOption, ITaggable, ICombinable
    {
        #region Serialization


        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Key")]
        public string Key
        {
            get => _key;
            set
            {
                if (!isInitialized)
                {
                    _key = value;
                }
                else if (_key != value)
                {
                    World.RemoveReference(this);
                    _key = Key;
                    World.AddReference(this);
                }
            }
        }
        private string _key;

        [XmlAttribute("Tags")]
        public string TagsExpression
        {
            get => string.Join(",", Tags);
            set => Tags = Tag.ParseCollection(value);
        }

        [XmlElement("Combination")]
        public ComponentCollection<Combination> Combinations
        {
            get => _combinations;
            set => Set(value, ref _combinations);
        }
        private ComponentCollection<Combination> _combinations;

        [XmlIgnore]
        public override IComponent Source { get => this; }

        public override void Init(World world, IComponent parent)
        {
            base.Init(world, parent);
            Combinations.Init(World, Source);
            World.AddReference(this);
        }

        public override void UnInit()
        {
            base.UnInit();
            World?.RemoveReference(this);
        }


        #endregion

        #region Tags


        [XmlIgnore]
        public SortedSet<string> Tags { get; set; } = new SortedSet<string>();


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


        #endregion


        public Combination GetAvailableCombinationWith(ICombinable combinable, bool checkTwoWay = true)
        {
            Combination combination = null;
            combination = Combinations?.FirstOrDefault(x => x.ItemKey == combinable.Key && x.IsAvailable());
            if (combination != null)
                return combination;
            else if (checkTwoWay)
                combination = combinable.GetAvailableCombinationWith(this, false);
            return combination;
        }

        #region Events


        public event TagEventHandler TagAdded;
        protected virtual void OnTagAdded(TagEventArgs args) => TagAdded?.Invoke(this, args);
        public event TagEventHandler TagRemoved;
        protected virtual void OnTagRemoved(TagEventArgs args) => TagRemoved?.Invoke(this, args);
        public event TagEventHandler TagChanged;
        protected virtual void OnTagChanged(TagEventArgs args) => TagChanged?.Invoke(this, args);


        #endregion
    }
}
