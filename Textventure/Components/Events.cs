using System;
using System.Collections.Generic;
using System.Text;

namespace Textventure.Components
{
    public delegate void FocusEventHandler(object sender, FocusEventArgs args);

    public class FocusEventArgs : EventArgs
    {
        public FocusEventArgs (Scene from, Scene to)
        {
            From = from;
            To = to;
        }

        public Scene From { get; }
        public Scene To { get; }
    }

    public delegate void ImpactEventEventHandler(object sender, ImpactEventEventArgs args);

    public class ImpactEventEventArgs : EventArgs
    {
        public ImpactEventEventArgs(Scene source, string argument)
        {
            Source = source;
            Argument = argument;
        }

        public Scene Source { get; }
        public string Argument { get; }
    }

    public delegate void TagEventHandler(ITaggable sender, TagEventArgs args);

    public class TagEventArgs : EventArgs
    {
        public TagEventArgs(string tag, bool state)
        {
            Tag = tag;
            State = state;
        }

        public bool State { get; }

        public string Tag { get; }
    }

    public delegate void InventoryEventHandler(Player player, InventoryEventArgs args);

    public class InventoryEventArgs
    {
        public InventoryEventArgs(string key, bool state)
        {
            Key = key;
            State = state;
        }

        public string Key { get; }

        public bool State { get; }
    }
}
