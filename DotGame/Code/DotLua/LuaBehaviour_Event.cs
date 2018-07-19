using UnityEngine;

namespace Game.Core.DotLua
{
    public partial class LuaBehaviour : MonoBehaviour
    {
        public void OnButtonClicked(string funcName)
        {
            if(string.IsNullOrEmpty(funcName))
            {
                return;
            }
            lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, classRef);
            lua.GetField(-1, funcName);
            if(lua.IsNil(-1))
            {
                lua.Pop(2);
            }else
            {
                lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
                lua.PCall(1, 0, 0);
                lua.Pop(1);
            }
        }
    }
}
