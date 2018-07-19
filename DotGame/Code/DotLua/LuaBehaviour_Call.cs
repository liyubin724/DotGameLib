using Game.Core.DotLua;
using Game.Core.Util;
using UnityEngine;

namespace Game.Core.DotLua
{
    public partial class LuaBehaviour : MonoBehaviour
    {
        public void CallFunction(string funcName)
        {
            LuaState lua_ = LuaInstance.Instance.Get();
            lua_.RawGetI(LuaAPI.LUA_REGISTRYINDEX, classRef);
            lua_.GetField(-1, funcName);
            if (lua_.IsFunction(-1))
            {
                lua_.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
                lua_.PCall(1, 0, 0);
                lua_.Pop(1);
            }
            else
            {
                DebugLogger.LogError(funcName + " not exist.");
                lua_.Pop(2);
            }
        }

        public void CallFunction<T>(string funcName, T arg)
        {
            LuaState lua_ = LuaInstance.Instance.Get();
            lua_.RawGetI(LuaAPI.LUA_REGISTRYINDEX, classRef);
            lua_.GetField(-1, funcName);
            if (lua_.IsFunction(-1))
            {
                lua_.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
                lua_.PushObj(arg);
                lua_.PCall(2, 0, 0);
                lua_.Pop(1);
            }
            else
            {
                DebugLogger.LogError(funcName + " not exist.");
                lua_.Pop(2);
            }
        }

        private void CallFunction(int funcRef)
        {
            LuaState lua_ = LuaInstance.Instance.Get();
            lua_.RawGetI(LuaAPI.LUA_REGISTRYINDEX, funcRef);
            lua_.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
            lua_.PCall(1, 0, 0);
        }
    }
}
