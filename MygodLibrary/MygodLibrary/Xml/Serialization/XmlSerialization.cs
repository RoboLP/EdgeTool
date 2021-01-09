using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Mygod.Xml.Serialization
{
    public static class XmlSerialization
    {
        private static readonly XmlSerializerNamespaces Namespaces = new XmlSerializerNamespaces();
        private static readonly Dictionary<Type, XmlSerializer> Serializers = new Dictionary<Type, XmlSerializer>();
        private static XmlSerializer GetSerializer<T>()
        {
            var type = typeof(T);
            XmlSerializer result;
            if (Serializers.TryGetValue(type, out result)) return result;
            return Serializers[type] = new XmlSerializer(type);
        }
        static XmlSerialization()
        {
            Namespaces.Add(string.Empty, string.Empty);
        }

        public static void SerializeToFile<T>(string path, T value)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                GetSerializer<T>().Serialize(stream, value, Namespaces);
        }
        public static T DeserializeFromFile<T>(string path)
        {
            if (!File.Exists(path)) return default(T);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return (T) GetSerializer<T>().Deserialize(stream);
        }
    }
}
