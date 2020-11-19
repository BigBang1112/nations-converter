using System.IO;
using YamlDotNet.Serialization;

namespace NationsConverter
{
    public static class YamlManager
    {
        public static T Parse<T>(Stream stream)
        {
            using (var r = new StreamReader(stream))
            {
                Deserializer yaml = new Deserializer();
                return yaml.Deserialize<T>(r);
            }
        }

        public static T Parse<T>(string fileName)
        {
            using (var fs = File.OpenRead(fileName))
                return Parse<T>(fs);
        }
    }
}
