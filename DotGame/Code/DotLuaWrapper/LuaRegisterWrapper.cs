using System;
using Game.Core.DotLua;

namespace Game.Core.DotLuaWrapper
{
    public class LuaRegisterWrapper
    {
        static LuaState lua_;
        public static void RegisterToLua(LuaState lua)
        {
            lua_ = lua;
            string[] funcList = new string[]
            {
             "RegisterClass",
             "RegisterIngoreStatic",
            };

            LuaAPI.lua_CFunction[] funcDeList = new LuaAPI.lua_CFunction[]
            {
             RegisterClass,
             RegisterIngoreStatic,
            };
            LuaRegister.RegisterToLua(lua, "RegisterWrapper", funcList, funcDeList);
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        private static int RegisterClass(IntPtr l)
        {
            string className = lua_.ToString(-1);
            LuaRegister.RegisterClass(className);

            return 0;
        }

        [MonoPInvokeCallback(typeof(LuaAPI.lua_CFunction))]
        private static int RegisterIngoreStatic(IntPtr l)
        {
            string className = lua_.ToString(-1);
            LuaRegister.RegisterIngoreStatic(className);

            return 0;
        }
    }
}
