using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSToLua
{
    public class CSToLuaRegisterManager
    {
        private static CSToLuaRegisterManager manager = null;

        public static CSToLuaRegisterManager GetInstance()
        {
            if (manager == null)
            {
                manager = new CSToLuaRegisterManager();
            }
            return manager;
        }

        private CSToLuaRegisterManager()
        {

        }

        private AssemblyConfig aConfig = null;
        private static string Assembly_Config_Path = "config.xml";
        public void Init(string configDir)
        {
            aConfig = AssemblyConfig.Deserialize(File.ReadAllText(configDir+"/"+Assembly_Config_Path));
        }

        public void ConvertCSToLua(string dir)
        {
            if (aConfig == null || aConfig.exportClasses == null || aConfig.exportClasses.Count == 0)
                return;

            if(!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using Game.Core.DotLua;");
            sb.AppendLine("public static class CSToLua{");
            sb.AppendLine("public static void InitStaticRegistToLua(){");

            foreach(ClassConfig cc in aConfig.exportClasses)
            {
                Type type = CSToLuaRegisterHelper.GetTypeByName(cc.name);
                if(type!=null)
                {
                    DebugLog(cc.name);

                    CSToLuaClassRegister cr = new CSToLuaClassRegister(type);
                    string script = cr.RegisterToLua();
                    string filePath = dir + "/" + cr.GetRegisterFileName() + ".cs";
                    File.WriteAllText(filePath, script);

                    sb.AppendLine(string.Format("LuaInstance.instance.RegisterData.AddStaticRegisterData(new LuaStaticRegisterData(typeof({0}),{1}.RegisterToLua));", CSToLuaRegisterHelper.GetTypeFullName(cr.RegisterType), cr.GetRegisterFileName()));
                }
            }

            sb.AppendLine("}}");

            string cstoluaPath = dir + "/CSToLua.cs";
            File.WriteAllText(cstoluaPath, sb.ToString());
        }

        public bool IsConstructorExport(string classFullName)
        {
            foreach (ClassConfig cc in aConfig.exportClasses)
            {
                if (cc.name.ToLower() == classFullName.ToLower())
                {
                    return cc.isConstructor;
                }
            }
            return false;
        }

        public bool IsPropertyOrFieldExport(string classFullName,string name)
        {
            foreach(IngoreConfig ic in aConfig.ingoreFieldProperty)
            {
                if(ic.name.ToLower() == name.ToLower())
                {
                    return false;
                }
            }

            foreach (ClassConfig cc in aConfig.exportClasses)
            {
                if (cc.name.ToLower() == classFullName.ToLower())
                {
                    if (cc.ingorePropertyOrField == null || cc.ingorePropertyOrField.Count == 0)
                        break;
                    foreach (IngoreConfig ic in cc.ingorePropertyOrField)
                    {
                        if (ic.name.ToLower() == name.ToLower())
                        {
                            return false;
                        }
                    }

                    break;
                }
            }
            return true;
        }

        public bool IsMethodExport(string classFullName,string methodName)
        {
            foreach(IngoreConfig ic in aConfig.ingoreMethods)
            {
                if(ic.name.ToLower() == methodName.ToLower())
                {
                    return false;
                }
            }
            foreach(ClassConfig cc in aConfig.exportClasses)
            {
                if(cc.name.ToLower() == classFullName.ToLower())
                {
                    if (cc.ingoreMethods == null || cc.ingoreMethods.Count == 0)
                        break;
                    foreach(IngoreConfig ic in cc.ingoreMethods)
                    {
                        if (ic.name.ToLower() == methodName.ToLower())
                        {
                            return false;
                        }
                    }
                    break;
                }
            }
            return true;
        }

        private StreamWriter logSW = null;
        public void DebugLog(string log)
        {
            if (logSW == null)
                logSW = new StreamWriter("D:/cstolua.log", false);

            if(!string.IsNullOrEmpty(log))
            {
                logSW.WriteLine(log);
                logSW.Flush();
            }
        }

        public void Dispose()
        {
            if (logSW != null)
            {
                logSW.Flush();
                logSW.Close();
            }
            logSW = null;

            manager = null;
        }
    }
}
