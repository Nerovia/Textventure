using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Textventure.Components
{
    [XmlType("World")]
    public class World
    {   
        public World()
        {
            Player = new Player();
        }



        [XmlAttribute("Start")]
        public string StartKey { get; set; }
        
        [XmlElement("Player")]
        public Player Player
        {
            get => _player;
            set => Set(value, ref _player);
        }
        private Player _player;
        
        [XmlElement("Scene")]
        public ComponentCollection<Scene> Scenes
        {
            get => _scenes;
            set => Set(value, ref _scenes);
        }
        private ComponentCollection<Scene> _scenes;

        [XmlElement("Item")]
        public ComponentCollection<Item> Items 
        {
            get => _items;
            set => Set(value, ref _items);
        }
        private ComponentCollection<Item> _items;


        public static World Deserialize(FileStream stream)
        {
            var serializer = new XmlSerializer(typeof(World));
            return serializer.Deserialize(stream) as World;
        }

        public void Serialize(FileStream stream)
        {
            var serializer = new XmlSerializer(typeof(World));
            serializer.Serialize(stream, this);
        }



        [XmlIgnore]
        public Scene Focus
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
        private Scene _focus;





        private Dictionary<string, ITaggable> References { get; } = new Dictionary<string, ITaggable>();



        private void Set<T>(T value, ref T field) where T : IComponent
        {
            if ((IComponent)field != (IComponent)value)
            {
                field?.UnInit();
                field = value;
                field?.Init(this, null);
            }
        }




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

        public T[] GetReferences<T>(IEnumerable<string> keys) where T : ITaggable
        {
            List<T> references = new List<T>();
            foreach (var key in keys)
            {
                var reference = GetReference<T>(key);
                if (reference != null)
                    references.Add(reference);
            }
            return references.ToArray();
        }


        public void Execute(IComponent source, IEnumerable<Impact> impacts)
        {
            foreach (var impact in impacts)
                Execute(source, impact);
        }

        public void Execute(IComponent source, Impact impact)
        {
            if (impact is ManipulateImpact)
                ExecuteManipulation(source, impact as ManipulateImpact);
            else if (impact is FocusImpact)
                ExecuteFocus(source, impact as FocusImpact);
            else if (impact is EventImpact)
                ExecuteEvent(source, impact as EventImpact);
        }

        private void ExecuteManipulation(IComponent source, ManipulateImpact impact)
        {
            var reference = impact.Reference;

            if (reference.IsInventory)
                Player.SetItem(reference.Property, impact.Manipulation);
            else if (reference.IsPlayer)
                Player.SetTag(reference.Property, impact.Manipulation);
            else if (reference.IsLocal)
                (source as ITaggable).SetTag(reference.Property, impact.Manipulation);
            else
                GetReference(reference.Element).SetTag(reference.Property, impact.Manipulation);

        }

        private void ExecuteFocus(IComponent source, FocusImpact impact)
        {
            Focus = GetReference<Scene>(impact.Element);
        }

        private void ExecuteEvent(IComponent source, EventImpact impact)
        {
            var reference = impact.Reference;
            var args = new ImpactEventEventArgs(source as Scene, reference.Property);
            if (reference.IsInventory)
                throw new Exception("Cannot delegate impact event to inventory reference.");
            else if (reference.IsPlayer)
                Player._OnImpactEvent(args);
            else if (reference.IsLocal)
                (source as Scene)._OnImpactEvent(args);
            else
                GetReference<Scene>(reference.Element)._OnImpactEvent(args);
        }




        public bool EvaluateReference(ITaggable source, Reference reference)
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

        internal void RemoveReference(ITaggable taggable)
        {
            if (!string.IsNullOrEmpty(taggable.Key))
                References.Remove(taggable.Key);
        }

        public event FocusEventHandler FocusChanged;
    }
}
