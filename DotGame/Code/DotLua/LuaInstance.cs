using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Game.Core.DotLuaWrapper;


namespace Game.Core.DotLua
{
    public class LuaInstance
    {
        private static LuaInstance instance;
        static object[][] methodParamCached = new object[10][];

        private LuaState lua;
        string luaPackagePath;

        HashSet<string> doFileScripts = new HashSet<string>();

        private LuaRegisterData registerData = null;
        public LuaRegisterData RegisterData
        {
            get
            {
                return registerData;
            }
        }

        public static LuaInstance Instance 
        {
            get
            {
                if (instance == null)
                {
                    instance = new LuaInstance();
                }
                return instance;
            }
        }

        LuaInstance()
        {
            for (int i = 0; i < 10; i++)
                methodParamCached[i] = new object[i];
        }

        public void Init(LuaState luaState, string luaRootPath)
        {
            lua = luaState;
            registerData = new LuaRegisterData(lua);

            doFileScripts = new HashSet<string>();
            luaPackagePath = luaRootPath.Replace("\\", "/");

            lua.GetGlobal("package");
            lua.PushString(luaRootPath + "?.txt");
            lua.SetField(-2, "path");
            lua.Pop(1);

            LuaWrapper.PreWrapper(lua);
        }

        public LuaState Get()
        {
            return lua;
        }

        public bool IsValid()
        {
            if(lua == null || lua.GetLuaPtr() == IntPtr.Zero)
            {
                return false;
            }
            return true;
        }

        public static string ConstructString(LuaState lua)
        {
            int top = lua.GetTop();

            StringBuilder builder = new StringBuilder();
            for (int i = 1; i <= top; i++)
            {
                switch (lua.Type(i))
                {
                    case LuaType.LUA_TNIL:
                        builder.Append("nil");
                        break;

                    case LuaType.LUA_TBOOLEAN:
                        builder.Append(lua.ToBoolean(i));
                        break;

                    case LuaType.LUA_TNUMBER:
                        double n = lua.ToNumber(i);
                        builder.Append(n.ToString());
                        break;

                    case LuaType.LUA_TSTRING:
                        builder.Append("\n" + lua.ToString(i));
                        break;

                    case LuaType.LUA_TTABLE:
                        builder.Append("table");
                        break;

                    case LuaType.LUA_TFUNCTION:
                        builder.Append(string.Format("function", new object[0]));
                        break;
                    case LuaType.LUA_TUSERDATA:
                        builder.Append("userdata");
                        break;
                    case LuaType.LUA_TID:
                        builder.Append("TID:" + lua.ToString(i));
                        break;
                    default:
                        builder.Append("unknow");
                        break;
                }
                if ((i + 1) <= top)
                {
                    builder.Append(",");
                }
            }
            return builder.ToString();
        }


        public void DoFile(string filename)
        {
            if (doFileScripts.Contains(filename))
            {
                return;
            }
            doFileScripts.Add(filename);

#if LUAPACK
            string path = filename;
#else
            string path = luaPackagePath + filename;
#endif
            lua.L_DoFile(path);
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        static public int SetField(IntPtr l)
        {
            LuaState lua = LuaInstance.Instance.lua;

            int id = lua.ToInteger(lua.UpvalueIndex(1));
            int classIndex = 0;
            int fieldIndex = 0;
            SpritInt(id, ref fieldIndex, ref classIndex);

            LuaClass luaClass = LuaInstance.Instance.RegisterData.GetDynamicRegisterData(classIndex);
            FieldInfo fieldInfo = luaClass.fields[fieldIndex];

            int top = lua.GetTop();
            if(fieldInfo.IsStatic)
            {
                if(top > 0)
                {
                    object paramValue = lua.ToSystemObject(1, fieldInfo.FieldType);
                    fieldInfo.SetValue(null, paramValue);
                    return 0;
                }else
                {
                    object returnValue = fieldInfo.GetValue(null);
                    lua.PushSystemObject(returnValue,fieldInfo.FieldType);
                    return 1;
                }
            }else
            {
                object obj = lua.ToUserDataObject(1);
                if(top > 1)
                {
                    object paramValue = lua.ToSystemObject(2, fieldInfo.FieldType);
                    fieldInfo.SetValue(obj, paramValue);
                    return 0;
                }else
                {
                    object returnValue = fieldInfo.GetValue(obj);
                    lua.PushSystemObject(returnValue, fieldInfo.FieldType);
                    return 1;
                }
            }
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        public static int DoCreate(IntPtr L)
        {
            LuaState lua = LuaInstance.Instance.lua;
            int classIndex = lua.ToInteger(lua.UpvalueIndex(1));
            LuaClass luaClass = LuaInstance.Instance.RegisterData.GetDynamicRegisterData(classIndex);
            Type classType = luaClass.registerType;
            object obj = classType.Assembly.CreateInstance(classType.FullName);
            lua.NewClassUserData(obj);
            return 1;
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        static public int Callback(IntPtr l)
        {
            LuaState lua = LuaInstance.Instance.lua;
            object obj = lua.ToUserDataObject(1);
            return DoCallBack(lua, obj, 2);
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        static public int CallbackStatic(IntPtr l)
        {
            LuaState lua = LuaInstance.Instance.lua;
            return DoCallBack(lua, null, 1);
        }

        static int DoCallBack(LuaState lua, object obj, int indexInStack)
        {
            int id = lua.ToInteger(lua.UpvalueIndex(1));

            int classIndex = 0;
            int methodIndex = 0;
            SpritInt(id, ref methodIndex, ref classIndex);

            LuaClass luaClass = LuaInstance.Instance.RegisterData.GetDynamicRegisterData(classIndex);
            ParameterInfo[] ps = luaClass.paramters[methodIndex];
            MethodInfo methodInfo = luaClass.methods[methodIndex];

            object[] p = LuaInstance.methodParamCached[luaClass.paramters[methodIndex].Length];

            if (ps.Length > 0)
            {
                int top = lua.GetTop();
                for(int i =0;i<ps.Length;i++)
                {
                    if(top < indexInStack + i)
                    {
                        if(ps[i].IsOptional)
                        {
                            p[i] = ps[i].DefaultValue;
                        }else
                        {
                            p[i] = ps[i].ParameterType.IsValueType ? Activator.CreateInstance(ps[i].ParameterType) : null;
                        }
                    }else
                    {
                        p[i] = lua.ToSystemObject(indexInStack + i, ps[i].ParameterType);
                    }
                }
            }
            try
            {
                object result = methodInfo.Invoke(obj, p);
                if (methodInfo.ReturnType == typeof(void))
                {
                    return 0;
                }
                lua.PushSystemObject(result, methodInfo.ReturnType);
            }catch(Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
            }

            return 1;
        }

        public static int MergeInt(int first, int second)
        {
            return ((first << 16) + second);
        }

        public static void SpritInt(int num, ref int reslut1, ref int reslut2)
        {
            reslut1 = num >> 16;
            reslut2 = num & 0xffff;
        }

        public static string LogCallBackError(Exception ex)
        {
            string luaError = "";

            luaError = LuaInstance.Instance.Get().ErrorInfo();

            string errorString = "luaError: " + luaError + Environment.NewLine;
            if (ex.InnerException != null && ex.InnerException.Message != "")
            {
                errorString += ex.InnerException.Message + Environment.NewLine;
                errorString += ex.InnerException.StackTrace + Environment.NewLine;
            }
            else
            {
                errorString += " " + ex.Message + Environment.NewLine;
                if (ex.Message == "")
                {
                    errorString += " " + ex.ToString() + Environment.NewLine;
                }
                if (ex.InnerException != null && ex.InnerException.Message == "")
                {
                    errorString += " " + ex.InnerException.ToString() + Environment.NewLine;
                }
            }

            return errorString;
        }

        public void Dispose()
        {
            if (lua != null)
            {
                lua.Close();
            }
            lua = null;
            instance = null;
        }
    }
}