using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace Tridion.Extensions.Reporting.Gamification
{
    [Serializable]
    [XmlRoot("Badge")]
    public class Badge
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("eventFamily")]
        public string EventFamily { get; set; }

        [XmlAttribute("eventPhase")]
        public string EventPhase { get; set; }

        [XmlAttribute("maxValue")]
        public int MaxValue { get; set; }

        [XmlAttribute("minValue")]
        public string MinValue { get; set; }

        [XmlAttribute("operator")]
        public string Operator { get; set; }

        [XmlAttribute("title")]
        public string Title { get; set; }

        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlAttribute("message")]
        public string Message { get; set; }

        [XmlAttribute("gameType")]
        public string GameType { get; set; }

        [XmlAttribute("objectType")]
        public string ObjectType { get; set; }

        public static Badge Deserialize(string xml)
        {
            Badge result;

            using (var reader = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(reader))
            {
                var serializer = new XmlSerializer(typeof(Badge));
                result = (Badge)serializer.Deserialize(xmlReader);
            }

            return result;
        }

        public string Serialize()
        {
            var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = Encoding.ASCII };

            using (var stream = new MemoryStream())
            using (var writer = XmlWriter.Create(stream, settings))
            {
                var serializer = new XmlSerializer(typeof(Badge));
                serializer.Serialize(writer, this);

                var xml = Encoding.ASCII.GetString(stream.ToArray());

                return xml;
            }
        }
    }
}
