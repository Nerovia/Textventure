using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Textventure.Components
{
    public abstract class DescribableModel
    {
        [XmlAttribute]
        public virtual string Requires { get; set; }

        [XmlAttribute]
        public string Description { get; set; }

        [XmlAttribute]
        public string Impact { get; set; }

        [XmlElement("Descriptor")]
        public DescriptorModel[] Descriptors { get; set; }

        [XmlAttribute]
        public virtual int Limit { get; set; }
    }


    [XmlType("Descriptor")]
    public class DescriptorModel : DescribableModel
    {
        [XmlAttribute]
        public Correlation Correlation { get; set; }
    }

    public class ElementModel : DescribableModel
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string Tags { get; set; }

        [XmlAttribute]
        public bool DisableExit { get; set; } = false;

        [XmlElement("Exit")]
        public InteractionModel Exit { get; set; }

        [XmlElement("Item")]
        public ElementModel[] Items { get; set; }

        [XmlElement("Interaction")]
        public InteractionModel[] Interactions { get; set; }
    }

    [XmlType("Interaction")]
    public class InteractionModel : DescribableModel
    {
        [XmlAttribute]
        public string Name { get; set; }
    }

    [XmlType("Player")]
    public class PlayerModel
    {
        [XmlAttribute]
        public string Inventory { get; set; }

        [XmlAttribute]
        public string Tags { get; set; }
    }

    [XmlType("World")]
    public class WorldModel
    {
        [XmlAttribute]
        public string Start { get; set; }

        [XmlElement("Room")]
        public ElementModel[] Rooms { get; set; }

        [XmlElement("Item")]
        public ElementModel[] Items { get; set; }

        [XmlElement("Player")]
        public PlayerModel Player { get; set; } = new PlayerModel(); 

        public static WorldModel Deserialize(FileStream stream)
        {
            var serializer = new XmlSerializer(typeof(WorldModel));
            return serializer.Deserialize(stream) as WorldModel;
        }
    }
}
