using System;
using System.Reflection;
using System.Text;

namespace CSToLua
{
    public static class CSToLuaRegisterHelper
    {
        private static string INDENT = "    ";
        private static StringBuilder strBuilder = new StringBuilder();
        public static string GetRegisterAction(int indent,string funName,string luaName)
        {
            strBuilder.Length = 0;
            string indentStr = GetIndent(indent);
            strBuilder.AppendLine(string.Format("{0}luaState.PushLuaClosure({1},0);",indentStr, funName));
            strBuilder.AppendLine(string.Format("{0}luaState.SetField(-2,\"{1}\");",indentStr, luaName));
            return strBuilder.ToString();
        }

        public static string GetRegisterFunStart(int indent,string funName)
        {
            strBuilder.Length = 0;
            string indentStr = GetIndent(indent);
            strBuilder.AppendLine(string.Format("{0}[Game.Core.DotLua.MonoPInvokeCallback(typeof(Game.Core.DotLua.LuaAPI.lua_CFunction))]", indentStr));
            strBuilder.AppendLine(string.Format("{0}private static int {1}(System.IntPtr ptr){{", indentStr,funName));
            return strBuilder.ToString();
        }

        public static string GetRegisterFunEnd(int indent)
        {
            return GetIndent(indent) + "}\r\n";
        }

        public static string GetToUserDataAction(int indent,Type type,int index,string toName)
        {
            strBuilder.Length = 0;
            string indentStr = GetIndent(indent);
            strBuilder.AppendLine(string.Format("{0}{1} {2} = ({1})luaState.ToUserDataObject({3});", indentStr, GetTypeFullName(type),toName,index));
            return strBuilder.ToString();
        }

        public static string GetToAction(int indent,Type type,int index,string toName)
        {
            strBuilder.Length = 0;
            string indentStr = GetIndent(indent);
            if(IsBaseType(type))
            {
                strBuilder.AppendLine(string.Format("{0}{1} {2}={3}luaState.{4}({5});", indentStr, GetTypeFullName(type),
                                                                                                                    toName, GetToTypeCast(type),
                                                                                                                    GetToActionStr(type), index));
            }else
            {
                strBuilder.AppendLine(string.Format("{0}{1} {2}=({1})luaState.ToSystemObject({3},typeof({1}));", indentStr, GetTypeFullName(type),toName,index));
            }
            return strBuilder.ToString();
        }

        public static string GetToActionWithNoType(int indent, Type type, int index, string toName)
        {
            strBuilder.Length = 0;
            string indentStr = GetIndent(indent);
            if (IsBaseType(type))
            {
                strBuilder.AppendLine(string.Format("{0}{1}={2}luaState.{3}({4});", indentStr,toName, GetToTypeCast(type),
                                                                                                                    GetToActionStr(type), index));
            }
            else
            {
                strBuilder.AppendLine(string.Format("{0}{1}=({2})luaState.ToSystemObject({3},typeof({2}));", indentStr, toName, GetTypeFullName(type), index));
            }
            return strBuilder.ToString();
        }

        public static string GetPushAction(int indent, Type type, string pushName)
        {
            strBuilder.Length = 0;
            string indentStr = GetIndent(indent);
            if(IsBaseType(type))
            {
                strBuilder.AppendLine(string.Format("{0}luaState.{1}({2}{3});", indentStr, GetPushStr(type), GetPushTypeCast(type), pushName));
            }
            else
            {
                strBuilder.AppendLine(string.Format("{0}luaState.PushSystemObject({1},typeof({2}));", indentStr, pushName, GetTypeFullName(type)));
            }
            return strBuilder.ToString();
        }

        public static Type GetTypeWithoutRef(Type type)
        {
            if(type.IsByRef)
            {
                string name = type.FullName;
                name = name.Replace("&", "");

                return GetTypeByName(name);
            }
            return type;
        }

        public static string GetTypeFullName(Type type)
        {
            string name = type.FullName;
            if (type.IsGenericType)
            {
                name = name.Substring(0, name.IndexOf('`')) + "<";
                Type[] argTypes = type.GetGenericArguments();
                for (int i = 0; i < argTypes.Length; i++)
                {
                    name += GetTypeFullName(argTypes[i]) + ",";
                }
                name = name.Substring(0, name.Length - 1);
                name += ">";
            }
            name = name.Replace("+", ".");
            if(type.IsByRef)
                name = name.Replace("&", "");
            return name;
        }

        public static Type GetTypeByName(string typeFullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (assemblies == null || assemblies.Length == 0)
                return null;
            foreach (Assembly assem in assemblies)
            {
                Type type = assem.GetType(typeFullName, false, true);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public static string GetToTypeCast(Type type)
        {
            if (type.IsByRef)
                type = GetTypeWithoutRef(type);
            if (type == typeof(byte))
                return "(byte)";

            if (type.IsEnum)
                return "(" + GetTypeFullName(type) + ")";
            return "";
        }

        public static string GetToActionStr(Type type)
        {
            if (type.IsByRef)
                type = GetTypeWithoutRef(type);

            if (type == typeof(int) || type.IsEnum)
                return "ToInteger";
            if (type == typeof(char) || type == typeof(byte))
                return "ToChar";
            if (type == typeof(uint))
                return "ToUInt";
            if (type == typeof(double))
                return "ToNumber";
            if (type == typeof(float))
                return "ToFloat";
            if (type == typeof(string))
                return "ToString";
            if (type == typeof(bool))
                return "ToBoolean";
            if (type == typeof(long))
                return "ReadLongId";
            if (type == typeof(ulong))
                return "ReadULong";
            return "----";
        }

        public static string GetPushTypeCast(Type type)
        {
            if (type.IsByRef)
                type = GetTypeWithoutRef(type);

            if (type.IsEnum)
                return "(int)";
            return "";
        }

        public static string GetPushStr(Type type)
        {
            if (type.IsByRef)
                type = GetTypeByName(GetTypeFullName(type));

            if (type == typeof(void))
                return null;
            if(type == typeof(int) || type.IsEnum || type == typeof(char) || type == typeof(byte))
            {
                return "PushInteger";
            }
            if (type == typeof(uint) || type == typeof(float) || type == typeof(double))
                return "PushNumber";
            if (type == typeof(long))
                return "PushLongId";
            if (type == typeof(ulong))
                return "PushULong";
            if (type == typeof(string))
                return "PushString";
            if (type == typeof(bool))
                return "PushBoolean";

            return "----";
        }

        public static bool IsBaseType(Type type)
        {

            if (GetTypeFullName(type).Contains("CMessagePanel.MsgInfoList"))
            {
                int i = 0;
            }

            if (type.IsByRef)
                type = GetTypeWithoutRef(type);

            if (type == typeof(object))
                return false;
            if (type.IsEnum)
                return true;
            return type.IsPrimitive || type == typeof(string) ;
        }

        public static string GetIndent(int indent)
        {
            StringBuilder sb = new StringBuilder();
            for(int i =0;i<indent;i++)
            {
                sb.Append(INDENT);
            }
            return sb.ToString();
        }

        public static string GetDefaultValue(Type type)
        {
            if (type.IsByRef)
                type = GetTypeWithoutRef(type);

            if(type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                if(values!=null && values.Length>0)
                {
                    return GetTypeFullName(type) + "." + values.GetValue(0).ToString();
                }
            }else if(type.IsValueType)
            {
                if(type.IsPrimitive)
                {
                    if(type == typeof(int))
                    {
                        return "0";
                    }else if(type == typeof(double))
                    {
                        return "0.0";
                    }else if(type == typeof(float))
                    {
                        return "0.0f";
                    }else if(type == typeof(char))
                    {
                        return "(char)0";
                    }else if(type == typeof(byte))
                    {
                        return "(byte)0";
                    }else if(type == typeof(long))
                    {
                        return "0L";
                    }else if(type == typeof(ulong))
                    {
                        return "0U";
                    }else if(type == typeof(bool))
                    {
                        return "false";
                    }else if(type == typeof(uint))
                    {
                        return "0";
                    }else
                    {
                        return "0";
                    }
                }else
                {
                    return "new " + GetTypeFullName(type) + "()";
                }
            }else if(type.IsClass)
            {
                return "null";
            }else if(type == typeof(string))
            {
                return "";
            }
            return "";
        }

        public static string GetDefaultValue(object defValue,Type type)
        {
            if (type.IsByRef)
                type = GetTypeWithoutRef(type);

            if(type.IsEnum)
            {
                return GetTypeFullName(type) + "." + defValue.ToString();
            }else if(type == typeof(string))
            {
                string v = defValue == null ? "\"\"" : (string)defValue;
                if(v == null)
                {
                    return "\"\"";
                }else
                {
                    return string.Format("\"{0}\"", (string)defValue);
                }
            }else if(type == typeof(int))
            {
                int v = defValue == null ? 0 : (int)defValue;
                return "" + v;
            }else if(type == typeof(uint))
            {
                uint v = defValue == null ? 0 : (uint)defValue;
                return "" + v;
            }
            else if(type == typeof(double))
            {
                double v = defValue == null ? 0.0 : (double)defValue;
                return "" + v;
            }else if(type == typeof(float))
            {
                float v = defValue == null ? 0.0f : (float)defValue;
                return "" + v + "f";
            }else if(type == typeof(long))
            {
                long v = defValue == null ? 1L : (long)defValue;
                return "" + v + "L";
            }else if(type == typeof(ulong))
            {
                ulong v = defValue == null ? 1U : (ulong)defValue;
                return "" + v + "U";
            }else if(type == typeof(bool))
            {
                bool v = defValue == null?false:(bool)defValue;
                return ("" + v).ToLower();
            }else if(type == typeof(byte))
            {
                byte v = defValue == null ? (byte)0 : (byte)defValue;
                return "(byte)" + v;
            }
            else if(type == typeof(char))
            {
                char v = defValue == null ? '0' : (char)defValue;
                return "\'" + v + "\'";
            }else
            {
                return "null";
            }
        }
    }

    
}
