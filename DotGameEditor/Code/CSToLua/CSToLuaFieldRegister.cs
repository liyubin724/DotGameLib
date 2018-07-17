using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSToLua
{
    public class CSToLuaFieldRegister : ICSToLuaRegister
    {
        private enum CSToLuaFieldType
        {
            None,
            Get,
            Set,
            GetSet,
        }

        private FieldInfo fieldInfo = null;
        private CSToLuaClassRegister classRegister = null;
        private CSToLuaFieldType fieldType = CSToLuaFieldType.None;
        public CSToLuaFieldRegister(CSToLuaClassRegister cr,FieldInfo fi)
        {
            classRegister = cr;
            fieldInfo = fi;

            if (fieldInfo.IsStatic)
            {
                if ((fieldInfo.Attributes & FieldAttributes.Literal) == 0)
                {
                    fieldType = CSToLuaFieldType.GetSet;
                }
                fieldType = CSToLuaFieldType.Get;
            }
            else
            {
                if ((fieldInfo.Attributes & FieldAttributes.Literal) == 0)
                {
                    fieldType = CSToLuaFieldType.GetSet;
                }
                fieldType = CSToLuaFieldType.Get;
            }
        }

        public string RegisterActionToLua(int indent)
        {
            return CSToLuaRegisterHelper.GetRegisterAction(indent, GetFunName(fieldType), fieldInfo.Name);
        }

        public string RegisterFunctionToLua(int indent)
        {
            if (fieldType == CSToLuaFieldType.Get)
                return RegisterGetFunction(indent);
            else if(fieldType == CSToLuaFieldType.GetSet)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(RegisterGetFunction(indent));
                sb.Append(RegisterSetFunction(indent));
                sb.Append(RegisterGetSetFunction(indent));
                return sb.ToString();
            }
            return "";
        }

        private string GetFunName(CSToLuaFieldType fType)
        {
            switch (fType)
            {
                case CSToLuaFieldType.Get:
                    return string.Format("GetField_{0}",fieldInfo.Name);
                case CSToLuaFieldType.GetSet:
                    return string.Format("GetSetField_{0}",fieldInfo.Name);
                case CSToLuaFieldType.Set:
                    return string.Format("SetField_{0}", fieldInfo.Name);
            }
            return "";
        }

        private string RegisterGetFunction(int indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunStart(indent, GetFunName(CSToLuaFieldType.Get)));
            indent++;
            if(fieldInfo.IsStatic)
            {
                sb.Append(CSToLuaRegisterHelper.GetPushAction(indent, fieldInfo.FieldType, CSToLuaRegisterHelper.GetTypeFullName(classRegister.RegisterType)+"." + fieldInfo.Name));
            }else
            {
                sb.Append(CSToLuaRegisterHelper.GetToUserDataAction(indent, classRegister.RegisterType, 1, "obj"));
                sb.Append(CSToLuaRegisterHelper.GetPushAction(indent, fieldInfo.FieldType, "obj." + fieldInfo.Name));
            }
            sb.AppendLine(string.Format("{0}return 1;",CSToLuaRegisterHelper.GetIndent(indent)));
            indent--;
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunEnd(indent));
            
            return sb.ToString();
        }

        private string RegisterSetFunction(int indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunStart(indent, GetFunName(CSToLuaFieldType.Set)));
            indent++;
            if (fieldInfo.IsStatic)
            {
                sb.Append(CSToLuaRegisterHelper.GetToAction(indent, fieldInfo.FieldType, 2, "v"));
                sb.AppendLine(string.Format("{0}{1}.{2} = v;", CSToLuaRegisterHelper.GetIndent(indent), CSToLuaRegisterHelper.GetTypeFullName(classRegister.RegisterType), fieldInfo.Name));
            }
            else
            {
                sb.Append(CSToLuaRegisterHelper.GetToUserDataAction(indent, classRegister.RegisterType, 1, "obj"));
                sb.Append(CSToLuaRegisterHelper.GetToAction(indent, fieldInfo.FieldType, 2,"v"));
                sb.AppendLine(string.Format("{0}obj.{0} = v;", CSToLuaRegisterHelper.GetIndent(indent), fieldInfo.Name));
            }
            sb.AppendLine(string.Format("{0}return 0;", CSToLuaRegisterHelper.GetIndent(indent)));
            indent--;
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunEnd(indent));

            return sb.ToString();
        }

        private string RegisterGetSetFunction(int indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunStart(indent, GetFunName(CSToLuaFieldType.GetSet)));
            indent++;
            sb.AppendLine(string.Format("{0}int top = luaState.GetTop();", CSToLuaRegisterHelper.GetIndent(indent)));
            sb.AppendLine(string.Format("{0}if(top == 0){{", CSToLuaRegisterHelper.GetIndent(indent)));
            indent++;
            sb.AppendLine(string.Format("{0}return {1}(ptr);", CSToLuaRegisterHelper.GetIndent(indent), GetFunName(CSToLuaFieldType.Get)));
            indent--;
            sb.AppendLine(string.Format("{0}}}else{{", CSToLuaRegisterHelper.GetIndent(indent)));
            indent++;
            sb.AppendLine(string.Format("{0}return {1}(ptr);", CSToLuaRegisterHelper.GetIndent(indent), GetFunName(CSToLuaFieldType.Set)));
            indent--;
            sb.AppendLine(string.Format("{0}}}", CSToLuaRegisterHelper.GetIndent(indent)));
            indent--;
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunEnd(indent));

            return sb.ToString();
        }
    }
}
