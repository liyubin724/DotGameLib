using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

namespace CSToLua
{
    [XmlRoot("configs")]
    public class AssemblyConfig
    {
        [XmlElement("class")]
        public List<ClassConfig> exportClasses = new List<ClassConfig>();

        [XmlElement("ingore_method")]
        public List<IngoreConfig> ingoreMethods = new List<IngoreConfig>();
        [XmlElement("ingore_field_property")]
        public List<IngoreConfig> ingoreFieldProperty = new List<IngoreConfig>();

        public static AssemblyConfig Deserialize(string xml)
        {
            AssemblyConfig config = null;
            XmlSerializer serializer = new XmlSerializer(typeof(AssemblyConfig));
            StringReader reader = new StringReader(xml);

            config = (AssemblyConfig)serializer.Deserialize(reader);
            reader.Close();

            return config;
        }

        public static void Serialize(string path,AssemblyConfig config)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AssemblyConfig));
            config.exportClasses.Sort((x, y) => x.name.CompareTo(y.name));
            FileStream file = new FileStream(path,FileMode.Create);
            serializer.Serialize(file, config);
            file.Flush();
            file.Close();
        }
    }

    public class ClassConfig
    {
        [XmlAttribute("name")]
        public string name;
        [XmlAttribute("is_constructor")]
        public bool isConstructor = false;

        [XmlElement("ingore_method")]
        public List<IngoreConfig> ingoreMethods = new List<IngoreConfig>();
        [XmlElement("ingore_field_property")]
        public List<IngoreConfig> ingorePropertyOrField = new List<IngoreConfig>();
    }

    public class IngoreConfig
    {
        [XmlAttribute("name")]
        public string name;
    }
}
