using UnityEngine; 
using UnityEditor; 
using System.Linq; 
using System.Collections.Generic;

namespace ArtIsDark
{
    [CustomEditor(typeof(ShaderKeywordsTool))]
    public class ShaderKeywordsToolInspector : BBEditor<ShaderKeywordsTool>
    {
        override public string Title { get { return "ShaderKeywordsTool"; } }
        override public int MajorVersion { get { return 1; } }
        override public int MinorVersion { get { return 0; } }
        private List<string> sharedKeywordsList = new List<string>();
        private ShaderKeywords shaderKeywords;
        private Color selectedColor = new Color(.54f, .54f, .42f, 1f);

        override public void BBEditorGUI()
        {
            int count;
            GUI.backgroundColor = BB.buttonColor;
            if (GUILayout.Button("\r\nScan Project for Shader Keywords\r\n", BB.buttonStyleThick))
            {
                BBTarget.scanForKeywords();
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);

            if (BBTarget.globalKeywordsFound.Count > 0)
            {
                GUILayout.Label("Keywords found in project:" + BBTarget.globalKeywordsFound.Count);
                GUILayout.Space(10);


                if (!BBTarget.showProjectKeywords)
                {
                    GUI.backgroundColor = BB.buttonColor;
                    if (GUILayout.Button("Show Project Keywords", BB.buttonStyleThick))
                    {
                        BBTarget.showProjectKeywords = true;
                    }

                }
                else
                {
                    GUI.backgroundColor = BB.buttonColor;
                    if (GUILayout.Button("Hide Project Keywords", BB.buttonStyleThick))
                    {
                        BBTarget.showProjectKeywords = false;
                    }
                    GUILayout.Space(10);

                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.TextArea(BBTarget.globalKeywords, BB.textAreaTiny);
                }
                GUILayout.Space(10);
                GUILayout.Label("Shaders found in project:" + BBTarget.shaderKeywordsList.Count);
                GUILayout.Space(10);
                if (!BBTarget.showProjectShaders)
                {
                    GUI.backgroundColor = BB.buttonColor;
                    if (GUILayout.Button("Show Project Shaders", BB.buttonStyleThick))
                    {
                        BBTarget.showProjectShaders = true;
                    }
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.backgroundColor = BB.buttonColor;
                    if (GUILayout.Button("Hide Project Shaders", BB.buttonStyleThick))
                    {
                        BBTarget.showProjectShaders = false;
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.Space(10);

                    count = BBTarget.shaderKeywordsList.Count;

                    for (int i = 0; i < count; i++)
                    {
                        shaderKeywords = BBTarget.shaderKeywordsList[i];

                        if (shaderKeywords.selected)
                        {
                            GUI.backgroundColor = selectedColor;
                        }
                        else
                        {
                            GUI.backgroundColor = Color.white;
                        }
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button(" Show Shader", BB.leftWhite, GUILayout.Width(90)))
                            {
                                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(shaderKeywords.path));
                            }

                            if (shaderKeywords.selected)
                            {
                                if (GUILayout.Button(shaderKeywords.shaderName + " ( Shared Keywords " + sharedKeywordsList.Count + " )", BB.leftWhite))
                                {
                                    shaderKeywords.selected = false;
                                    calculateSharedKeywordList();
                                }

                            }
                            else
                            {
                                if (GUILayout.Button(shaderKeywords.shaderName + " ( Keywords " + shaderKeywords.keywordsUsedList.Count + " )", BB.leftWhite))
                                {
                                    shaderKeywords.selected = true;
                                    calculateSharedKeywordList();
                                }
                            }

                            if (!shaderKeywords.expand)
                            {
                                if (GUILayout.Button("More info...", BB.leftWhite, GUILayout.Width(70)))
                                {
                                    shaderKeywords.expand = true;
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("Less info...", BB.leftWhite, GUILayout.Width(70)))
                                {
                                    shaderKeywords.expand = false;
                                }
                            }

                            if (shaderKeywords.selected)
                            {
                                if (GUILayout.Button("◄", BB.leftWhite, GUILayout.Width(20)))
                                {
                                    count = BBTarget.shaderKeywordsList.Count;

                                    for (int s = 0; s < count; s++)
                                    {
                                        BBTarget.shaderKeywordsList[s].selected = false;
                                    }

                                    sharedKeywordsList.Clear();
                                }
                            }
                        }
                        GUILayout.EndHorizontal();

                        GUI.backgroundColor = Color.white;

                        if (shaderKeywords.expand)
                        {
                            GUILayout.Space(5);
                            GUILayout.BeginHorizontal();
                            {

                                GUILayout.Label(shaderKeywords.path, BB.labelTiny);
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.Space(5);

                            if (shaderKeywords.selected)
                            {
                                foreach (string keyword in sharedKeywordsList)
                                {
                                    GUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label("     " + keyword);
                                    }
                                    GUILayout.EndHorizontal();
                                }
                            }
                            else
                            {

                                foreach (string keyword in shaderKeywords.keywordsUsedList)
                                {
                                    GUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label("     " + keyword);
                                    }
                                    GUILayout.EndHorizontal();
                                }
                            }

                            GUILayout.Space(10);
                        }
                    }
                }

                GUILayout.Space(10);
                GUILayout.Label("Unused Keywords found in materials:" + BBTarget.materialKeywordsFound);
                GUILayout.Space(10);

                if (BBTarget.materialKeywordsFound > 0)
                {
                    GUI.backgroundColor = BB.deleteColor;
                    if (GUILayout.Button("\r\nRemove " + BBTarget.materialKeywordsFound + " Unused keywords found in " + BBTarget.materialsFound + " Materials\r\n(Recommend backing up your project first as this cannot be undone)\r\n", BB.buttonStyleThick))
                    {
                        BBTarget.removeMaterialKeywords();
                    }
                }
            }
        }

        private void calculateSharedKeywordList()
        {
            sharedKeywordsList.Clear();
            int count = BBTarget.shaderKeywordsList.Count;

            for (int i = 0; i < count; i++)
            {
                shaderKeywords = BBTarget.shaderKeywordsList[i];
                int keyword_count = shaderKeywords.keywordsUsedList.Count;

                if (shaderKeywords.selected)
                {
                    for (int k = 0; k < keyword_count; k++)
                    {
                        string keyword = shaderKeywords.keywordsUsedList[k];

                        if (!sharedKeywordsList.Contains(keyword))
                        {
                            sharedKeywordsList.Add(keyword);
                        }
                    }
                }
            }
        }
    }
}