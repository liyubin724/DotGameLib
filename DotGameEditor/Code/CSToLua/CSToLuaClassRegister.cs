using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;

namespace CSToLua
{
    public class CSToLuaClassRegister
    {
        private Type registerType;

        private List<ICSToLuaRegister> registerList = new List<ICSToLuaRegister>();

        public CSToLuaClassRegister(Type type)
        {
            registerType = type;

            BindingFlags flag = GetBindingFlag();

            RegisterConstructor();
            RegisterField(flag);
            RegisterProperty(flag);
            RegisterMethod(flag);
        }

        public Type RegisterType
        {
            get { return registerType; }
        }

        public string RegisterToLua()
        {
            int indent = 0;
            string indentStr ="";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static class "+GetRegisterFileName()+"{");
            indent ++;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}private static Game.Core.DotLua.LuaState luaState;", indentStr));
            sb.AppendLine(string.Format("{0}public static int RegisterToLua(Game.Core.DotLua.LuaState lua){{", indentStr));
            indent++;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}luaState = lua;", indentStr));
            sb.AppendLine(string.Format("{0}luaState.NewTable();", indentStr));
            sb.AppendLine(string.Format("{0}luaState.PushValue(-1);", indentStr));
            sb.AppendLine(string.Format("{0}luaState.SetField(-2,\"__index\");", indentStr));
            sb.AppendLine(string.Format("{0}luaState.PushValue(-1);", indentStr));
            sb.AppendLine(string.Format("{0}luaState.SetGlobal(\"{1}\");", indentStr,registerType.Name));
            sb.AppendLine(string.Format("{0}luaState.PushValue(-1);", indentStr));
            sb.AppendLine(string.Format("{0}luaState.SetMetaTable(-2);", indentStr));

            foreach (ICSToLuaRegister register in registerList)
            {
                sb.Append(register.RegisterActionToLua(indent));
            }

            sb.AppendLine(string.Format("{0}return luaState.L_Ref(Game.Core.DotLua.LuaAPI.LUA_REGISTRYINDEX);", indentStr));

            indent--;
            sb.AppendLine(string.Format("{0}}}",CSToLuaRegisterHelper.GetIndent(indent)));

            foreach (ICSToLuaRegister register in registerList)
            {
                sb.Append(register.RegisterFunctionToLua(indent));
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        public string GetRegisterFileName()
        {
            return "CTL_" + CSToLuaRegisterHelper.GetTypeFullName(registerType).Replace(".", "_");
        }

        private void RegisterEvent(BindingFlags flags)
        {

        }

        private void RegisterConstructor()
        {
            if(!CSToLuaRegisterManager.GetInstance().IsConstructorExport(CSToLuaRegisterHelper.GetTypeFullName(registerType)))
            {
                return;
            }

            ConstructorInfo[] cInfos = registerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if(cInfos!=null && cInfos.Length>0)
            {
                registerList.Add(new CSToLuaConstructorRegister(this, cInfos));
            }
        }

        private void RegisterField(BindingFlags flags)
        {
            FieldInfo[] fields = registerType.GetFields(flags);
            foreach (FieldInfo f in fields)
            {
                if (f.IsDefined(typeof(ObsoleteAttribute), true))
                {
                    continue;
                }

                if(!CSToLuaRegisterManager.GetInstance().IsPropertyOrFieldExport(registerType.FullName,f.Name))
                {
                    continue;
                }

                registerList.Add(new CSToLuaFieldRegister(this, f));
            }
        }
        private void RegisterProperty(BindingFlags flags)
        {
            PropertyInfo[] propertys = registerType.GetProperties(flags);
            foreach (PropertyInfo p in propertys)
            {
                if (p.IsDefined(typeof(ObsoleteAttribute), true))
                {
                    continue;
                }
                if (!CSToLuaRegisterManager.GetInstance().IsPropertyOrFieldExport(registerType.FullName, p.Name))
                    continue;

                if (p.GetIndexParameters().Length > 0)
                    continue;

                registerList.Add(new CSToLuaPropertyRegister(this, p));
            }
        }
        private void RegisterMethod(BindingFlags flags)
        {
            MethodInfo[] methods = registerType.GetMethods(flags);
            Dictionary<string, int> methodIndexDic = new Dictionary<string, int>();
            foreach (MethodInfo m in methods)
            {
                if (m.IsGenericMethod)
                    continue;
                if (m.IsDefined(typeof(ObsoleteAttribute), true))
                {
                    continue;
                }

                if(!CSToLuaRegisterManager.GetInstance().IsMethodExport(registerType.FullName,m.Name))
                {
                    continue;
                }

                if (m.Name.StartsWith("set_") || m.Name.StartsWith("get_") || m.Name.StartsWith("add_") || m.Name.StartsWith("remove_") || m.Name.StartsWith("op_"))
                    continue;
                string aliasName = m.Name;
                if (methodIndexDic.ContainsKey(aliasName))
                {
                    methodIndexDic[aliasName] += 1;
                    aliasName += methodIndexDic[aliasName];
                }
                else
                {
                    methodIndexDic.Add(aliasName, 0);
                }
                ParameterInfo[] pInfos = m.GetParameters();
                bool isExport = true;
                foreach (ParameterInfo pInfo in pInfos)
                {
                    Type t = pInfo.ParameterType;
                    if (t.IsGenericType)
                    {
                        isExport = false;
                        break;
                    }
                }

                if (isExport)
                {
                    registerList.Add(new CSToLuaMethodRegister(this, m, aliasName));
                }
            }
        }

        private BindingFlags GetBindingFlag()
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.FlattenHierarchy;
            if (registerType.IsValueType && !registerType.IsPrimitive && !registerType.IsEnum)
                flags = BindingFlags.Public;

            TypeAttributes typeAttr = registerType.Attributes;
            if ((typeAttr & TypeAttributes.Sealed) != 0 && (typeAttr & TypeAttributes.Abstract) != 0)
            {
                flags = flags | BindingFlags.Static;
            }
            else
            {
                flags = flags | BindingFlags.Instance | BindingFlags.Static;
            }

            return flags;
        }
    }
}
