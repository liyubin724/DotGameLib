using Game.Core.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.DotLua
{
    public class LuaTable : IDisposable
    {
        private LuaState lua = null;
        private int objRef = LuaAPI.LUA_REFNIL;

        private Dictionary<int, LuaTable> childInfoIntDic = null;
        private Dictionary<string, LuaTable> childInfoStrDic = null;

        public LuaTable(LuaState l,string requireLuaPath)
        {
            if(!string.IsNullOrEmpty(requireLuaPath))
            {
                lua = l;
                string dataName = System.IO.Path.GetFileNameWithoutExtension(requireLuaPath);
                bool isReady = false;
                lua.GetGlobal(dataName);
                if(lua.IsNil(-1))
                {
                    lua.Pop(1);
                    ThreadStatus status = lua.L_DoString(string.Format("require \"{0}\"", requireLuaPath));
                    if(status == ThreadStatus.LUA_OK)
                    {
                        lua.GetGlobal(dataName);
                        if(lua.IsNil(-1))
                        {
                            lua.Pop(1);
                        }else
                        {
                            isReady = true;
                        }
                    }
                }
                else
                {
                    isReady = true;
                }

                if(isReady)
                {
                    if(lua.Type(-1) == LuaType.LUA_TTABLE)
                    {
                        lua.PushValue(-1);
                        objRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
                    }
                    lua.Pop(1);
                }
            }
        }

        public LuaTable(LuaState l)
        {
            lua = l;
            if (lua.Type(-1) == LuaType.LUA_TTABLE)
            {
                lua.PushValue(-1);
                objRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
            }
            else
            {
                Debug.LogError("LuaDataInfo->The class can only used for Table!");
            }
        }

        public LuaTable(LuaState l,int objRef)
        {
            lua = l;
            this.objRef = objRef;
        }

        ~LuaTable()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool isDisposed = false;
        private void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if(lua!=null && lua.GetLuaPtr()!= IntPtr.Zero)
            {
                if (objRef == LuaAPI.LUA_REFNIL)
                    lua.L_Unref(LuaAPI.LUA_REGISTRYINDEX, ref objRef);

                if (childInfoIntDic != null && childInfoIntDic.Count > 0)
                {
                    foreach (KeyValuePair<int, LuaTable> kvp in childInfoIntDic)
                    {
                        kvp.Value.Dispose();
                    }
                }
                if (childInfoStrDic != null && childInfoStrDic.Count > 0)
                {
                    foreach (KeyValuePair<string, LuaTable> kvp in childInfoStrDic)
                    {
                        kvp.Value.Dispose();
                    }
                }
            }

            objRef = LuaAPI.LUA_REFNIL;
            childInfoIntDic = null;
            childInfoStrDic = null;

            isDisposed = true;
        }

        public int TableRef()
        {
            return objRef;
        }

        public bool IsValid()
        {
            return objRef != LuaAPI.LUA_REFNIL;
        }

        public T GetValue<T>(int index)
        {
            if (objRef == LuaAPI.LUA_REFNIL)
                return default(T);

            if (index == 0)
                DebugLogger.LogWarning("LuaDataInfo::GetValue->index must start form 1");

            object value = null;
            System.Type tType = typeof(T);
            if(tType == typeof(LuaTable))
            {
                LuaTable tData;
                if (childInfoIntDic != null && childInfoIntDic.TryGetValue(index, out tData))
                {
                    if (tData.objRef != LuaAPI.LUA_REFNIL)
                    {
                        value = tData;
                        return (T)value;
                    }
                    else
                    {
                        childInfoIntDic.Remove(index);
                    }
                }
            }

            lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
            lua.PushInteger(index);
            lua.GetTable(-2);
            value = GetValue(tType);
            lua.Pop(2);

            if (value != null && tType == typeof(LuaTable))
            {
                if (childInfoIntDic == null)
                {
                    childInfoIntDic = new Dictionary<int, LuaTable>();
                }
                childInfoIntDic.Add(index, (LuaTable)value);
            }

            return value == null ? default(T) : (T)value;
        }

        public T GetValue<T>(string key)
        {
            if (objRef == LuaAPI.LUA_REFNIL)
                return default(T);
            object value = null;
            System.Type tType = typeof(T);

            if(tType == typeof(LuaTable))
            {
                LuaTable tData;
                if (childInfoStrDic != null && childInfoStrDic.TryGetValue(key, out tData))
                {
                    if (tData.objRef != LuaAPI.LUA_REFNIL)
                    {
                        value = tData;
                        return (T)value;
                    }
                    else
                    {
                        childInfoStrDic.Remove(key);
                    }
                }
            }
            
            lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
            lua.GetField(-1, key);
            value = GetValue(tType);
            lua.Pop(2);

            if (tType == typeof(LuaTable) && value != null)
            {
                if (childInfoStrDic == null)
                {
                    childInfoStrDic = new Dictionary<string, LuaTable>();
                }
                childInfoStrDic.Add(key, (LuaTable)value);
            }

            return value == null ? default(T) : (T)value;
        }

        public int GetCount()
        {
            if (objRef == LuaAPI.LUA_REFNIL)
                return 0;

            lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
            int count = lua.RawLen(-1);
            lua.Pop(1);
            return count;
        }

        public void GetKeys(out string[] strKeys, out int[] intKeys)
        {
            strKeys = null;
            intKeys = null;
            List<string> strKeyList = new List<string>();
            List<int> intKeyList = new List<int>();

            lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
            lua.PushNil();
            while (lua.Next(-2))
            {
                LuaType lt = lua.Type(-2);
                if (lt == LuaType.LUA_TSTRING)
                {
                    ///TODO::对于此类访问，有时候会造成key位置错误，目前没有好的办法解决
                    string key = lua.ToString(-2);
                    strKeyList.Add(key);
                }
                else if (lt == LuaType.LUA_TNUMBER)
                {
                    int key = lua.ToInteger(-2);
                    intKeyList.Add(key);
                }
                lua.Pop(1);
            }

            lua.Pop(1);

            if (strKeyList.Count > 0)
                strKeys = strKeyList.ToArray();
            if (intKeyList.Count > 0)
                intKeys = intKeyList.ToArray();
        }

        private object GetValue(System.Type type)
        {
            object value = null;
            LuaType luaParaType = lua.Type(-1);
            if (luaParaType == LuaType.LUA_TNIL)
            {
                return value;
            }
            else if (luaParaType == LuaType.LUA_TNUMBER)
            {
                if (type == typeof(int))
                {
                    value = lua.ToInteger(-1);
                }
                else if (type == typeof(float))
                {
                    value = (float)lua.ToNumber(-1);
                }
                else
                {
                    value = lua.ToNumber(-1);
                }
            }
            else if (luaParaType == LuaType.LUA_TSTRING)
            {
                value = lua.ToString(-1);
            }
            else if (luaParaType == LuaType.LUA_TBOOLEAN)
            {
                value = lua.ToBoolean(-1);
            }
            else if (luaParaType == LuaType.LUA_TID)
            {
                value = lua.ReadLongId(-1);
            }
            else if (luaParaType == LuaType.LUA_TTABLE)
            {
                value = new LuaTable(lua);
            }

            return value;
        }
    }
}


