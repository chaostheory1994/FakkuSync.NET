using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace FakkuSync.Core.Common
{
    public static class ObjectSaver
    {
        public static void SaveObject<T>(T obj, string path)
        {
            using (Stream output = File.Open(path, FileMode.Create))
            {
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(output, obj);
            }
        }

        public static T ReadObject<T>(string path)
        {
            if (!File.Exists(path))
            {
                throw new ArgumentException($"Could not find path {path}.");
            }

            using (Stream input = File.OpenRead(path))
            {
                BinaryFormatter deserializer = new BinaryFormatter();
                return (T)deserializer.Deserialize(input);
            }
        }
    }
}