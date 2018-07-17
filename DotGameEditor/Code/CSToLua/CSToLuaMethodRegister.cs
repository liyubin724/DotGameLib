using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSToLua
{
    public class CSToLuaMethodRegister : ICSToLuaRegister
    {
        private CSToLuaClassRegister classRegister = null;
        private MethodInfo methodInfo = null;
        private string altasName = "";

        private List<Type> parTypes = new List<Type>();
        private List<int> parRefIndex = new List<int>();
        private List<int> parOutIndex = new List<int>();
        private int parDefaultStartIndex = -1;
        private bool isParams = false;

        public CSToLuaMethodRegister(CSToLuaClassRegister cr,MethodInfo mi,string name)
        {
            methodInfo = mi;
            classRegister = cr;
            altasName = name;

            ParameterInfo[] pInfos = methodInfo.GetParameters();
            if(pInfos!=null && pInfos.Length>0)
            {
                for (int i = 0; i < pInfos.Length;i++ )
                {
                    ParameterInfo p = pInfos[i];
                    parTypes.Add(p.ParameterType);
                    if (p.ParameterType.IsByRef)
                    {
                        if (p.IsOut)
                        {
                            parOutIndex.Add(i);
                        }else
                        {
                            parRefIndex.Add(i);
                        }
                    }else if(p.IsOptional && parDefaultStartIndex == -1)
                    {
                        parDefaultStartIndex = i;
                    }
                }

                ParameterInfo lastP = pInfos[pInfos.Length - 1];
                if(lastP.ParameterType.IsArray && lastP.IsDefined(typeof(ParamArrayAttribute),false))
                {
                    isParams = true;
                }
            }
        }

        public string RegisterActionToLua(int indent)
        {
            return CSToLuaRegisterHelper.GetRegisterAction(indent, "Method_" + altasName, altasName);
        }

        public string RegisterFunctionToLua(int indent)
        {
            CSToLuaRegisterManager.GetInstance().DebugLog("    " + methodInfo.Name);

            ParameterInfo[] pInfos = methodInfo.GetParameters();
            StringBuilder sb = new StringBuilder();
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunStart(indent, "Method_" + altasName));
            if(!methodInfo.IsStatic)
            {
                indent++;
                sb.Append(CSToLuaRegisterHelper.GetToUserDataAction(indent, classRegister.RegisterType, 1, "obj"));
                indent--;
            }
            indent++;
            if(pInfos == null || pInfos.Length ==0)
            {
                RegisterNMethod(indent, sb);
                RegisterReturnMethod(indent, sb);
            }else
            {
                RegisterCheckParamCount(indent, sb);
                if(!isParams && parDefaultStartIndex == -1)
                {
                    RegisterNPamasNDefaultMethod(indent, sb, methodInfo.IsStatic ? 1 : 2);
                    RegisterReturnMethod(indent, sb);
                }else if(!isParams && parDefaultStartIndex>=0)
                {
                    RegisterNParamsDefaultMethod(indent, sb, methodInfo.IsStatic ? 1 : 2);
                    RegisterReturnMethod(indent, sb);
                }else if(isParams && parDefaultStartIndex == -1)
                {
                    RegisterParamsNDefaultMethod(indent, sb, methodInfo.IsStatic ? 1 : 2);
                    RegisterReturnMethod(indent, sb);
                }else
                {
                    RegisterParamsDefaultMethod(indent, sb, methodInfo.IsStatic ? 1 : 2);
                }
            }
            indent--;
            sb.Append(CSToLuaRegisterHelper.GetRegisterFunEnd(indent));
            return sb.ToString();
        }

        private void RegisterCheckParamCount(int indent,StringBuilder sb)
        {
            int minParamCount = parTypes.Count;
            if(parDefaultStartIndex!=-1)
            {
                minParamCount = parDefaultStartIndex;
            }else
            {
                if(isParams)
                {
                    minParamCount -= 1;
                }
            }

            if(!methodInfo.IsStatic)
            {
                minParamCount += 1;
            }

            string indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}int top = luaState.GetTop();", indentStr));
            sb.AppendLine(string.Format("{0}if(top< {1}){{", indentStr, minParamCount));
            indent++;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}UnityEngine.Debug.LogError(\"Count not enough!!\");", indentStr));
            sb.AppendLine(string.Format("{0}return 0;", indentStr));
            indent--;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}}}", indentStr));
        }

        private void RegisterReturnMethod(int indent,StringBuilder sb )
        {
            int returnCount = 0;
            if (methodInfo.ReturnType == typeof(void))
            {
                returnCount = 0;
            }
            else
            {
                sb.Append(CSToLuaRegisterHelper.GetPushAction(indent, methodInfo.ReturnType, "rv"));
                returnCount += 1;
            }
            returnCount += parRefIndex.Count;
            returnCount += parOutIndex.Count;

            for (int i = 0; i < parRefIndex.Count; i++)
            {
                sb.Append(CSToLuaRegisterHelper.GetPushAction(indent, parTypes[parRefIndex[i]], "v" + parRefIndex[i]));
            }
            for (int i = 0; i < parOutIndex.Count; i++)
            {
                sb.Append(CSToLuaRegisterHelper.GetPushAction(indent, parTypes[parOutIndex[i]], "v" + parOutIndex[i]));
            }

            sb.AppendLine(string.Format("{0}return {1};", CSToLuaRegisterHelper.GetIndent(indent), returnCount));
        }

        private void RegisterParamsDefaultMethod(int indent,StringBuilder sb,int startIndex)
        {
            string indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}if(top > " + (startIndex + parDefaultStartIndex - 2) + " && top < " + (startIndex + parTypes.Count - 1) + "){{", indentStr));
            RegisterNParamsDefaultMethod(indent + 1, sb, startIndex,true);
            RegisterReturnMethod(indent+1, sb);
            sb.AppendLine(string.Format("{0}}}else{{", indentStr));
            RegisterParamsNDefaultMethod(indent + 1, sb, startIndex);
            RegisterReturnMethod(indent+1, sb);
            sb.AppendLine(string.Format("{0}}}", indentStr));
        }

        //No params but have default
        private void RegisterNParamsDefaultMethod(int indent,StringBuilder sb ,int startIndex,bool ingoreLast = false)
        {
            string indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            List<string> values = new List<string>();
            for (int i = 0; i < parDefaultStartIndex; i++)
            {
                sb.Append(CSToLuaRegisterHelper.GetToAction(indent, parTypes[i], startIndex + i, "v" + i));
                if (parRefIndex.IndexOf(i) >= 0)
                {
                    values.Add("ref v" + i);
                }
                else if (parOutIndex.IndexOf(i) >= 0)
                {
                    values.Add("out v" + i);
                }
                else
                {
                    values.Add("v" + i);
                }
            }

            for(int i =parDefaultStartIndex;i<parTypes.Count;i++)
            {
                string typeName = CSToLuaRegisterHelper.GetTypeFullName(parTypes[i]);
                object defValue  = methodInfo.GetParameters()[i].DefaultValue;
                string defStr = CSToLuaRegisterHelper.GetDefaultValue(defValue,parTypes[i]);
                sb.AppendLine(string.Format("{0}{1} v"+i +" = {2};",indentStr,typeName,defStr));
            }
            if (methodInfo.ReturnType != typeof(void))
            {
                sb.AppendLine(string.Format("{0}{1} rv = {2};", indentStr, CSToLuaRegisterHelper.GetTypeFullName(methodInfo.ReturnType),CSToLuaRegisterHelper.GetDefaultValue(methodInfo.ReturnType)));
            }

            for (int i = 0; i < (ingoreLast ? parTypes.Count - parDefaultStartIndex : parTypes.Count - parDefaultStartIndex + 1); i++)
            {
                sb.AppendLine(string.Format("{0}if(top == {1}){{",indentStr,startIndex+parDefaultStartIndex+i -1));
                indent++;
                indentStr = CSToLuaRegisterHelper.GetIndent(indent);
                List<string> newValues = new List<string>(); 
                newValues.AddRange(values);

                for (int j = 0; j < i; j++)
                {
                    sb.Append(CSToLuaRegisterHelper.GetToActionWithNoType(indent, parTypes[parDefaultStartIndex + j], startIndex + parDefaultStartIndex + j , "v" + (parDefaultStartIndex + j)));
                    newValues.Add("v" + (parDefaultStartIndex + j));
                }
                
                if (methodInfo.ReturnType == typeof(void))
                {
                    if (methodInfo.IsStatic)
                    {
                        sb.AppendLine(string.Format("{0}{1}.{2}({3});", indentStr, classRegister.RegisterType, methodInfo.Name, string.Join(",", newValues.ToArray())));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("{0}obj.{1}({2});", indentStr, methodInfo.Name, string.Join(",", newValues.ToArray())));
                    }
                }
                else
                {
                    if (methodInfo.IsStatic)
                    {
                        sb.AppendLine(string.Format("{0}rv = {1}.{2}({3});", indentStr, classRegister.RegisterType, methodInfo.Name, string.Join(",", newValues.ToArray())));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("{0}rv = obj.{1}({2});", indentStr, methodInfo.Name, string.Join(",", newValues.ToArray())));
                    }
                }
                indent--;
                indentStr = CSToLuaRegisterHelper.GetIndent(indent);
                sb.AppendLine(string.Format("{0}}}", indentStr));
            }
        }

        //have params but no default
        private void RegisterParamsNDefaultMethod(int indent,StringBuilder sb,int startIndex)
        {
            string indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            List<string> values = new List<string>();
            for (int i = 0; i < parTypes.Count-1; i++)
            {
                sb.Append(CSToLuaRegisterHelper.GetToAction(indent, parTypes[i], startIndex + i, "v" + i));
                if (parRefIndex.IndexOf(i) >= 0)
                {
                    values.Add("ref v" + i);
                }
                else if (parOutIndex.IndexOf(i) >= 0)
                {
                    values.Add("out v" + i);
                }
                else
                {
                    values.Add("v" + i);
                }
            }
            Type paramsType = parTypes[parTypes.Count-1];
            string paramsTypeName = CSToLuaRegisterHelper.GetTypeFullName(paramsType);
            paramsTypeName = paramsTypeName.Replace("[]","");
            Type sPType = CSToLuaRegisterHelper.GetTypeByName(paramsTypeName);
            sb.AppendLine(string.Format("{0}{1}[] ps;", indentStr, paramsTypeName));
            sb.AppendLine(string.Format("{0}if(top > " + (startIndex + parTypes.Count - 2) + "){{", indentStr));
            indent++;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}ps = new {1}[{2}];", indentStr, paramsTypeName, (startIndex + parTypes.Count - 3)));
            sb.AppendLine(string.Format("{0}for(int i =0;i<top - " + (startIndex + parTypes.Count - 2) + ";i++){{",indentStr));
            indent++;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            if (CSToLuaRegisterHelper.IsBaseType(sPType))
            {
                sb.AppendLine(string.Format("{0}ps[{1}]={2}luaState.{3}({4});", indentStr, "i", CSToLuaRegisterHelper.GetToTypeCast(sPType),
                                                                                                                                CSToLuaRegisterHelper.GetToActionStr(sPType),(startIndex + parTypes.Count - 1)+"+i"));
            }
            else
            {
                sb.AppendLine(string.Format("{0}ps[{1}]=({2})luaState.ToSystemObject({3},typeof({2}));", indentStr, "i",paramsTypeName, (startIndex + parTypes.Count - 1)+"+i"));
            }
            indent--;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}}}", indentStr));
            indent--;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}}}else{{",indentStr));
            indent++;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}ps = new {1}[0];", indentStr, paramsTypeName));
            indent--;
            indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            sb.AppendLine(string.Format("{0}}}",indentStr));
            values.Add("ps");
            if (methodInfo.ReturnType == typeof(void))
            {
                if (methodInfo.IsStatic)
                {
                    sb.AppendLine(string.Format("{0}{1}.{2}({3});", indentStr, classRegister.RegisterType, methodInfo.Name, string.Join(",", values.ToArray())));
                }
                else
                {
                    sb.AppendLine(string.Format("{0}obj.{1}({2});", indentStr, methodInfo.Name, string.Join(",", values.ToArray())));
                }
            }
            else
            {
                if (methodInfo.IsStatic)
                {
                    sb.AppendLine(string.Format("{0}{1} rv = {2}.{3}({4});", indentStr, CSToLuaRegisterHelper.GetTypeFullName(methodInfo.ReturnType), classRegister.RegisterType, methodInfo.Name, string.Join(",", values.ToArray())));
                }
                else
                {
                    sb.AppendLine(string.Format("{0}{1} rv = obj.{2}({3});", indentStr, CSToLuaRegisterHelper.GetTypeFullName(methodInfo.ReturnType), methodInfo.Name, string.Join(",", values.ToArray())));
                }
            }
            
        }

        // no params ,no default value
        private void RegisterNPamasNDefaultMethod(int indent,StringBuilder sb,int startIndex)
        {
            string indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            List<string> values = new List<string>();
            for (int i = 0; i < parTypes.Count; i++)
            {
                sb.Append(CSToLuaRegisterHelper.GetToAction(indent, parTypes[i], startIndex + i, "v" + i));
                if (parRefIndex.IndexOf(i) >= 0)
                {
                    values.Add("ref v" + i);
                }
                else if (parOutIndex.IndexOf(i) >= 0)
                {
                    values.Add("out v" + i);
                }
                else
                {
                    values.Add("v" + i);
                }
            }
            if (methodInfo.ReturnType == typeof(void))
            {
                if (methodInfo.IsStatic)
                {
                    sb.AppendLine(string.Format("{0}{1}.{2}({3});", indentStr, classRegister.RegisterType, methodInfo.Name, string.Join(",", values.ToArray())));
                }
                else
                {
                    sb.AppendLine(string.Format("{0}obj.{1}({2});", indentStr, methodInfo.Name, string.Join(",", values.ToArray())));
                }
            }
            else
            {
                if (methodInfo.IsStatic)
                {
                    sb.AppendLine(string.Format("{0}{1} rv = {2}.{3}({4});", indentStr, CSToLuaRegisterHelper.GetTypeFullName(methodInfo.ReturnType), classRegister.RegisterType, methodInfo.Name, string.Join(",", values.ToArray())));
                }
                else
                {
                    sb.AppendLine(string.Format("{0}{1} rv = obj.{2}({3});", indentStr, CSToLuaRegisterHelper.GetTypeFullName(methodInfo.ReturnType), methodInfo.Name, string.Join(",", values.ToArray())));
                }
            }
        }

        //no param
        private void RegisterNMethod(int indent,StringBuilder sb)
        {
            string indentStr = CSToLuaRegisterHelper.GetIndent(indent);
            if (methodInfo.ReturnType == typeof(void))
            {
                if (methodInfo.IsStatic)
                {
                    sb.AppendLine(string.Format("{0}{1}.{2}();", indentStr, classRegister.RegisterType, methodInfo.Name));
                }
                else
                {
                    sb.AppendLine(string.Format("{0}obj.{1}();", indentStr, methodInfo.Name));
                }
            }
            else
            {
                if (methodInfo.IsStatic)
                {
                    sb.AppendLine(string.Format("{0}{1} rv = {2}.{3}();", indentStr, CSToLuaRegisterHelper.GetTypeFullName(methodInfo.ReturnType), classRegister.RegisterType, methodInfo.Name));
                }
                else
                {
                    sb.AppendLine(string.Format("{0}{1} rv = obj.{2}();", indentStr, CSToLuaRegisterHelper.GetTypeFullName(methodInfo.ReturnType), methodInfo.Name));
                }
            }
        }
    }
}
