﻿using Game.Core.DotLua;
using Game.Core.Timer;
using System;

namespace Game.Core.DotLuaWrapper
{
    public static class LuaTimerWrapper
    {
        static LuaState lua;
        public static void RegisterToLua(LuaState l)
        {
            lua = l;
            string[] funcList = new string[]
            {
                "AddTimer",
                "RemoveTimer",
            };

            LuaAPI.lua_CFunction[] funcDeList = new LuaAPI.lua_CFunction[]
            {
                AddTimer,
                RemoveTimer,
            };
            LuaRegister.RegisterToLua(l, "LuaTimerWheel", funcList, funcDeList);
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        private static int AddTimer(IntPtr l)
        {
            object userData = null;
            userData = lua.ToSystemObject(-1, typeof(object));

            LuaFunction endFun = null;
            if(lua.IsFunction(-2))
            {
                lua.PushValue(-2);
                int endFunRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
                int selfRef = LuaAPI.LUA_REFNIL;
                if(lua.IsTable(-7))
                {
                    lua.PushValue(-7);
                    selfRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
                }
                endFun = new LuaFunction(lua, endFunRef, selfRef);
            }
            LuaFunction intervalFun = null;
            if(lua.IsFunction(-3))
            {
                lua.PushValue(-3);
                int intervalFunRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
                int selfRef = LuaAPI.LUA_REFNIL;
                if (lua.IsTable(-7))
                {
                    lua.PushValue(-7);
                    selfRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
                }
                intervalFun = new LuaFunction(lua, intervalFunRef, selfRef);
            }
            LuaFunction startFun = null;
            if(lua.IsFunction(-4))
            {
                lua.PushValue(-4);
                int startFunRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
                int selfRef = LuaAPI.LUA_REFNIL;
                if (lua.IsTable(-7))
                {
                    lua.PushValue(-7);
                    selfRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
                }
                startFun = new LuaFunction(lua, startFunRef, selfRef);
            }
            float totalTime = lua.ToFloat(-5);
            float intervalTime = lua.ToFloat(-6);
            LuaTable luaTimer = null;
            if (lua.IsTable(-8))
            {
                lua.PushValue(-8);
                int timerRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
                luaTimer = new LuaTable(lua, timerRef);
            }

            int timerIndex = LuaTimerManager.GetInstance().AddTimer(luaTimer, intervalTime, totalTime,
                                                                                startFun, intervalFun, endFun, userData);
            lua.PushInteger(timerIndex);
            return 1;
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        private static int RemoveTimer(IntPtr l)
        {
            int timerIndex = lua.ToInteger(-1);
            LuaTimerManager.GetInstance().RemoveTimer(timerIndex);
            
            return 0;
        }
        
    }
}
