using System;
using System.Collections.Generic;
using System.Text;

namespace Textventure.Components
{
    public delegate void FocusEventHandler(object sender, FocusEventArgs args);

    public class FocusEventArgs : EventArgs
    {
        public FocusEventArgs (Element from, Element to)
        {
            From = from;
            To = to;
        }

        public Element From { get; }
        public Element To { get; }
    }

    public delegate void ImpactEventEventHandler(object sender, ImpactEventEventArgs args);

    public class ImpactEventEventArgs : EventArgs
    {
        public ImpactEventEventArgs(Element source, string argument)
        {
            Source = source;
            Argument = argument;
        }

        public Element Source { get; }
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
        public InventoryEventArgs(Element item, bool state)
        {
            Item = item;
            State = state;
        }

        public Element Item { get; }

        public bool State { get; }
    }
}
