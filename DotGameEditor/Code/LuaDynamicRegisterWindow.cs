using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Game.Core.DotLuaEditor
{
    public class LuaDynamicRegisterWindow : EditorWindow
    {
        private class RegisterMethodInfo
        {
            public string orginName = "";
            public string registerName ="";
            public bool isStatic = false;
            public List<string> paramTypes = new List<string>();
        }

        [MenuItem("MHJ/Lua/Dynamic Register")]
        private static void DisplayWin()
        {
            LuaDynamicRegisterWindow win = EditorWindow.GetWindow<LuaDynamicRegisterWindow>();
            win.Show();
        }

        private string registerTypeName = "";
        private Vector2 scrollPos = Vector2.zero;
        private List<RegisterMethodInfo> registerMethods = new List<RegisterMethodInfo>();
        private string errorMsg = "";
        private string searchString = "";
        private SearchField searchField;

        void Awake()
        {
            searchField = new SearchField();
        }
        void OnGUI()
        {
            EditorGUIUtility.labelWidth = 110;
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar,GUILayout.Height(22));
                {
                    registerTypeName = EditorGUILayout.TextField("RegisterType:", registerTypeName, EditorStyles.toolbarTextField,GUILayout.ExpandWidth(true));
                    if(GUILayout.Button("Register",EditorStyles.toolbarButton,GUILayout.Width(120)))
                    {
                        Type type = GetType(registerTypeName);
                        errorMsg = "";
                        registerMethods.Clear();
                        if(type == null)
                        {
                            errorMsg = "Can't Found Class Type.name =" + registerTypeName;
                        }else
                        {
                            RegisterMethod(type);
                        }
                    }
                    searchString = searchField.OnToolbarGUI(searchString);
                }
                EditorGUILayout.EndHorizontal();
                if(string.IsNullOrEmpty(errorMsg))
                {
                    EditorGUILayout.LabelField("Register Method:");
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                    {
                        foreach(RegisterMethodInfo mInfo in registerMethods)
                        {
                            if (string.IsNullOrEmpty(searchString) || mInfo.orginName.ToLower().Contains(searchString.ToLower()))
                            {
                                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                {
                                    EditorGUILayout.TextField("Register Name:", mInfo.registerName);
                                    EditorGUILayout.LabelField("IsStatic:", mInfo.isStatic.ToString());
                                    EditorGUILayout.LabelField("Orgin Name:", mInfo.orginName);
                                    EditorGUILayout.LabelField("Param Type:", string.Join(",", mInfo.paramTypes.ToArray()));
                                }
                                EditorGUILayout.EndVertical();
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }else
                {
                    EditorGUILayout.HelpBox(errorMsg, MessageType.Error);
                }
                
            }
            EditorGUILayout.EndVertical();
            
        }

        void RegisterMethod(Type type)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            Dictionary<string, int> funcIndexDic = new Dictionary<string, int>();
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].IsGenericMethod)
                    continue;
                string funName = methods[i].Name;
                int fi = 0;
                if (funcIndexDic.TryGetValue(funName, out fi))
                {
                    funcIndexDic[funName] = (++fi);
                }
                else
                {
                    funcIndexDic[funName] = 0;
                }
                if (fi > 0)
                {
                    funName += fi.ToString();
                }

                RegisterMethodInfo info = new RegisterMethodInfo();
                registerMethods.Add(info);
                info.orginName = methods[i].Name;
                info.registerName = funName;
                info.isStatic = methods[i].IsStatic;

                ParameterInfo[] parInfos = methods[i].GetParameters();
                if(parInfos!=null && parInfos.Length>0)
                {
                    foreach(ParameterInfo pi in parInfos)
                    {
                        info.paramTypes.Add(pi.ParameterType.Name);
                    }
                }
            }
        }

        private Type GetType(string typeFullName)
        {
            Assembly[] assemblies  = AppDomain.CurrentDomain.GetAssemblies();
            if(assemblies!=null && assemblies.Length>0)
            {
                foreach(Assembly a in assemblies)
                {
                    Type type = a.GetType(typeFullName);
                    if(type!=null)
                    {
                        return type;
                    }
                }
            }
            return null;
        }

        
    }
}
