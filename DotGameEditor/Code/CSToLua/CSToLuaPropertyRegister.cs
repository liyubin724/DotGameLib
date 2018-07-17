using System.Reflection;
using System.Text;

namespace CSToLua
{
    public class CSToLuaPropertyRegister : ICSToLuaRegister
    {
        private enum CSToLuaPropertyType
        {
            None,
            Get,
            Set,
            GetSet,
        }

        private CSToLuaPropertyType propertyType = CSToLuaPropertyType.None;
        private CSToLuaClassRegister classRegister = null;
        private PropertyInfo propertyInfo;
        public CSToLuaPropertyRegister(CSToLuaClassRegister cr,PropertyInfo pi)
        {
            classRegister = cr;
            propertyInfo = pi;

            MethodInfo getMethod = propertyInfo.GetGetMethod();
            MethodInfo setMethod = propertyInfo.GetSetMethod();

            if (propertyInfo.CanRead && propertyInfo.CanWrite)
            {
                if(setMethod!=null && getMethod!=null && setMethod.IsPublic && getMethod.IsPublic)
                {
                    propertyType = CSToLuaPropertyType.GetSet;
                }else if(setMethod!=null && setMethod.IsPublic)
                {
                    propertyType = CSToLuaPropertyType.Set;
                }else if(getMethod!=null && getMethod.IsPublic)
                {
                    propertyType = CSToLuaPropertyType.Get;
                }
            }
            else if (propertyInfo.CanRead)
            {
                if (getMethod != null && getMethod.IsPublic)
                {
                    propertyType = CSToLuaPropertyType.Get;
                }
            }
            else
            {
                if (setMethod != null && setMethod.IsPublic)
                {
                    propertyType = CSToLuaPropertyType.Set;
                }
            }
        }

        public string RegisterActionToLua(int indent)
        {
            StringBuilder sb = new StringBuilder();
            if (propertyType == CSToLuaPropertyType.Get || propertyType == CSToLuaPropertyType.GetSet)
            {
                sb.Append(CSToLuaRegisterHelper.GetRegisterAction(indent,GetFunName(CSToLuaPropertyType.Get),"get_"+propertyInfo.Name));
            }
            if (propertyType == CSToLuaPropertyType.Set || propertyType == CSToLuaPropertyType.GetSet)
            {
                sb.Append(CSToLuaRegisterHelper.GetRegisterAction(indent, GetFunName(CSToLuaPropertyType.Set), "set_" + propertyInfo.Name));
            }
            return sb.ToString();
        }

        public string RegisterFunctionToLua(int indent)
        {
            StringBuilder sb = new StringBuilder();
            if (propertyType == CSToLuaPropertyType.Get || propertyType == CSToLuaPropertyType.GetSet)
            {
                sb.Append(RegisterGetFunction(indent));
            }
            if (propertyType == CSToLuaPropertyType.Set || propertyType == CSToLuaPropertyType.GetSet)
            {
                sb.Append(RegisterSetFunction(indent));
            }
            return sb.ToString();
        }

        private string GetFunName(CSToLuaPropertyType pType)
        {
            if (pType == CSToLuaPropertyType.Get)
                return string.Format("GetProperty_{0}", propertyInfo.Name);
            if (pType == CSToLuaPropertyType.Set)
                return string.Format("SetProperty_{0}", propertyInfo.Name);

            return "";
        }

        private string RegisterGetFunction(int indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunStart(indent, GetFunName(CSToLuaPropertyType.Get)));
            indent++;
            if(propertyInfo.GetGetMethod().IsStatic)
            {
                sb.Append(CSToLuaRegisterHelper.GetPushAction(indent, propertyInfo.PropertyType, CSToLuaRegisterHelper.GetTypeFullName(classRegister.RegisterType)+"." + propertyInfo.Name));
            }else
            {
                sb.Append(CSToLuaRegisterHelper.GetToUserDataAction(indent, classRegister.RegisterType, 1, "obj"));
                sb.Append(CSToLuaRegisterHelper.GetPushAction(indent, propertyInfo.PropertyType, "obj." + propertyInfo.Name));
            }
            sb.AppendLine(string.Format("{0}return 1;", CSToLuaRegisterHelper.GetIndent(indent)));
            indent--;
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunEnd(indent));

            return sb.ToString();
        }

        private string RegisterSetFunction(int indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunStart(indent, GetFunName(CSToLuaPropertyType.Set)));
            indent++;
            if (propertyInfo.GetSetMethod().IsStatic)
            {
                sb.Append(CSToLuaRegisterHelper.GetToAction(indent, propertyInfo.PropertyType, 1, "v"));
                sb.AppendLine(string.Format("{0}{1}.{2} = v;", CSToLuaRegisterHelper.GetIndent(indent), CSToLuaRegisterHelper.GetTypeFullName(classRegister.RegisterType), propertyInfo.Name));
            }
            else
            {
                sb.Append(CSToLuaRegisterHelper.GetToUserDataAction(indent, classRegister.RegisterType, 1, "obj"));
                sb.Append(CSToLuaRegisterHelper.GetToAction(indent, propertyInfo.PropertyType, 2, "v"));
                sb.AppendLine(string.Format("{0}obj.{1} = v;", CSToLuaRegisterHelper.GetIndent(indent), propertyInfo.Name));
            }
            sb.AppendLine(string.Format("{0}return 0;", CSToLuaRegisterHelper.GetIndent(indent)));
            indent--;
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunEnd(indent));

            return sb.ToString();
        }

    }
}
