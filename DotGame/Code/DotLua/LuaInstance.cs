using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Game.Core.DotLuaWrapper;


namespace Game.Core.DotLua
{
    public class LuaInstance
    {
        private static LuaInstance instance_;
        private LuaState lua_;

        static object[][] params_ = new object[10][];

        public int table_table_ref = LuaAPI.LUA_REFNIL;
        HashSet<string> code_ = new HashSet<string>();
        string luaRootPath_;

        private LuaRegisterData registerData = null;
        public LuaRegisterData RegisterData
        {
            get
            {
                return registerData;
            }
        }

        public static LuaInstance instance 
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = new LuaInstance();
                }
                return instance_;
            }
        }

        LuaInstance()
        {
            for (int i = 0; i < 10; i++)
                params_[i] = new object[i];
        }

        public void Init(LuaState luaState, string luaRootPath)
        {
            lua_ = luaState;
            registerData = new LuaRegisterData(lua_);

            code_ = new HashSet<string>();

            luaRootPath_ = luaRootPath.Replace("\\", "/");

            lua_.GetGlobal("package");
            lua_.PushString(luaRootPath + "?.txt");
            lua_.SetField(-2, "path");
            lua_.Pop(1);

            lua_.GetGlobal("table");
            table_table_ref = lua_.L_Ref(LuaAPI.LUA_REGISTRYINDEX);

            LuaRegisterWrapper.RegisterToLua(lua_);
            DebugLoggerWrapper.RegisterToLua(lua_);
        }

        public LuaState Get()
        {
            return lua_;
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


        public void DoFile(string filename, bool checkContain = true)
        {
            if (checkContain)
            {
                if (code_.Contains(filename))
                {
                    return;
                }
                code_.Add(filename);
            }

#if LUAPACK
            string path = filename;
#else
            string path = luaRootPath_ + filename;
#endif
            lua_.L_DoFile(path);
        }

        //CallBacks for LuaClass
        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        static public int SetField(IntPtr l)
        {
            LuaState lua = LuaInstance.instance.lua_;

            int id = lua.ToInteger(lua.UpvalueIndex(1));
            int classIndex = 0;
            int fieldIndex = 0;
            SpritInt(id, ref fieldIndex, ref classIndex);

            LuaClass luaClass = LuaInstance.instance.RegisterData.GetDynamicRegisterData(classIndex);
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

          //New Instance
        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        public static int DoCreate(IntPtr L)
        {
            LuaState lua = LuaInstance.instance.lua_;
            int classIndex = lua.ToInteger(lua.UpvalueIndex(1));
            LuaClass luaClass = LuaInstance.instance.RegisterData.GetDynamicRegisterData(classIndex);
            Type classType = luaClass.registerType;
            object obj = classType.Assembly.CreateInstance(classType.FullName);
            lua.NewClassUserData(obj);
            return 1;
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        static public int Callback(IntPtr l)
        {
            LuaState lua = LuaInstance.instance.lua_;
            object obj = lua.ToUserDataObject(1);
            return DoCallBack(lua, obj, 2);
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        static public int CallbackStatic(IntPtr l)
        {
            LuaState lua = LuaInstance.instance.lua_;
            return DoCallBack(lua, null, 1);
        }

        static int DoCallBack(LuaState lua, object obj, int indexInStack)
        {
            int id = lua.ToInteger(lua.UpvalueIndex(1));

            int classIndex = 0;
            int methodIndex = 0;
            SpritInt(id, ref methodIndex, ref classIndex);

            LuaClass luaClass = LuaInstance.instance.RegisterData.GetDynamicRegisterData(classIndex);
            ParameterInfo[] ps = luaClass.paramters[methodIndex];
            MethodInfo methodInfo = luaClass.methods[methodIndex];

            object[] p = LuaInstance.params_[luaClass.paramters[methodIndex].Length];

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

            luaError = LuaInstance.instance.Get().ErrorInfo();

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
            if (lua_ != null)
            {
                lua_.Close();
                lua_ = null;
            }
            instance_ = null;
        }
    }
}