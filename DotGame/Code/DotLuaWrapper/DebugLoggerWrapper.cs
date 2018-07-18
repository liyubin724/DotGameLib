using Game.Core.DotLua;
using Game.Core.Util;
using System;

namespace Game.Core.DotLuaWrapper
{
    public static class DebugLoggerWrapper
    {
        static LuaState lua;
        public static void RegisterToLua(LuaState l)
        {
            lua = l;
            string[] funcList = new string[]
            {
             "Log",
             "LogError",
             "LogWarning",
            };

            LuaAPI.lua_CFunction[] funcDeList = new LuaAPI.lua_CFunction[]
            {
             Log,
             LogError,
             LogWarning,
            };
            LuaRegister.RegisterToLua(l, "DebugLogger", funcList, funcDeList);
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        public static int Log(IntPtr l)
        {
            string res = GetLogInfo();
            DebugLogger.Log(res);
            return 0;
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        public static int LogError(IntPtr l)
        {
            string res = GetLogInfo();
            DebugLogger.LogError(res);
            return 0;
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        public static int LogWarning(IntPtr l)
        {
            string res = GetLogInfo();
            DebugLogger.LogWarning(res);
            return 0;
        }
        
        public static string GetLogInfo(int stackNum = 4)
        {
            int idx = -1;
            LuaType paraType = lua.Type(idx);

            object p = null;
            if (paraType == LuaType.LUA_TNUMBER)
            {
                p = (float)lua.ToNumber(idx);
            }
            else if (paraType == LuaType.LUA_TSTRING)
            {
                p = lua.ToString(idx);
            }
            else if (paraType == LuaType.LUA_TBOOLEAN)
            {
                p = lua.ToBoolean(idx);
            }
            else if (paraType == LuaType.LUA_TUSERDATA)
            {
                p = lua.ToUserDataObject(idx);
            }
            else if (paraType == LuaType.LUA_TNIL)
            {
                p = "Nil";
            }
            else if (paraType == LuaType.LUA_TTABLE)
            {
                p = "Table Can't be Log.";
            }

            lua.GetGlobal("debug");
            lua.GetField(-1, "getinfo");
            lua.PushNumber(stackNum);
            lua.PCall(1, 1, 0);
            string stackInfo = string.Empty;
            if (!lua.IsNil(-1))
            {
                lua.GetField(-1, "source");
                lua.GetField(-2, "currentline");

                stackInfo = lua.ToString(-2);
                stackInfo += lua.ToString(-1);
                lua.Pop(4);
            }
            else
            {
                lua.Pop(2);
            }

            lua.GetGlobal("lua_get_debug_info");
            lua.PCall(0, 1, 0);
            string fullStackInfo = lua.ToString(-1);
            lua.Pop(1);

            string res = string.Empty;

            if (p != null)
            {
                res = p.ToString() + System.Environment.NewLine + stackInfo + System.Environment.NewLine + "StackInfo:\n" + fullStackInfo;
            }
            else
            {
                DebugLogger.LogError("call LuaDebugError: " + paraType + " stack:" + fullStackInfo + System.Environment.NewLine + LuaInstance.ConstructString(lua));
            }

            return res;
        }
    }
}
    

