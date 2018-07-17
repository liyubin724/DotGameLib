using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CSToLua
{
    public class GenCSToLuaTool 
    {

        [MenuItem("DotGame/Lua/Register Class")]
        public static void GenCSToLua()
        {
            CSToLuaRegisterManager.GetInstance().Init(Application.dataPath.Replace("\\","/")+"/ClientCode/client-tools/CSRegisterToLua");
            CSToLuaRegisterManager.GetInstance().ConvertCSToLua(Application.dataPath.Replace("\\", "/") + "/ClientCode/client-cstolua");
            CSToLuaRegisterManager.GetInstance().Dispose();
            AssetDatabase.Refresh();
        }

        [MenuItem("DotGame/Lua/Clear Register")]
        public static void ClearRegister()
        {
            string fileDir = Application.dataPath.Replace("\\", "/") + "/ClientCode/client-cstolua";

            if(Directory.Exists(fileDir))
            {
                Directory.Delete(fileDir, true);
            }

            Directory.CreateDirectory(fileDir);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static class CSToLua{");
            sb.AppendLine("public static void InitStaticRegistToLua(){");
            sb.AppendLine("}}");

            string cstoluaPath = fileDir + "/CSToLua.cs";
            File.WriteAllText(cstoluaPath, sb.ToString());

            AssetDatabase.Refresh();
        }

        //[MenuItem("MHJ/Lua/Create Config")]
        public static void ExportXMLByRegisterTxt()
        {
            AssemblyConfig config = null;
            string configPath = Application.dataPath.Replace("\\", "/") + "/ClientCode/client-tools/CSRegisterToLua/config.xml";
            if(File.Exists(configPath))
            {
                config = AssemblyConfig.Deserialize(File.ReadAllText(configPath));
            }else
            {
                config = new AssemblyConfig();
            }

            string filePath = Application.streamingAssetsPath.Replace("\\", "/") + "/Script/Utils/RegisterClass.txt";
            String content = File.ReadAllText(filePath);
            StringReader sr = new StringReader(content);
            List<string> delList = new List<string>();
            string line = null;
            while((line = sr.ReadLine())!=null)
            {
                line = line.Trim();
                if (line.StartsWith("--"))
                    continue;
                if(line.StartsWith("Register.RegisterClass"))
                {
                    line = line.Replace("Register.RegisterClass", "");
                    line = line.Substring(line.IndexOf("\"")+1, line.LastIndexOf("\"")-2);
                    ClassConfig classConfig = null;
                    //if(line.StartsWith("PBProto") || line.StartsWith("t3config"))
                    {
                        bool isNew = true;
                        foreach(ClassConfig cc in config.exportClasses)
                        {
                            if(cc.name == line)
                            {
                                classConfig = cc;
                                isNew = false;
                                break;
                            }
                        }
                        if(isNew)
                        {
                            classConfig = new ClassConfig();
                            classConfig.name = line;
                            config.exportClasses.Add(classConfig);
                        }

                        if(classConfig!=null)
                        {
                            if(classConfig.name.StartsWith("PBProto"))
                            {
                                classConfig.isConstructor = true;
                            }
                        }
                    }
                }
            }

            FileInfo fi = new FileInfo(configPath);
            DirectoryInfo di = fi.Directory;
            if(!di.Exists)
            {
                di.Create();
            }

            for (int i = config.exportClasses.Count - 1; i >= 0;i-- )
            {
                Type type = CSToLuaRegisterHelper.GetTypeByName(config.exportClasses[i].name);
                bool isDel = false;
                if(type == null)
                {
                    isDel = true;
                }
                if(!isDel)
                {
                    EventInfo[] events = type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if(events!=null && events.Length>0)
                    {
                        isDel = true;
                    }
                }
                if(!isDel)
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    foreach(FieldInfo f in fields)
                    {
                        if(f.FieldType.IsSubclassOf(typeof(Delegate)))
                        {
                            isDel = true;
                            break;
                        }
                    }
                }

                if(!isDel)
                {
                    PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    foreach(PropertyInfo p in properties)
                    {
                        if(p.PropertyType.IsSubclassOf(typeof(Delegate)))
                        {
                            isDel = true;
                            break;
                        }
                    }
                }
                if(!isDel)
                {
                    if(type.IsGenericType)
                    {
                        isDel = true;
                    }
                }

                if(isDel)
                {
                    config.exportClasses.RemoveAt(i);
                }
            }

            AssemblyConfig.Serialize(configPath, config);
            AssetDatabase.Refresh();
        }
    }
}
