﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BOT_Platform
{

    namespace FileManager
    {
        [XmlRoot("dictionary")]
        public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
        {
            public XmlSchema GetSchema()
            {
                return null;
            }
            public void ReadXml(XmlReader reader)
            {
                var keySerializer = new XmlSerializer(typeof(TKey));
                var valueSerializer = new XmlSerializer(typeof(TValue));
                bool wasEmpty = reader.IsEmptyElement;
                reader.Read();
                if (wasEmpty) return;

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    reader.ReadStartElement("item");
                    reader.ReadStartElement("key");
                    var key = (TKey)keySerializer.Deserialize(reader);
                    reader.ReadEndElement();
                    reader.ReadStartElement("value");
                    var value = (TValue)valueSerializer.Deserialize(reader);
                    reader.ReadEndElement();
                    Add(key, value);
                    reader.ReadEndElement();
                    reader.MoveToContent();
                }
                reader.ReadEndElement();
            }

            public void WriteXml(XmlWriter writer)
            {
                var keySerializer = new XmlSerializer(typeof(TKey));
                var valueSerializer = new XmlSerializer(typeof(TValue));

                foreach (TKey key in Keys)
                {
                    writer.WriteStartElement("item");
                    writer.WriteStartElement("key");
                    keySerializer.Serialize(writer, key);
                    writer.WriteEndElement();
                    writer.WriteStartElement("value");
                    TValue value = this[key];
                    valueSerializer.Serialize(writer, value);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
            }

            public SerializableDictionary(IDictionary<TKey, TValue> dictionary)
                : base(dictionary)
            {

            }
            public SerializableDictionary()
            {

            }

            public void CopyFrom(SerializableDictionary<TKey, TValue> dictionary)
            {
                foreach (var key in dictionary.Keys)
                {
                    Add(key, dictionary[key]);
                }
            }
        }
    }

}
