﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Game.Core.Util;

namespace Game.Core.DotLua
{
    // For iOS AOT
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type t) { }
    }

    public class LuaState
    {
        private IntPtr luaPtr = IntPtr.Zero;
        private LuaAPI.lua_CFunction pfnDispatch;
        private LuaAPI.lua_CFunction pfnPanic;
		private static LuaState globalLuaState;

        public LuaState(IntPtr lua)
        {
            this.luaPtr = lua;
			globalLuaState = this;
            this.pfnPanic = LuaState.Lua_Panic;
            this.pfnDispatch = LuaState.Lua_DispatchFun;
            LuaAPI.lua_atpanic(this.luaPtr, this.pfnPanic);
        }

        public void Close()
        {
            LuaAPI.lua_close(luaPtr);
            luaPtr = IntPtr.Zero;
        }

        public bool CheckStack(int size)
        {
            return (LuaAPI.lua_checkstack(this.luaPtr, size) != 0);
        }

        public void Concat(int n)
        {
            LuaAPI.lua_concat(this.luaPtr, n);
        }

        public static IntPtr Create()
        {
            return LuaAPI.luaL_newstate();
        }

        public void CreateTable(int narray, int nrec)
        {
            LuaAPI.lua_createtable(this.luaPtr, narray, nrec);
        }

        public int Error()
        {
            return LuaAPI.lua_error(this.luaPtr);
        }

        public void GetField(int index, string key)
        {
            LuaAPI.lua_getfield(this.luaPtr, index, key);
        }

        public void GetGlobal(string name)
        {
            LuaAPI.lua_getfield(this.luaPtr, LuaAPI.LUA_GLOBALSINDEX, name);
        }

        public IntPtr GetLuaPtr()
        {
            return this.luaPtr;
        }

        public bool GetMetaTable(int index)
        {
            return (LuaAPI.lua_getmetatable(this.luaPtr, index) != 0);
        }

        public void GetTable(int index)
        {
            LuaAPI.lua_gettable(this.luaPtr, index);
        }

        public int GetTop()
        {
            return LuaAPI.lua_gettop(this.luaPtr);
        }

        public void Insert(int index)
        {
            LuaAPI.lua_insert(this.luaPtr, index);
        }

        public bool IsFunction(int index)
        {
            return Type(index) == LuaType.LUA_TFUNCTION;
        }

        public bool IsNil(int index)
        {
            return Type(index) == LuaType.LUA_TNIL;
        }

        public bool IsNone(int index)
        {
            return Type(index) == LuaType.LUA_TNONE;
        }

        public bool IsNoneOrNil(int index)
        {
            LuaType type = Type(index);
            return type == LuaType.LUA_TNIL || type == LuaType.LUA_TNONE;
        }

        public bool IsString(int index)
        {
            return Type(index) == LuaType.LUA_TSTRING;
        }

        public bool IsTable(int index)
        {
            return Type(index) == LuaType.LUA_TTABLE;
        }

        public int L_CheckInteger(int narg)
        {
            return (int)LuaAPI.luaL_checkinteger(this.luaPtr, narg);
        }

        public double L_CheckNumber(int narg)
        {
            return LuaAPI.luaL_checknumber(this.luaPtr, narg);
        }

        public string L_CheckString(int narg)
        {
            int l = 0;
            IntPtr ptr = LuaAPI.luaL_checklstring(this.luaPtr, narg, ref l);
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }
            return LuaAPI.StringFromNativeUtf8(ptr, l);
        }

        public void L_CheckType(int index, LuaType t)
        {
            LuaAPI.luaL_checktype(this.luaPtr, index, (int)t);
        }


        public ThreadStatus L_DoFile(string filename)
        {
#if LUAPACK
            if ((LuaAPI.luaL_loadpak(this.m_lua, filename) == 0) && (LuaAPI.lua_pcall(this.m_lua, 0, -1, 0) == 0))
            {
                return ThreadStatus.LUA_OK;
            }
#else
            if ((LuaAPI.luaL_loadfile(this.luaPtr, filename) == 0) && (LuaAPI.lua_pcall(this.luaPtr, 0, -1, 0) == 0))
            {
                return ThreadStatus.LUA_OK;
            }
#endif
            DebugLogger.LogError("DoFile Error: " + filename + "\nlua stack: " + LuaInstance.ConstructString(LuaInstance.Instance.Get()));
            return ThreadStatus.LUA_ERRRUN;
        }

        public ThreadStatus L_DoString(string s)
        {
            ThreadStatus status = (ThreadStatus)LuaAPI.luaL_loadstring(this.luaPtr, s);
            if (status != ThreadStatus.LUA_OK)
            {
                DebugLogger.LogError(ToString(-1));
                return status;
            }
            status = (ThreadStatus)LuaAPI.lua_pcall(this.luaPtr, 0, -1, 0);
            if (status != ThreadStatus.LUA_OK)
            {
                DebugLogger.LogError(ToString(-1));
            }
            return status;
        }

        public void L_Require(string fileName) {
            PushString(fileName);
            LuaAPI.ll_require(luaPtr);
        }

        public void L_OpenLibs()
        {
            LuaAPI.luaL_openlibs(this.luaPtr);
        }

        public int L_Ref(int t)
        {
            return LuaAPI.luaL_ref(this.luaPtr, t);
        }

        public void L_Unref(int t, ref int reference)
        {
            if (reference != LuaAPI.LUA_REFNIL)
            {
                LuaAPI.luaL_unref(this.luaPtr, t, reference);
                reference = LuaAPI.LUA_REFNIL;
            }
        }

		[MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        private static int Lua_DispatchFun(IntPtr L)
        {
            CSharpFunctionDelegate target = (CSharpFunctionDelegate)GCHandle.FromIntPtr(LuaAPI.lua_touserdata(L, -1001001)).Target;
			return target(LuaState.globalLuaState);
        }

		[MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
		private static int Lua_Panic(IntPtr lua)
        {
#if UNITY_IPHONE
            long len = 0;
#else
            int len = 0;
#endif
            IntPtr ptr = LuaAPI.lua_tolstring(lua, -1, ref len);
            DebugLogger.LogError(new System.Diagnostics.StackTrace().ToString());
            if (ptr != IntPtr.Zero)
            {
                DebugLogger.LogError(string.Format("Lua Panic {0}", LuaAPI.StringFromNativeUtf8(ptr, (int)len)));
            }
            return 0;
        }

        public void NewTable()
        {
            NewTable(0, 0);
        }

        public void NewTable(int narr, int nrec = 0)
        {
            LuaAPI.lua_createtable(this.luaPtr, narr, nrec);
        }

        public bool Next(int index)
        {
            return (LuaAPI.lua_next(this.luaPtr, index) != 0);
        }

        public ThreadStatus PCall(int numArgs, int numResults, int errFunc)
        {
            LuaState lua = LuaInstance.Instance.Get();
            int errorHandlerIndex = 0;
#if UNITY_EDITOR||UNITY_STANDALONE
            lua.GetGlobal("lua_get_debug_info");
            lua.PushValue(-2 - numArgs);
            for (int i = 0; i < numArgs; i++)
            {
                lua.PushValue(-numArgs - 2);
            }
            errorHandlerIndex = -2 - numArgs;
#endif
            if (LuaAPI.lua_pcall(this.luaPtr, numArgs, numResults, errorHandlerIndex) != 0)
            {
                DebugLogger.LogError("lua Error stack: " + LuaInstance.ConstructString(lua));
#if UNITY_EDITOR||UNITY_STANDALONE 
                lua.Pop(numArgs + 3);
#endif

                return ThreadStatus.LUA_ERRRUN;
            }

#if UNITY_EDITOR||UNITY_STANDALONE
            int removeNum = numArgs + 2;
            while (removeNum > 0)
            {
                lua.Remove(-numResults - 1);
                removeNum--;
            }
#endif
           
            return ThreadStatus.LUA_OK;
        }

        public void CallMeta(int index,string metaFuncName) {
            int haveMetaFunc = LuaAPI.luaL_callmeta(luaPtr, index, metaFuncName);
            if (haveMetaFunc == 0)
            {
                DebugLogger.LogError("this table DO NOT have metafunction:" + metaFuncName);
            }
        }

        public void PushSystemObject(object obj,Type type)
        {
            if(obj == null)
            {
                PushNil();
            }else
            {
                if(type == typeof(object))
                {
                    type = obj.GetType();
                }
                if(type == typeof(int) || type.IsEnum || type == typeof(char))
                {
                    PushInteger((int)obj);
                }else if(type == typeof(string))
                {
                    PushString((string)obj);
                }
                else if (type == typeof(long))
                {
                    PushLongId((long)obj);
                }else if(type == typeof(ulong))
                {
                    PushULong((ulong)obj);
                }
                else if (type == typeof(double))
                {
                    PushNumber((double)obj);
                }else if(type == typeof(uint))
                {
                    PushNumber((double)((uint)obj));
                }else if(type == typeof(float))
                {
                    PushNumber((double)((float)obj));
                }
                else if(type == typeof(bool))
                {
                    PushBoolean((bool)obj);
                }
                else if(type == typeof(LuaTable))
                {
                    LuaTable luaTable = (LuaTable)obj;
                    RawGetI(LuaAPI.LUA_REGISTRYINDEX, luaTable.TableRef());
                }
                else
                {
                    NewClassUserData(obj);
                }
            }
        }

        public void PushObj(object p) {
            PushSystemObject(p, p.GetType());
        }

        bool LuaGetFunc(int luaclass_ref, string funcName)
        {
            if (luaclass_ref == LuaAPI.LUA_REFNIL)
                return false;
            RawGetI(LuaAPI.LUA_REGISTRYINDEX, luaclass_ref);
            GetField(-1, funcName);

            if (IsNil(-1))
            {
#if UNITY_EDITOR
                DebugLogger.LogWarning(funcName + " dont exist.");
#endif
                Pop(2);
                return false;
            }

            return true;
        }

        public void Pop(int n)
        {
            LuaAPI.lua_settop(this.luaPtr, -n - 1);
        }

        public void PushBoolean(bool b)
        {
            LuaAPI.lua_pushboolean(this.luaPtr, !b ? 0 : 1);
        }

        public GCHandle PushCSharpClosure(CSharpFunctionDelegate f, int n)
        {
            GCHandle handle = GCHandle.Alloc(f);
            IntPtr p = GCHandle.ToIntPtr(handle);
            LuaAPI.lua_pushlightuserdata(this.luaPtr, p);
            LuaAPI.lua_insert(this.luaPtr, -(n + 1));
            LuaAPI.lua_pushcclosure(this.luaPtr, this.pfnDispatch, n + 1);
            return handle;
        }

        public GCHandle PushCSharpFunction(CSharpFunctionDelegate f)
        {
            GCHandle handle = GCHandle.Alloc(f);
            IntPtr p = GCHandle.ToIntPtr(handle);
            LuaAPI.lua_pushlightuserdata(this.luaPtr, p);
            LuaAPI.lua_pushcclosure(this.luaPtr, this.pfnDispatch, 1);
            return handle;
        }

        public void PushLuaClosure(LuaAPI.lua_CFunction func, int n) {
            LuaAPI.lua_pushcclosure(this.luaPtr, func, n);
        
        }


        public void PushInteger(int n)
        {
#if UNITY_ANDROID 
             LuaAPI.lua_pushinteger(this.m_lua, n);
#else
            long longN = n;
            LuaAPI.lua_pushinteger(this.luaPtr, longN);
#endif
        }

        public GCHandle PushLightUserData(object o)
        {
            GCHandle handle = GCHandle.Alloc(o);
            LuaAPI.lua_pushlightuserdata(this.luaPtr, GCHandle.ToIntPtr(handle));
            return handle;
        }

        public void PushNil()
        {
            LuaAPI.lua_pushnil(this.luaPtr);
        }

        public void PushNumber(double n)
        {
            LuaAPI.lua_pushnumber(this.luaPtr, n);
        }

        public void PushString(string s)
        {
           // int len = 0;
          //  IntPtr ptr = LuaAPI.NativeUtf8FromString(s, ref len);
            LuaAPI.lua_pushstring(this.luaPtr, s);
        }

        public void Pushlstring(IntPtr s, int len)
        {
            LuaAPI.lua_pushlstring(this.luaPtr, s, len);
        }

        public void PushValue(int index)
        {
            LuaAPI.lua_pushvalue(this.luaPtr, index);
        }

        public void PushIntList(List<int> list)
        {
            if (list != null)
            {
                NewTable(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    PushInteger(list[i]);
                    RawSetI(-2, i + 1);
                }
            }
            else
            {
                NewTable();
                DebugLogger.Log("List is null!");
            }
        }

        public void PushFloatList(List<float> list)
        {
            if (list != null)
            {
                NewTable(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    PushNumber(list[i]);
                    RawSetI(-2, i + 1);
                }
            }
            else
            {
                NewTable();
                DebugLogger.Log("List is null!");
            }
        }

        public void PushBoolList(List<bool> list)
        {
            if (list != null)
            {
                NewTable(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    PushBoolean(list[i]);
                    RawSetI(-2, i + 1);
                }
            }
            else
            {
                NewTable();
                DebugLogger.LogError("List is null!");
            }
        }

        public void PushStringList(List<String> list)
        {
            if (list != null)
            {
                NewTable(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    PushString(list[i]);
                    RawSetI(-2, i + 1);
                }
            }
            else
            {
                NewTable();
                DebugLogger.LogError("List is null!");
            }
        }

        public bool RawEqual(int index1, int index2)
        {
            return (LuaAPI.lua_rawequal(this.luaPtr, index1, index2) != 0);
        }

        public void RawGet(int index)
        {
            LuaAPI.lua_rawget(this.luaPtr, index);
        }

        public void RawGetI(int index, int n)
        {
            LuaAPI.lua_rawgeti(this.luaPtr, index, n);
        }

        public int RawLen(int index)
        {
            return LuaAPI.lua_objlen(this.luaPtr, index);
        }

        public void RawSet(int index)
        {
            LuaAPI.lua_rawset(this.luaPtr, index);
        }

        public void RawSetI(int index, int n)
        {
            LuaAPI.lua_rawseti(this.luaPtr, index, n);
        }

        public void Remove(int index)
        {
            LuaAPI.lua_remove(this.luaPtr, index);
        }

        public void Replace(int index)
        {
            LuaAPI.lua_replace(this.luaPtr, index);
        }

        public void SetField(int index, string key)
        {
            LuaAPI.lua_setfield(this.luaPtr, index, key);
        }

        public void SetGlobal(string name)
        {
            LuaAPI.lua_setfield(this.luaPtr, LuaAPI.LUA_GLOBALSINDEX, name);
        }

        public bool SetMetaTable(int index)
        {
            return (LuaAPI.lua_setmetatable(this.luaPtr, index) != 0);
        }
        public int NewMetaTable(string name)
        {
            return LuaAPI.luaL_newmetatable(this.luaPtr, name);
        }

        public void SetTable(int index)
        {
            LuaAPI.lua_settable(this.luaPtr, index);
        }

        public void SetTop(int top)
        {
            LuaAPI.lua_settop(this.luaPtr, top);
        }

        public bool ToBoolean(int index)
        {
            return (LuaAPI.lua_toboolean(this.luaPtr, index) != 0);
        }

        public int ToInteger(int index)
        {
            return (int)LuaAPI.lua_tonumber(this.luaPtr, index);
        }

        public char ToChar(int index)
        {
            return (char)ToInteger(index);
        }

        public int ToIntegerX(int index, out bool isnum)
        {
            int num = 0;
            long num2 = LuaAPI.lua_tointeger(this.luaPtr, index);
            isnum = num != 0;
            return (int)num2;
        }

        public double ToNumber(int index)
        {
            return LuaAPI.lua_tonumber(this.luaPtr, index);
        }

        public object ToSystemObject(int index,Type type)
        {
            object result = null;
            LuaType luaType = Type(index);
            if(luaType == LuaType.LUA_TNIL)
            {
                result = type.IsValueType ? Activator.CreateInstance(type) : null;
            }else if(luaType == LuaType.LUA_TNUMBER)
            {
                if(type == typeof(int) || type.IsEnum)
                {
                    result = ToInteger(index);
                }else if(type == typeof(char))
                {
                    result = ToChar(index);
                }else if(type == typeof(float))
                {
                    result = ToFloat(index);
                }else if(type == typeof(double))
                {
                    result = ToNumber(index);
                }else if(type == typeof(uint))
                {
                    result = ToUInt(index);
                }else if(type == typeof(byte))
                {
                    return (byte)ToInteger(index);
                }
                else if (type == typeof(object))
                {
                    result = ToNumber(index);
                }
            }else if(luaType == LuaType.LUA_TSTRING)
            {
                result = ToString(index);
            }else if(luaType == LuaType.LUA_TTABLE)
            {
                if(type == typeof(LuaTable) || type == typeof(object))
                {
                    PushValue(index);
                    int tRef = L_Ref(LuaAPI.LUA_REGISTRYINDEX);
                    result = new LuaTable(this,tRef);
                }
            }else if(luaType == LuaType.LUA_TUSERDATA)
            {
                if(type == typeof(long))
                {
                    result = ReadLongId(index);
                }else if(type == typeof(ulong))
                {
                    result = (ulong)ReadLongId(index);
                }
                else
                {
                    result = ToUserDataObject(index);
                }
            }else if(luaType == LuaType.LUA_TBOOLEAN)
            {
                result = ToBoolean(index);
            }else if(luaType == LuaType.LUA_TID)
            {
                if(type == typeof(ulong))
                {
                    result = ReadULong(index);
                }else if(type == typeof(long))
                {
                    result = ReadLongId(index);
                }
            }
            return result;
        }

        public object ToUserDataObject(int index)
        {
            IntPtr ptr = LuaAPI.lua_topointer(this.luaPtr, index);
            if (ptr == IntPtr.Zero)
            {
                return null;
            }
            IntPtr data_ptr = Marshal.ReadIntPtr(ptr);
            if (data_ptr == IntPtr.Zero)
            {
                return null;
            }
            GCHandle handle = GCHandle.FromIntPtr(data_ptr);
            return handle.Target;
        }

        public byte[] CopyBytesFromUnManaged(int index,int length)
        {
            IntPtr ptr = LuaAPI.lua_topointer(this.luaPtr, index);
            if (ptr == IntPtr.Zero)
            {
                return null;
            }
            byte[] data = new byte[length];
            Marshal.Copy(ptr, data, 0, length);
            return data;
        }

        public float ToFloat(int index)
        {
            return (float)ToNumber(index);
        }

        public uint ToUInt(int index)
        {
            return (uint)ToNumber(index);
        }

        public void PushLongInterger(long n)
        {
            LuaAPI.lua_pushlong(this.luaPtr, n);
        }

        public long ReadLongId(int index) {
            long longValue = LuaAPI.lua_toID(this.luaPtr, index);
            return longValue;
        }

        public void PushLongId(long v) {
            LuaAPI.lua_pushID(this.luaPtr, v);
        }

        public void PushULong(ulong v)
        {
            LuaAPI.lua_pushulong(this.luaPtr, v);
        }

        public ulong ReadULong(int index)
        {
            return LuaAPI.lua_toulong(this.luaPtr, index);
        }

        public GCHandle ToUserDataHandler(int index)
        {
            IntPtr ptr = LuaAPI.lua_topointer(this.luaPtr, index);
            if (ptr == IntPtr.Zero)
            {
                return new GCHandle();
            }
            IntPtr data_ptr = Marshal.ReadIntPtr(ptr);
            if (data_ptr == IntPtr.Zero)
            {
                return new GCHandle();
            }
            GCHandle handle = GCHandle.FromIntPtr(data_ptr);
            return handle;
        }

        public string ToString(int index)
        {
#if UNITY_IPHONE
            long len = 0;
#else
            int len = 0;
#endif
            IntPtr ptr = LuaAPI.lua_tolstring(this.luaPtr, index, ref len);
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }
            return Marshal.PtrToStringAnsi(ptr);
        }


        public int[] ToIntArray(int index)
        {
            int length = LuaAPI.lua_objlen(this.luaPtr, index);
            int[] array = new int[length];

            for (int i = 0; i < length; i++)
            {
                RawGetI(index, i + 1);
                array[i] = (int)ToInteger(-1);
                Pop(1);
            }

            return array;
        }

        public float[] ToFloatArray(int index)
        {
            int length = LuaAPI.lua_objlen(this.luaPtr, index);
            float[] array = new float[length];

            for (int i = 0; i < length; i++)
            {
                RawGetI(index, i + 1);
                array[i] = (float)ToNumber(-1);
                Pop(1);
            }

            return array;
        }

        public bool[] ToBoolArray(int index)
        {
            int length = LuaAPI.lua_objlen(this.luaPtr, index);
            bool[] array = new bool[length];

            for (int i = 0; i < length; i++)
            {
                RawGetI(index, i + 1);
                array[i] = (bool)ToBoolean(-1);
                Pop(1);
            }

            return array;
        }

        public string[] ToStringArray(int index)
        {
            int length = LuaAPI.lua_objlen(this.luaPtr, index);
            string[] array = new string[length];

            for (int i = 0; i < length; i++)
            {
                //RawGetI(index, i + 1);
                PushInteger(i + 1);
                GetTable(index - 1);

                array[i] = (string)ToString(-1);
                Pop(1);
            }

            return array;
        }

        public GCHandle ToUserData(int index)
        {
            return GCHandle.FromIntPtr(LuaAPI.lua_topointer(this.luaPtr, index));
        }

        private GCHandle NewUserData(object o)
        {
            GCHandle handle = GCHandle.Alloc(o);
            IntPtr obj_ptr = GCHandle.ToIntPtr(handle);
           
            IntPtr ptr = LuaAPI.lua_newuserdata(this.luaPtr, IntPtr.Size);

            Marshal.WriteIntPtr(ptr, obj_ptr);
      
            return handle;
        }

        public bool NewClassUserData(object obj)
        {
            if (obj == null)
            {
                PushNil();
                return true;
            }
            int luaRef = LuaInstance.Instance.RegisterData.GetRegisterRef(obj.GetType()) ;
            if (luaRef != LuaAPI.LUA_REFNIL)
            {
                NewUserData(obj);
                RawGetI(LuaAPI.LUA_REGISTRYINDEX, luaRef);
                PushLuaClosure(LuaState.GC, 0);
                SetField(-2, "__gc");
                SetMetaTable(-2);

                return true;
            }
            else if (obj.GetType().IsSubclassOf(typeof(Delegate)))
            {
                NewUserData(obj);
                GetGlobal("LuaDelegate");
                PushLuaClosure(LuaState.GC, 0);
                SetField(-2, "__gc");
                SetMetaTable(-2);
                return true;
            }
            else if ((obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition() == typeof(List<>)) || obj.GetType().IsArray)
            {
                LuaRegister.RegisterType(obj.GetType());
                luaRef = LuaInstance.Instance.RegisterData.GetRegisterRef(obj.GetType());
                if (luaRef != LuaAPI.LUA_REFNIL)
                {
                    NewUserData(obj);
                    RawGetI(LuaAPI.LUA_REGISTRYINDEX, luaRef);
                    PushLuaClosure(LuaState.GC, 0);
                    SetField(-2, "__gc");
                    SetMetaTable(-2);
                    return true;
                }
                else
                {
                    DebugLogger.LogError("-------No Class Found In Lua classesDic--------" + "Type   " + obj.GetType());
                    return false;
                }
            }
            else
            {
                DebugLogger.LogError("-------No Class Found In Lua classesDic--------" + "Type   " + obj.GetType());
                return false;
            }
        }

        public bool NewTypeClassUserData(object obj,Type type)
        {
            int luaRef = LuaInstance.Instance.RegisterData.GetRegisterRef(obj.GetType());
            if (luaRef != LuaAPI.LUA_REFNIL)
            {
                NewUserData(obj);
                RawGetI(LuaAPI.LUA_REGISTRYINDEX, luaRef);
                PushLuaClosure(LuaState.GC, 0);
                SetField(-2, "__gc");
                SetMetaTable(-2);

                return true;
            }
            else
            {
                DebugLogger.LogError("-------No Class Found In Lua classesDic--------" + "Type   " + obj.GetType());
                return false;
            }
        }

        public void NewUserDataWithGC(object o)
        {
            GCHandle handle = GCHandle.Alloc(o);
            IntPtr obj_ptr = GCHandle.ToIntPtr(handle);
            IntPtr ptr = LuaAPI.lua_newuserdata(this.luaPtr, IntPtr.Size);
            Marshal.WriteIntPtr(ptr, obj_ptr);
            SetGCFunc();
        }

        public void SetGCFunc()
        {
            LuaState lua = LuaInstance.Instance.Get();
            lua.NewTable();
            lua.PushLuaClosure(GC, 0);
            lua.SetField(-2, "__gc");
            lua.SetMetaTable(-2);
        }

        public GCHandle NewUnManageMem(object o) {
            GCHandle handle = GCHandle.Alloc(o, GCHandleType.Pinned);
            IntPtr obj_ptr = handle.AddrOfPinnedObject();
            LuaInstance.Instance.Get().PushNumber((double)obj_ptr.ToInt64());
            return handle;
        }

        public GCHandle NewUnManageUserData(object o)
        {
            GCHandle handle = GCHandle.Alloc(o, GCHandleType.Pinned);
            IntPtr obj_ptr = handle.AddrOfPinnedObject();
            LuaAPI.lua_pushlightuserdata(luaPtr, obj_ptr);
            return handle;
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        public static int GC(IntPtr l)
        {
            GCHandle h = LuaInstance.Instance.Get().ToUserDataHandler(-1);
            h.Free();
            
            return 0;
        }
        public LuaType Type(int index)
        {
            return (LuaType)LuaAPI.lua_type(this.luaPtr, index);
        }

        public string TypeName(LuaType t)
        {
            IntPtr ptr = LuaAPI.lua_typename(this.luaPtr, (int)t);
            if (ptr == IntPtr.Zero)
            {
                return null;
            }
            return LuaAPI.StringFromNativeUtf8(ptr, 0);
        }

        public int UpvalueIndex(int i)
        {
            return (LuaAPI.LUA_GLOBALSINDEX - i);
        }

        public string ErrorInfo() { 
            LuaAPI.lua_geterror_info(this.luaPtr);
            if (this.IsNil(-1))
            {
                return string.Empty;
            }
            string errorInfo = LuaInstance.ConstructString(LuaInstance.Instance.Get());
            return errorInfo;
        
        }
    }
}

