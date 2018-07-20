using Game.Core.DotLua;
using GameEditor.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GameEditor.Core.DotLua
{
    [CustomEditor(typeof(LuaBehaviour))]
    public class LuaBehaviourEditor : Editor
    {
        private UnityEngine.Object monoScript = null;
        private List<RegisterToLuaObject> luaObjList = new List<RegisterToLuaObject>();
        private List<RegisterToLuaBehaviour> luaBehList = new List<RegisterToLuaBehaviour>();

        private List<RegisterToLuaBehaviourArr> luaBehArrList = new List<RegisterToLuaBehaviourArr>();
        private List<RegisterToLuaObjectArr> luaObjArrList = new List<RegisterToLuaObjectArr>();

        LuaBehaviour behaviour;
        private string scriptAssetPath = "";
        private string LuaScriptPathDir = "Assets/Scripts/LuaScripts/";
        public void Awake()
        {
            behaviour = target as LuaBehaviour;
            monoScript = MonoScript.FromMonoBehaviour(behaviour);
            if(behaviour.regLuaObject!=null && behaviour.regLuaObject.Length>0)
            {
                luaObjList.AddRange(behaviour.regLuaObject);
            }

            if(behaviour.regLuaBehaviour!=null && behaviour.regLuaBehaviour.Length>0)
            {
                luaBehList.AddRange(behaviour.regLuaBehaviour);
            }

            if(behaviour.regLuaBehaviourArr!=null && behaviour.regLuaBehaviourArr.Length>0)
            {
                luaBehArrList.AddRange(behaviour.regLuaBehaviourArr);
            }

            if(behaviour.regLuaObjectArr!=null && behaviour.regLuaObjectArr.Length>0)
            {
                luaObjArrList.AddRange(behaviour.regLuaObjectArr);
            }

            if(!string.IsNullOrEmpty(behaviour.scriptShortPath))
            {
                scriptAssetPath = LuaScriptPathDir + behaviour.scriptShortPath;
            }
        }

        private bool isLuaObjFoldout = true;
        private bool isLuaObjArrFoldout = true;
        private bool isLuaBehFoldout = true;
        private bool isLuaBehArrFoldout = true;
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            {
                EditorGUILayout.ObjectField("Script:", monoScript, typeof(MonoScript), false);
            }
            EditorGUI.EndDisabledGroup();

            TextAsset luaScriptText = null;
            if(!string.IsNullOrEmpty(scriptAssetPath))
            {
                luaScriptText = (TextAsset)AssetDatabase.LoadAssetAtPath(scriptAssetPath, typeof(TextAsset));
                if(luaScriptText == null)
                {
                    scriptAssetPath = "";
                }
            }
            TextAsset ta = (TextAsset)EditorGUILayout.ObjectField("Lua Script:", luaScriptText, typeof(TextAsset),false);
            if(ta == null)
            {
                if(!string.IsNullOrEmpty(behaviour.scriptShortPath))
                {
                    behaviour.scriptShortPath = "";
                    behaviour.scriptName = "";
                }
                scriptAssetPath = "";
            }else
            {
                string taPath = AssetDatabase.GetAssetPath(ta.GetInstanceID()).Replace("\\", "/");
                if(taPath != scriptAssetPath && taPath.IndexOf(LuaScriptPathDir)>=0)
                {
                    scriptAssetPath = taPath;

                    behaviour.scriptShortPath = scriptAssetPath.Replace(LuaScriptPathDir, "");
                    behaviour.scriptName = Path.GetFileNameWithoutExtension(scriptAssetPath);
                }
            }


            GTEditorGUI.DrawObjectListEditor<RegisterToLuaObject>("Register Lua Object:",
                                                                    luaObjList, ref isLuaObjFoldout,
                                                                    OnDrawRegisterLuaObject, Color.red);

            EditorGUILayout.Space();
            GTEditorGUI.DrawObjectListEditor<RegisterToLuaObjectArr>("Register Lua Object Arr:",
                                                                   luaObjArrList, ref isLuaObjArrFoldout,
                                                                   OnDrawRegisterLuaObjectArr, Color.red);

            EditorGUILayout.Space();
            GTEditorGUI.DrawObjectListEditor<RegisterToLuaBehaviour>("Register Lua Behaviour:",
                                                                   luaBehList, ref isLuaBehFoldout,
                                                                   OnDrawRegisterLuaBehaviour, Color.red);

            EditorGUILayout.Space();
            GTEditorGUI.DrawObjectListEditor<RegisterToLuaBehaviourArr>("Register Lua Behaviour Arr:",
                                                                   luaBehArrList, ref isLuaBehArrFoldout,
                                                                   OnDrawRegisterLuaBehaviourArr, Color.red);
            if (GUI.changed)
            {
                behaviour.regLuaObject = luaObjList.ToArray();
                behaviour.regLuaObjectArr = luaObjArrList.ToArray();
                behaviour.regLuaBehaviour = luaBehList.ToArray();
                behaviour.regLuaBehaviourArr = luaBehArrList.ToArray();
            }
        }

        private string nameReg = @"^[a-z][a-zA-Z0-9]*";

        private RegisterToLuaObject OnDrawRegisterLuaObject(object obj, Type t)
        {
            if(obj == null)
            {
                obj = new RegisterToLuaObject();
            }
            RegisterToLuaObject luaObject = obj as RegisterToLuaObject;
            EditorGUILayout.BeginVertical();
            {
                luaObject.name = EditorGUILayout.TextField("Name:", luaObject.name);
                luaObject.obj = (GameObject)EditorGUILayout.ObjectField("Game Object:", luaObject.obj, typeof(GameObject), true);
                if(luaObject.obj!=null)
                {
                    if(string.IsNullOrEmpty(luaObject.name))
                    {
                        luaObject.name = luaObject.obj.name;
                    }
                    List<string> compList = new List<string>();
                    
                    Component[] coms = luaObject.obj.GetComponents(typeof(Component));
                    bool isRepeat = false;
                    foreach(Component c in coms)
                    {
                        string name = c.GetType().Name;
                        if(compList.IndexOf(name)>=0)
                        {
                            isRepeat = true;
                        }else
                        {
                            if(name != typeof(LuaBehaviour).Name)
                            {
                                compList.Add(name);
                            }
                        }
                    }
                    compList.Sort();
                    compList.Insert(0, "GameObject");
                    int selectIndex = -1;
                    if(!string.IsNullOrEmpty(luaObject.typeName))
                    {
                        selectIndex = compList.IndexOf(luaObject.typeName);
                    }
                    if (selectIndex < 0) selectIndex = 0;
                    selectIndex = EditorGUILayout.Popup("Type:", selectIndex, compList.ToArray());
                    string targetTypeName = compList[selectIndex];
                    if(targetTypeName != luaObject.typeName)
                    {
                        luaObject.typeName = targetTypeName;
                        if(targetTypeName == "GameObject")
                        {
                            luaObject.regObj = luaObject.obj;
                        }else
                        {
                            luaObject.regObj = luaObject.obj.GetComponent(luaObject.typeName);
                        }
                    }

                    if(isRepeat)
                    {
                        EditorGUILayout.HelpBox("需要注册的GameObject上有重复的组件", MessageType.Error);
                    }
                }
                
                if(luaObject.obj == null)
                {
                    EditorGUILayout.HelpBox("请选择需要注册的GameObject", MessageType.Error);
                }
                if (string.IsNullOrEmpty(luaObject.name))
                {
                    EditorGUILayout.HelpBox("注册luaObject名称不能为空", MessageType.Error);
                }
                if (!string.IsNullOrEmpty(luaObject.name) && !Regex.IsMatch(luaObject.name, nameReg))
                {
                    EditorGUILayout.HelpBox("注册luaObject名称只能以小写字母开关，只能包括A-Z,a-z,0-9的字符", MessageType.Error);
                }
                if(!string.IsNullOrEmpty(luaObject.name))
                {
                    bool isNameRepeat = false;
                    foreach(RegisterToLuaObject rtlo in luaObjList)
                    {
                        if(rtlo!=null && rtlo!=luaObject && rtlo.name == luaObject.name && !string.IsNullOrEmpty(luaObject.name))
                        {
                            isNameRepeat = true;
                        }
                    }
                    if(isNameRepeat)
                    {
                        EditorGUILayout.HelpBox("注册luaObject名称重复", MessageType.Error);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            return luaObject;
        }

        private RegisterToLuaObjectArr OnDrawRegisterLuaObjectArr(object obj, Type t)
        {
            if (obj == null)
            {
                obj = new RegisterToLuaObjectArr();
            }
            RegisterToLuaObjectArr luaObjArr = obj as RegisterToLuaObjectArr;
            EditorGUILayout.BeginVertical();
            {
                luaObjArr.name = EditorGUILayout.TextField("Name:", luaObjArr.name);
                if (string.IsNullOrEmpty(luaObjArr.name))
                {
                    EditorGUILayout.HelpBox("注册Object名称不能为空", MessageType.Error);
                }
                if (!string.IsNullOrEmpty(luaObjArr.name) && !Regex.IsMatch(luaObjArr.name, nameReg))
                {
                    EditorGUILayout.HelpBox("注册Object名称只能以小写字母开关，只能包括A-Z,a-z,0-9的字符", MessageType.Error);
                }

                List<RegisterToLuaObject> childs = new List<RegisterToLuaObject>();
                if (luaObjArr.luaObjects != null && luaObjArr.luaObjects.Length > 0)
                {
                    childs.AddRange(luaObjArr.luaObjects);
                }
                GTEditorGUI.DrawObjectListEditor<RegisterToLuaObject>("Register Lua Object:",
                                                                   childs, ref luaObjArr.isFoldout,
                                                                   OnDrawRegisterLuaObjectInArr, Color.green);
                luaObjArr.luaObjects = childs.ToArray();

                if (!string.IsNullOrEmpty(luaObjArr.name))
                {
                    bool isNameRepeat = false;
                    foreach (RegisterToLuaObjectArr rtloa in luaObjArrList)
                    {
                        if (rtloa != null && rtloa != luaObjArr && rtloa.name == luaObjArr.name && !string.IsNullOrEmpty(luaObjArr.name))
                        {
                            isNameRepeat = true;
                        }
                    }
                    if (isNameRepeat)
                    {
                        EditorGUILayout.HelpBox("注册luaObjectArr名称重复", MessageType.Error);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            return luaObjArr;
        }

        private RegisterToLuaObject OnDrawRegisterLuaObjectInArr(object obj, Type t)
        {
            if (obj == null)
            {
                obj = new RegisterToLuaObject();
            }
            RegisterToLuaObject luaObject = obj as RegisterToLuaObject;
            EditorGUILayout.BeginVertical();
            {
                luaObject.obj = (GameObject)EditorGUILayout.ObjectField("Game Object:", luaObject.obj, typeof(GameObject), true);
                if (luaObject.obj != null)
                {
                    List<string> compList = new List<string>();

                    Component[] coms = luaObject.obj.GetComponents(typeof(Component));
                    bool isRepeat = false;
                    foreach (Component c in coms)
                    {
                        string name = c.GetType().Name;
                        if (compList.IndexOf(name) >= 0)
                        {
                            isRepeat = true;
                        }
                        else
                        {
                            if (name != typeof(LuaBehaviour).Name)
                            {
                                compList.Add(name);
                            }
                        }
                    }
                    compList.Sort();
                    compList.Insert(0, "GameObject");
                    int selectIndex = -1;
                    if (!string.IsNullOrEmpty(luaObject.typeName))
                    {
                        selectIndex = compList.IndexOf(luaObject.typeName);
                    }
                    if (selectIndex < 0) selectIndex = 0;
                    selectIndex = EditorGUILayout.Popup("Type:", selectIndex, compList.ToArray());
                    string targetTypeName = compList[selectIndex];
                    if (targetTypeName != luaObject.typeName)
                    {
                        luaObject.typeName = targetTypeName;
                        if (targetTypeName == "GameObject")
                        {
                            luaObject.regObj = luaObject.obj;
                        }
                        else
                        {
                            luaObject.regObj = luaObject.obj.GetComponent(luaObject.typeName);
                        }
                    }

                    if (isRepeat)
                    {
                        EditorGUILayout.HelpBox("需要注册的GameObject上有重复的组件", MessageType.Error);
                    }
                }

                if (luaObject.obj == null)
                {
                    EditorGUILayout.HelpBox("请选择需要注册的GameObject", MessageType.Error);
                }
            }
            EditorGUILayout.EndVertical();

            return luaObject;
        }

        private RegisterToLuaBehaviour OnDrawRegisterLuaBehaviour(object obj, Type t)
        {
            if(obj == null)
            {
                obj = new RegisterToLuaBehaviour();
            }
            RegisterToLuaBehaviour luaBeh = obj as RegisterToLuaBehaviour;
            EditorGUILayout.BeginVertical();
            {
                luaBeh.name = EditorGUILayout.TextField("Name:", luaBeh.name);
                luaBeh.behaviour = (LuaBehaviour)EditorGUILayout.ObjectField("Behaviour:",luaBeh.behaviour, typeof(LuaBehaviour),true);

                if(luaBeh.behaviour!=null && string.IsNullOrEmpty(luaBeh.name))
                {
                    luaBeh.name = luaBeh.behaviour.name;
                }
                if(luaBeh.behaviour == null)
                {
                    EditorGUILayout.HelpBox("注册Behaviour不能为空", MessageType.Error);
                }
                if (string.IsNullOrEmpty(luaBeh.name))
                {
                    EditorGUILayout.HelpBox("注册Behaviour名称不能为空", MessageType.Error);
                }
                if(!string.IsNullOrEmpty(luaBeh.name) && !Regex.IsMatch(luaBeh.name,nameReg))
                {
                    EditorGUILayout.HelpBox("注册Behaviour名称只能以小写字母开关，只能包括A-Z,a-z,0-9的字符", MessageType.Error);
                }

                if (!string.IsNullOrEmpty(luaBeh.name))
                {
                    bool isNameRepeat = false;
                    foreach (RegisterToLuaBehaviour rtlb in luaBehList)
                    {
                        if (rtlb != null && rtlb != luaBeh && rtlb.name == luaBeh.name && !string.IsNullOrEmpty(luaBeh.name))
                        {
                            isNameRepeat = true;
                        }
                    }
                    if (isNameRepeat)
                    {
                        EditorGUILayout.HelpBox("注册luaObjectArr名称重复", MessageType.Error);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            return luaBeh;
        }

        private RegisterToLuaBehaviourArr OnDrawRegisterLuaBehaviourArr(object obj, Type t)
        {
            if(obj == null)
            {
                obj = new RegisterToLuaBehaviourArr();
            }

            RegisterToLuaBehaviourArr luaBehArr = obj as RegisterToLuaBehaviourArr;
            EditorGUILayout.BeginVertical();
            {
                luaBehArr.name = EditorGUILayout.TextField("Name:", luaBehArr.name);
                if (string.IsNullOrEmpty(luaBehArr.name))
                {
                    EditorGUILayout.HelpBox("注册Behaviour名称不能为空", MessageType.Error);
                }
                if (!string.IsNullOrEmpty(luaBehArr.name) && !Regex.IsMatch(luaBehArr.name, nameReg))
                {
                    EditorGUILayout.HelpBox("注册Behaviour名称只能以小写字母开关，只能包括A-Z,a-z,0-9的字符", MessageType.Error);
                }
                List<RegisterToLuaBehaviour> childs = new List<RegisterToLuaBehaviour>();
                if(luaBehArr.luaBehaviours!=null && luaBehArr.luaBehaviours.Length>0)
                {
                    childs.AddRange(luaBehArr.luaBehaviours);
                }
                GTEditorGUI.DrawObjectListEditor<RegisterToLuaBehaviour>("Register Lua Behaviour:",
                                                                   childs, ref luaBehArr.isFoldout,
                                                                   OnDrawRegisterLuaBehaviourInArr, Color.green);
                luaBehArr.luaBehaviours = childs.ToArray();

                if (!string.IsNullOrEmpty(luaBehArr.name))
                {
                    bool isNameRepeat = false;
                    foreach (RegisterToLuaBehaviourArr rtlba in luaBehArrList)
                    {
                        if (rtlba != null && rtlba != luaBehArr && rtlba.name == luaBehArr.name && !string.IsNullOrEmpty(luaBehArr.name))
                        {
                            isNameRepeat = true;
                        }
                    }
                    if (isNameRepeat)
                    {
                        EditorGUILayout.HelpBox("注册luaObjectArr名称重复", MessageType.Error);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            return luaBehArr;
        }

        private RegisterToLuaBehaviour OnDrawRegisterLuaBehaviourInArr(object obj, Type t)
        {
            if (obj == null)
            {
                obj = new RegisterToLuaBehaviour();
            }
            RegisterToLuaBehaviour luaBeh = obj as RegisterToLuaBehaviour;
            EditorGUILayout.BeginVertical();
            {
                luaBeh.behaviour = (LuaBehaviour)EditorGUILayout.ObjectField("Behaviour:", luaBeh.behaviour, typeof(LuaBehaviour), true);
                if (luaBeh.behaviour == null)
                {
                    EditorGUILayout.HelpBox("注册Behaviour不能为空", MessageType.Error);
                }
            }
            EditorGUILayout.EndVertical();
            return luaBeh;
        }
    }
}