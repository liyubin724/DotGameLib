using System.Collections.Generic;
using System.Reflection;
using System;

namespace Game.Core.DotLua
{
    public class LuaClass
    {
        public Type registerType = null;

        LuaState lua = null;
        int registerFuntionIndex = 0;
        int fieldIndex = 0;
        int classIndex = 0;

        public List<MethodInfo> methods = new List<MethodInfo>();
        public List<ParameterInfo[]> paramters = new List<ParameterInfo[]>();
        public FieldInfo[] fields = null;

        public int typeRef = LuaAPI.LUA_REFNIL;

        public bool IsRegister
        {
            get
            {
                return typeRef != LuaAPI.LUA_REFNIL;
            }
        }

        public LuaClass(LuaState lua, Type type, int index)
        {
            classIndex = index;
            this.lua = lua;
            registerType = type;
        }

        public void RegisterClass()
        {
            if (registerType == null)
                return;
            lua.NewTable();
            lua.PushValue(-1);
            lua.SetField(-2, "__index");

            lua.PushValue(-1);
            typeRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
            lua.PushLuaClosure(LuaState.GC, 0);
            lua.SetField(-2, "__gc");

            lua.NewTable();
            lua.PushInteger(classIndex);
            lua.PushLuaClosure(LuaInstance.DoCreate, 1);
            lua.SetField(-2, "__call");
            lua.SetMetaTable(-2);

            string registerName = registerType.Name;
            if (registerType.IsGenericType)
                registerName = registerType.FullName;

            RegisterField();
            RegisterMethod();

            lua.SetGlobal(registerName);
        }

        private void RegisterField()
        {
            fields = registerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);

            if (fields == null)
                return;
            else
            {
                LuaAPI.lua_CFunction cfunc = LuaInstance.SetField;

                for (int i = 0; i < fields.Length; i++)
                {
                    int a = LuaInstance.MergeInt(fieldIndex, classIndex);
                    lua.PushInteger(a);
                    lua.PushLuaClosure(cfunc, 1);
                    lua.SetField(-2, fields[i].Name);

                    fieldIndex++;
                }
            }
        }

        private void RegisterMethod()
        {
            MethodInfo[] methods = registerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            Dictionary<string, int> funcIndexDic = new Dictionary<string, int>();
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].IsGenericMethod)
                    continue;
                string funName = methods[i].Name;
                int fi = 0;
                if (funcIndexDic.TryGetValue(funName, out fi))
                {
                    funcIndexDic[funName] = (++fi);
                }
                else
                {
                    funcIndexDic[funName] = 0;
                }
                if (fi > 0)
                {
                    funName += fi.ToString();
                }

                if(methods[i].IsStatic)
                {
                    RegisterFuncByMethodInfo(funName, methods[i], LuaInstance.CallbackStatic);
                }else
                {
                    RegisterFuncByMethodInfo(funName, methods[i], LuaInstance.Callback);
                }
            }
        }

        public void RegisterFuncByMethodInfo(String funcName, MethodInfo m, LuaAPI.lua_CFunction cfunc)
        {
            if (m == null)
                return; 
            else
            {
                methods.Add(m);
                paramters.Add(m.GetParameters());

                int a = LuaInstance.MergeInt(registerFuntionIndex, classIndex);
                lua.PushInteger(a);
                lua.PushLuaClosure(cfunc, 1);
                lua.SetField(-2, funcName);

                registerFuntionIndex++;
            }
        }
    }
}