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
    [XmlRoot("Challenge")]
    public class Challenge
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("badgesToComplete")]
        public string BadgesToComplete { get; set; }

        [XmlAttribute("title")]
        public string Title { get; set; }

        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlAttribute("message")]
        public string Message { get; set; }

        public static Challenge Deserialize(string xml)
        {
            Challenge result;

            using (var reader = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(reader))
            {
                var serializer = new XmlSerializer(typeof(Challenge));
                result = (Challenge)serializer.Deserialize(xmlReader);
            }

            return result;
        }

        public string Serialize()
        {
            var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = Encoding.ASCII };

            using (var stream = new MemoryStream())
            using (var writer = XmlWriter.Create(stream, settings))
            {
                var serializer = new XmlSerializer(typeof(Challenge));
                serializer.Serialize(writer, this);

                var xml = Encoding.ASCII.GetString(stream.ToArray());

                return xml;
            }
        }
    }
}
