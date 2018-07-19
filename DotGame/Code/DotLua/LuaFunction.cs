using System;

namespace Game.Core.DotLua
{
    public class LuaFunction : IDisposable
    {
        private LuaState lua;
        private int funRef = LuaAPI.LUA_REFNIL;
        private int selfRef = LuaAPI.LUA_REFNIL;

        public LuaFunction(LuaState l,int fRef,int sRef):this(l,fRef)
        {
            selfRef = sRef;
        }

        public LuaFunction(LuaState l,int fRef)
        {
            lua = l;
            funRef = fRef;
        }

        public int FunRef
        {
            get { return funRef; }
        }

        public int SelfRef
        {
            get { return selfRef; }
        }

        public bool IsValid()
        {
            return funRef != LuaAPI.LUA_REFNIL;
        }

        private bool isDisposed = false;

        ~LuaFunction()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;
            
            if(lua != null && lua.GetLuaPtr() != IntPtr.Zero)
            {
                if (isDisposing)
                {
                    ///TODO::释放托管资源
                }

                if (funRef != LuaAPI.LUA_REFNIL)
                {
                    lua.L_Unref(LuaAPI.LUA_REGISTRYINDEX, ref funRef);
                }
                if (selfRef != LuaAPI.LUA_REFNIL)
                {
                    lua.L_Unref(LuaAPI.LUA_REGISTRYINDEX, ref selfRef);
                }
            }

            funRef = LuaAPI.LUA_REFNIL;
            selfRef = LuaAPI.LUA_REFNIL;
            lua = null;
            isDisposed = true;
        }

        public void Invoke()
        {
            if (funRef == LuaAPI.LUA_REFNIL)
                return;
            lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, funRef);
            if(selfRef!= LuaAPI.LUA_REFNIL)
            {
                lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, selfRef);
                lua.PCall(1, 0, 0);
            }else
            {
                lua.PCall(0, 0, 0);
            }
        }

        public void Invoke(object obj)
        {
            if (funRef == LuaAPI.LUA_REFNIL)
                return;
            lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, funRef);
            if (selfRef != LuaAPI.LUA_REFNIL)
            {
                lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, selfRef);
                if(obj!=null)
                {
                    lua.PushSystemObject(obj, typeof(object));
                    lua.PCall(2, 0, 0);
                }else
                {
                    lua.PCall(1, 0, 0);
                }
            }
            else
            {
                if(obj!=null)
                {
                    lua.PushSystemObject(obj, typeof(object));
                    lua.PCall(1, 0, 0);
                }else
                {
                    lua.PCall(0, 0, 0);
                }
            }
        }
    }
}
