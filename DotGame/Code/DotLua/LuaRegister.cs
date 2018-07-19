using Game.Core.Util;
using System;

namespace Game.Core.DotLua
{
    public interface ITypeReflect
    {
        Type GetType(string className);
    }

    public class LuaRegister
    {
        public static ITypeReflect typeReflect;

        public static void RegisterToLua(LuaState lua , string name, string[] funcList, LuaAPI.lua_CFunction[] funcDeList)
        {
            lua.NewTable();
            lua.PushValue(-1);
            lua.SetField(-1, "__index");

            for (int i = 0; i < funcList.Length; i++)
            {
                lua.PushLuaClosure(funcDeList[i], 0);
                lua.SetField(-2, funcList[i]);
            }

            lua.SetGlobal(name);
        }

        public static void RegisterClass(string className)
        {
            Type type = GetGameClassType(className);
            if(type != null)
            {
                RegisterType(type);
            }
            else
            {
                DebugLogger.LogError(className + " is null.");
            } 
        }
        public static void RegisterType(Type type)
        {
            LuaInstance.Instance.RegisterData.RegisterToLua(type);
        }

        public static void RegisterIngoreStatic(string className)
        {
            Type type = GetGameClassType(className);
            if (type != null)
            {
                LuaInstance.Instance.RegisterData.AddIngoreStaticRegister(type);
            }
            else
            {
                DebugLogger.LogError(className + " is null.");
            } 
        }

        public static Type GetGameClassType(string className)
        {
            if(typeReflect != null)
                return typeReflect.GetType(className);
            return null;
        }
    }
}
