using Game.Core.Util;
using System;
using UnityEngine;

namespace Game.Core.DotLua
{
    [Serializable]
    public class RegisterToLuaObject
    {
        public string name;
        public GameObject obj;
        public UnityEngine.Object regObj;
        public string typeName;
    }

    [Serializable]
    public class RegisterToLuaObjectArr
    {
        [NonSerialized]
        public bool isFoldout = true;

        public string name;
        public RegisterToLuaObject[] luaObjects;
    }
    
    [Serializable]
    public class RegisterToLuaBehaviour
    {
        public string name;
        public LuaBehaviour behaviour;
    }

    [Serializable]
    public class RegisterToLuaBehaviourArr
    {
        [NonSerialized]
        public bool isFoldout = true;

        public string name;
        public RegisterToLuaBehaviour[] luaBehaviours;
    }

    public partial class LuaBehaviour : MonoBehaviour
    {
        public string scriptShortPath; 
        public string scriptName;

        public RegisterToLuaObject[] regLuaObject;
        public RegisterToLuaObjectArr[] regLuaObjectArr;

        public RegisterToLuaBehaviour[] regLuaBehaviour;
        public RegisterToLuaBehaviourArr[] regLuaBehaviourArr;

        public bool IsOnClickPassGameObject = false;

        protected int awakeFunRef = LuaAPI.LUA_REFNIL;
        protected int startFunRef = LuaAPI.LUA_REFNIL;
        protected int destoryFunRef = LuaAPI.LUA_REFNIL;

        private int objRef = LuaAPI.LUA_REFNIL;
        public int ObjectRef
        {
            get { return objRef; }
        }

        private int classRef = LuaAPI.LUA_REFNIL;
        public int ClassRef
        {
            get { return classRef; }
        }

        private bool isInited = false;
        public void InitLua()
        {
            if (isInited)
                return;

            LuaState lua_ = LuaInstance.instance.Get();
            if (lua_ != null)
            {
                lua_.NewTable();
                objRef = lua_.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
            }
            isInited = true;
        }

        void Awake()
        {
            InitLua();
            if(string.IsNullOrEmpty(scriptName))
            {
                DebugLogger.LogError(string.Format("LuaBehaviour::Awake->scriptName is NULL!"));
                return;
            }

            LuaState lua = LuaInstance.instance.Get();
            lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
            lua.GetGlobal(scriptName);
            if(!lua.IsTable(-1))
            {
                lua.Pop(1);
                if (!string.IsNullOrEmpty(scriptShortPath))
                {
                    LuaInstance.instance.DoFile(scriptShortPath);
                    lua.GetGlobal(scriptName);
                    if(lua.IsTable(-1))
                    {
                        lua.PushValue(-1);
                        classRef = lua.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
                    }else
                    {
                        lua.Pop(2);
                        DebugLogger.LogError(string.Format("LuaBehaviour::Awake->Top is not a table!"));
                        return;
                    }
                }
                else
                {
                    DebugLogger.LogError(string.Format("LuaBehaviour::Awake->scriptShortPath is NULL!"));
                    lua.Pop(1);
                    return;
                }
            }

            if(lua.GetMetaTable(-1))
            {
                lua.Pop(1);
            }else
            {
                lua.PushValue(-1);
                lua.SetField(-2, "__index");
                lua.SetMetaTable(-2);
            }

            RefBehaviourFuncs();
            RegisterLuaBehaviour();
            RegisterLuaObject();
            RegisterLuaBehaviourArr();
            RegisterLuaObjectArr();

            lua.NewClassUserData(gameObject);
            lua.SetField(-2, "gameObject");
            lua.NewClassUserData(transform);
            lua.SetField(-2, "transform");

            lua.Pop(1);

            if (awakeFunRef != LuaAPI.LUA_REFNIL)
                CallFunction(awakeFunRef);
        }

        void RefBehaviourFuncs()
        {
            if (classRef == LuaAPI.LUA_REFNIL)
                return;

            LuaState lua_ = LuaInstance.instance.Get();
            lua_.RawGetI(LuaAPI.LUA_REGISTRYINDEX, classRef);
            lua_.GetField(-1, "Awake");
            awakeFunRef = lua_.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
            lua_.GetField(-1, "Start");
            startFunRef = lua_.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
            lua_.GetField(-1, "OnDestroy");
            destoryFunRef = lua_.L_Ref(LuaAPI.LUA_REGISTRYINDEX);
            lua_.Pop(1);
        }

        void RegisterLuaBehaviour()
        {
            if (regLuaBehaviour != null && regLuaBehaviour.Length > 0)
            {
                LuaState lua = LuaInstance.instance.Get();
                for (int i = 0; i < regLuaBehaviour.Length; i++)
                {
                    if (regLuaBehaviour[i].behaviour == null)
                    {
                        DebugLogger.LogError("LuaBehaviour::RegisterLuaBehaviour->behaviour is null.objName = " + name + "  index = " + i);
                        continue;
                    }
                    regLuaBehaviour[i].behaviour.InitLua();
                    
                    lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, regLuaBehaviour[i].behaviour.ObjectRef);
                    lua.SetField(-2, regLuaBehaviour[i].name);
                }
            }
        }

        void RegisterLuaBehaviourArr()
        {
            if (regLuaBehaviourArr != null && regLuaBehaviourArr.Length > 0)
            {
                LuaState lua = LuaInstance.instance.Get();
                for(int i =0;i<regLuaBehaviourArr.Length;i++)
                {
                    if(string.IsNullOrEmpty(regLuaBehaviourArr[i].name))
                    {
                        DebugLogger.LogError("LuaBehaviour::RegisterLuaBehaviourArr->Group Name is Null, index = " + i);
                        continue;
                    }

                    lua.NewTable();

                    lua.PushValue(-1);
                    lua.SetField(-3, regLuaBehaviourArr[i].name);

                    RegisterToLuaBehaviour[] behs = regLuaBehaviourArr[i].luaBehaviours;
                    if(behs!=null && behs.Length>0)
                    {
                        for(int j =0;j<behs.Length;j++)
                        {
                            if(behs[j].behaviour==null)
                            {
                                DebugLogger.LogError("LuaBehaviour::RegisterLuaBehaviourArr->behaviour is Null, index = " + j);
                                continue;
                            }
                            behs[j].behaviour.InitLua();

                            lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, behs[j].behaviour.ObjectRef);
                            lua.RawSetI(-2, j + 1);
                        }
                    }
                    lua.Pop(1);
                }
            }
        }

        void RegisterLuaObject()
        {
            LuaState lua_ = LuaInstance.instance.Get();
            for (int i = 0; i < regLuaObject.Length; i++)
            {
                if (regLuaObject[i].obj == null || regLuaObject[i].regObj == null)
                {
                    DebugLogger.LogError("LuaBehaviour::RegisterLuaObjects->obj or regObj is Null");
                    continue;
                }
                string regName = regLuaObject[i].name;
                if (string.IsNullOrEmpty(regName))
                {
                    regName = regLuaObject[i].regObj.name;
                }

                Type regType = regLuaObject[i].regObj.GetType();
                LuaRegister.RegisterType(regType);

                lua_.RawGetI(LuaAPI.LUA_REGISTRYINDEX, objRef);
                lua_.NewClassUserData(regLuaObject[i].regObj);
                lua_.SetField(-2, regName);
                lua_.Pop(1);
            }
        }

        void RegisterLuaObjectArr()
        {
            if(regLuaObjectArr!=null && regLuaObjectArr.Length>0)
            {
                LuaState lua = LuaInstance.instance.Get();
                for(int i =0;i<regLuaObjectArr.Length;i++)
                {
                    if (string.IsNullOrEmpty(regLuaObjectArr[i].name))
                    {
                        DebugLogger.LogError("LuaBehaviour::RegisterLuaObjectArr->Group Name is Null, index = " + i);
                        continue;
                    }
                    lua.NewTable();

                    RegisterToLuaObject[] luaObjs = regLuaObjectArr[i].luaObjects;
                    if(luaObjs != null && luaObjs.Length>0)
                    {
                        for(int j = 0;j<luaObjs.Length;j++)
                        {
                            if(luaObjs[j].regObj == null)
                            {
                                DebugLogger.LogError("LuaBehaviour::RegisterLuaObjectArr->obj or regObj is Null");
                                continue;
                            }
                            Type regType = luaObjs[j].regObj.GetType();
                            LuaRegister.RegisterType(regType);
                            lua.NewClassUserData(luaObjs[j].regObj);
                            lua.RawSetI(-2, j + 1);
                        }
                    }
                    lua.SetField(-2, regLuaObjectArr[i].name);
                }
            }
        }

        void Start()
        {
            if (startFunRef != LuaAPI.LUA_REFNIL)
                CallFunction(startFunRef);
        }

        void OnDestroy()
        {
            LuaState lua = LuaInstance.instance.Get();
            if (lua == null)
                return;

            if (destoryFunRef != LuaAPI.LUA_REFNIL)
                CallFunction(destoryFunRef);

            lua.L_Unref(LuaAPI.LUA_REGISTRYINDEX, ref awakeFunRef);
            lua.L_Unref(LuaAPI.LUA_REGISTRYINDEX, ref startFunRef);
            lua.L_Unref(LuaAPI.LUA_REGISTRYINDEX, ref destoryFunRef);
            lua.L_Unref(LuaAPI.LUA_REGISTRYINDEX, ref objRef);
            lua.L_Unref(LuaAPI.LUA_REGISTRYINDEX, ref classRef);
        }
    }
}