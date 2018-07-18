using Game.Core.DotLua;

namespace Game.Core.DotLuaWrapper
{
    public static class LuaWrapper
    {
        public static void PreWrapper(LuaState lua)
        {
            LuaRegisterWrapper.RegisterToLua(lua);
            DebugLoggerWrapper.RegisterToLua(lua);
            LuaTimerWrapper.RegisterToLua(lua);
        }
    }
}
