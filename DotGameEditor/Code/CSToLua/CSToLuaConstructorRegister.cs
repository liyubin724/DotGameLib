using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CSToLua
{
    public class CSToLuaConstructorRegister : ICSToLuaRegister
    {
        private CSToLuaClassRegister classRegister;
        private ConstructorInfo[] constructorInfo;

        public CSToLuaConstructorRegister(CSToLuaClassRegister cr,ConstructorInfo[] ci)
        {
            classRegister = cr;
            constructorInfo = ci;
        }

        public string RegisterActionToLua(int indent)
        {
            if (constructorInfo == null || constructorInfo.Length == 0)
                return "";
            return CSToLuaRegisterHelper.GetRegisterAction(indent, "CreateInstance", "__call");
        }

        public string RegisterFunctionToLua(int indent)
        {
            if (constructorInfo == null || constructorInfo.Length == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            string indentStr = "";
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunStart(indent, "CreateInstance"));

            indent++;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}int top = luaState.GetTop();", indentStr));
            sb.AppendLine(string.Format("{0}System.Object[] pInfos = new System.Object[top-1];",indentStr));
            sb.AppendLine(string.Format("{0}for(int i =0;i<top -1;i++){{", indentStr));
            indent++;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}pInfos[i] = luaState.ToSystemObject(i+2,typeof(System.Object));", indentStr));
            indent--;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}}}", indentStr));
            sb.AppendLine(string.Format("{0}try{{", indentStr));
            indent++;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}{1} obj = ({1})System.Activator.CreateInstance(typeof({1}), pInfos);", indentStr,CSToLuaRegisterHelper.GetTypeFullName(classRegister.RegisterType)));
            sb.Append(CSToLuaRegisterHelper.GetPushAction(indent,classRegister.RegisterType,"obj"));
            indent--;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}}}catch{{", indentStr));
            indent++;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}luaState.PushNil();", indentStr));
            indent--;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}}}", indentStr));
            sb.AppendLine(string.Format("{0}return 1;",indentStr));
            indent--;

            sb.Append(CSToLuaRegisterHelper.GetRegisterFunEnd(indent));

            return sb.ToString();
        }
    }
}
