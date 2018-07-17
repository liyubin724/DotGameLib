using System;
using System.Collections.Generic;

namespace Game.Core.DotLua
{
    public delegate int RegisterClassToLua(LuaState lua);

    public class LuaStaticRegisterData
    {
        public Type registerType = null;
        public RegisterClassToLua registerFun = null;
        public int luaRef = LuaAPI.LUA_REFNIL;

        public LuaStaticRegisterData() { }
        public LuaStaticRegisterData(Type type,RegisterClassToLua fun)
        {
            registerType = type;
            registerFun = fun;
        }
    }

    public class LuaRegisterData
    {
        private LuaState luaState = null;
        private int registerIndex = 0;

        private Dictionary<Type, LuaClass> dynamicRegisterDic = new Dictionary<Type, LuaClass>();
        private List<LuaClass> dynamicRegisterList = new List<LuaClass>();
        
        private Dictionary<Type, LuaStaticRegisterData> staticRegisterDic = new Dictionary<Type, LuaStaticRegisterData>();
        private List<Type> ingoreStaticRegisterInfo = new List<Type>();

        public LuaRegisterData(LuaState lua)
        {
            luaState = lua;
        }

        public void Clear()
        {
            registerIndex = 0;
            luaState = null;
            dynamicRegisterDic.Clear();
            dynamicRegisterList.Clear();
            staticRegisterDic.Clear();
            ingoreStaticRegisterInfo.Clear();
        }

        public void AddStaticRegisterData(LuaStaticRegisterData data)
        {
            if(data !=null)
            {
                if(!staticRegisterDic.ContainsKey(data.registerType))
                {
                    staticRegisterDic.Add(data.registerType, data);
                }
            }
        }

        public void AddStaticRegisterData(Type type,RegisterClassToLua fun)
        {
            if(type !=null && fun!=null)
            {
                if(!staticRegisterDic.ContainsKey(type))
                {
                    staticRegisterDic.Add(type, new LuaStaticRegisterData(type, fun));
                }
            }
        }

        public LuaClass GetDynamicRegisterData(int index)
        {
            if(index >=0 && index<dynamicRegisterList.Count)
            {
                return dynamicRegisterList[index];
            }
            return null;
        }

        public LuaClass GetDynamicRegisterData(Type type)
        {
            LuaClass luaClass = null;
            if(dynamicRegisterDic.TryGetValue(type,out luaClass))
            {
                return luaClass;
            }
            return null;
        }

        public int GetRegisterRef(Type type,bool isAutoRegister = true)
        {
            LuaStaticRegisterData staticRegData = null;
            if (!IsIngoreStaticRegister(type) && staticRegisterDic.TryGetValue(type, out staticRegData))
            {
                return staticRegData.luaRef;
            }
            LuaClass dynamicRegData = null;
            if (dynamicRegisterDic.TryGetValue(type, out dynamicRegData))
            {
                return dynamicRegData.typeRef;
            }

            if(isAutoRegister)
            {
                return RegisterToLua(type);
            }

            return LuaAPI.LUA_REFNIL;
        }

        public int RegisterToLua(Type type)
        {
            LuaStaticRegisterData staticRegData = null;
            if(!IsIngoreStaticRegister(type) && staticRegisterDic.TryGetValue(type,out staticRegData))
            { 
                if(staticRegData.luaRef == LuaAPI.LUA_REFNIL)
                {
                    staticRegData.luaRef = staticRegData.registerFun(luaState);

                    return staticRegData.luaRef;
                }else
                {
                    return staticRegData.luaRef;
                }
            }else
            {
                LuaClass dynamicRegData = null;
                if(!dynamicRegisterDic.TryGetValue(type,out dynamicRegData))
                {
                    dynamicRegData = new LuaClass(luaState, type, registerIndex++);
                    dynamicRegisterDic.Add(type, dynamicRegData);
                    dynamicRegisterList.Add(dynamicRegData);

                    dynamicRegData.RegisterClass();

                    return dynamicRegData.typeRef;
                }else
                {
                    return dynamicRegData.typeRef;
                }
            }
        }

        public bool IsIngoreStaticRegister(Type type)
        {
            return ingoreStaticRegisterInfo.Count>0 && ingoreStaticRegisterInfo.Contains(type);
        }

        public void AddIngoreStaticRegister(Type type)
        {
            if (!ingoreStaticRegisterInfo.Contains(type))
            {
                ingoreStaticRegisterInfo.Add(type);
            }
        }
    }
}
