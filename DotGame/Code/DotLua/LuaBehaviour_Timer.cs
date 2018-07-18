using UnityEngine;
using Game.Core.Util;

namespace Game.Core.DotLua
{
    public partial class LuaBehaviour : MonoBehaviour
    {
        public void RegisterTimerAction()
        {
            lua.GetGlobal("LuaTimer");
            if(lua.IsNil(-1))
            {
                DebugLogger.LogError("LuaBehaviour::RegisterTimerAction->");   
            }else
            {
                lua.GetField(-1, "Create");
                lua.PCall(0, 1, 0);

                lua.SetField(-3, "timer");
            }
            lua.Pop(1);
        }

        public void DisposeTimer()
        {
            lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
            lua.GetField(-1, "timer");
            lua.GetField(-1, "Clear");
            lua.PushValue(-2);
            lua.PCall(1, 0, 0);

            lua.Pop(2);
        }
    }
}
