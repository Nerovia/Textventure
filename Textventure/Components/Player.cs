using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Textventure.Resources;

namespace Textventure.Components
{
    [XmlType("Player")]
    public class Player : Component, ITaggable
    {
        #region Serialization


        [XmlIgnore]
        public string Key { get; } = "Player";

        [XmlAttribute("Tags")]
        public string TagsExpression
        {
            get => string.Join(", ", Tags);
            set => Tags = Tag.ParseCollection(value);
        }

        [XmlAttribute("Inventory")]
        public string InventoryExpression
        {
            get
            {
                if (Inventory != null)
                    return string.Join(", ", Inventory);
                return "";
            }
            set
            {
                if (value != null)
                    Inventory = new SortedSet<string>(value.SplitTrimmed(','));
            }
        }


        public override void Init(World world, IComponent parent)
        {
            base.Init(world, parent);
        }

        public override void UnInit()
        {
            base.UnInit();
        }


        #endregion

        #region Inventory


        [XmlIgnore]
        public SortedSet<string> Inventory { get; set; } = new SortedSet<string>();

        public Item[] GetAvailableItems() => World.GetReferences<Item>(Inventory).Where(x => x.IsAvailable()).ToArray();

        public bool HasItem(string key)
        {
            return (Inventory.Contains(key));
        }

        public void SetItem(string key, Manipulation manipulation)
        {
            switch (manipulation)
            {
                case Manipulation.Add:
                    AddItem(key);
                    break;
                case Manipulation.Remove:
                    RemoveItem(key);
                    break;
                case Manipulation.Invert:
                    if (HasItem(key))
                        RemoveItem(key);
                    else
                        AddItem(key);
                    break;
            }
        }

        public void AddItem(string key)
        {
            if (Inventory.Add(key))
            {
                var args = new InventoryEventArgs(key, true);
                OnItemAdded(args);
                OnItemChanged(args);
            }
        }

        public void RemoveItem(string key)
        {
            if (Inventory.Remove(key))
            {
                var args = new InventoryEventArgs(key, false);
                OnItemRemoved(args);
                OnItemChanged(args);
            }
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

        #region Events


        public event InventoryEventHandler ItemAdded;
        protected virtual void OnItemAdded(InventoryEventArgs args) => ItemAdded?.Invoke(this, args);
        public event InventoryEventHandler ItemRemoved;
        protected virtual void OnItemRemoved(InventoryEventArgs args) => ItemRemoved?.Invoke(this, args);
        public event InventoryEventHandler ItemChanged;
        protected virtual void OnItemChanged(InventoryEventArgs args) => ItemChanged?.Invoke(this, args);

        public event TagEventHandler TagAdded;
        protected virtual void OnTagAdded(TagEventArgs args) => TagAdded?.Invoke(this, args);
        public event TagEventHandler TagRemoved;
        protected virtual void OnTagRemoved(TagEventArgs args) => TagRemoved?.Invoke(this, args);
        public event TagEventHandler TagChanged;
        protected virtual void OnTagChanged(TagEventArgs args) => TagChanged?.Invoke(this, args);

        public event ImpactEventEventHandler ImpactEvent;
        internal void _OnImpactEvent(ImpactEventEventArgs args) => OnImpactEvent(args);
        protected virtual void OnImpactEvent(ImpactEventEventArgs args) => ImpactEvent?.Invoke(this, args);


        #endregion
    }
}
