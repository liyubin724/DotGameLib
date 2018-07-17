using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace GameEditor.Core.Util
{
    public delegate object DrawObjectInList(object obj, Type t);

    public static class GTEditorGUI
    {
        public static void DrawObjectListEditor<T>(string title, List<T> list, ref bool isFoldout, 
                                                    DrawObjectInList OnDrawObj,Color bgColor, bool isReadonly = false) where T : class
        {
            int delIndex = -1;
            int moveIndex = -1;
            int moveStep = 0;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    isFoldout = EditorGUILayout.Foldout(isFoldout, title, true);
                    if (!isReadonly)
                    {
                        Color tbgColor = GUI.backgroundColor;
                        GUI.backgroundColor = bgColor;
                        if (GUILayout.Button("+", GUILayout.Width(40)))
                        {
                            if (list == null)
                                list = new List<T>();

                            list.Add(default(T));
                        }
                        if (GUILayout.Button("-", GUILayout.Width(40)))
                        {
                            if (list != null && list.Count > 0)
                                list.RemoveAt(list.Count - 1);
                        }
                        GUI.backgroundColor = tbgColor;
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (isFoldout)
                {
                    EditorGUI.indentLevel++;
                    if (list == null)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            EditorGUILayout.LabelField("The list is NULL");
                        }
                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                            {
                                EditorGUILayout.LabelField("" + (i + 1), GUILayout.Width(40));
                                if (OnDrawObj != null)
                                {
                                    if (isReadonly)
                                    {
                                        OnDrawObj(list[i], typeof(T));
                                    }
                                    else
                                    {
                                        list[i] = (T)OnDrawObj(list[i], typeof(T));
                                    }
                                }
                                if (!isReadonly)
                                {
                                    Color tbgColor = GUI.backgroundColor;
                                    GUI.backgroundColor = bgColor;
                                    EditorGUILayout.BeginVertical(GUILayout.Width(50));
                                    {
                                        if (GUILayout.Button("-", GUILayout.Width(40)))
                                        {
                                            delIndex = i;
                                        }
                                        if (i != 0)
                                        {
                                            if (GUILayout.Button("\u25B2", GUILayout.Width(40)))
                                            {
                                                moveIndex = i;
                                                moveStep = -1;
                                            }
                                        }
                                        if (i != list.Count - 1)
                                        {
                                            if (GUILayout.Button("\u25BC", GUILayout.Width(40)))
                                            {
                                                moveIndex = i;
                                                moveStep = 1;
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndVertical();
                                    GUI.backgroundColor = tbgColor;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndVertical();

            if (list != null && delIndex >= 0 && delIndex < list.Count)
            {
                list.RemoveAt(delIndex);
            }
            if(list!=null && moveIndex>=0 && moveStep != 0)
            {
                int targetIndex = moveIndex + moveStep;
                T moveObj = list[moveIndex];
                T targetObj = list[targetIndex];
                list[moveIndex] = targetObj;
                list[targetIndex] = moveObj;
            }
            delIndex = -1;
            moveIndex = -1;
            moveStep = 0;
        }
    }
}


