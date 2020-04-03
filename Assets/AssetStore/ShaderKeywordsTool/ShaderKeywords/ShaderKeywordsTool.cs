using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace ArtIsDark
{
    public class ShaderKeywordsTool : MonoBehaviour
    {
        public String[] _skipProperties = { "surface", "ARB_precision_hint_fastest", "lambert", "surf", "skip_variants", ".", "target", "vert", "frag", "vertex", "gles", "glsl", "BlinnPhong", "exclude_renderers", "nomrt", "fragment", "geometry", "hull", "domain", "only_renderers", "enable_d3d11_debug_symbols" };
        public String[] _skipKeys = { "#pragma", "shader_feature", "multi_compile", "addshadow", "fullforwardshadows", "tessellate:TessFunction", "exclude_path:deferred", "exclude_path:forward", "exclude_path:prepass", "noshadow", "noambient", "novertexlights", "nolightmap", "nodynlightmap", "nodirlightmap", "nofog", "nometa", "noforwardadd", "softvegetation", "interpolateview", "halfasview", "approxview", "dualforward" };

        public List<string> globalKeywordsFound = new List<string>();
        public int materialKeywordsFound;
        public int materialsFound;
        public string globalKeywords;
        private List<string> keywordsFound = new List<string>();
        public bool showProjectKeywords;
        public bool showProjectShaders;
        public List<ShaderKeywords> shaderKeywordsList = new List<ShaderKeywords>();

        protected void Awake()
        {
            int count = _skipProperties.Length;

            for (int p = 0; p < count; p++)
            {
                _skipProperties[p] = _skipProperties[p].ToLower();
            }

            count = _skipKeys.Length;

            for (int k = 0; k < count; k++)
            {
                _skipKeys[k] = _skipKeys[k].ToLower();
            }
        }

        public void scanForKeywords()
        {
            globalKeywords = "";
            globalKeywordsFound.Clear();
            keywordsFound.Clear();
            shaderKeywordsList.Clear();
            materialsFound = 0;
            materialKeywordsFound = 0;

            List<String> fileList = Directory.GetFiles(Application.dataPath, "*.shader", SearchOption.AllDirectories).ToList();

            int count = fileList.Count;

            for (int i = 0; i < count; i++)
            {
                findKeywords(fileList[i]);
            }

            count = globalKeywordsFound.Count;
            //Debug.LogError("Global Keywords found:"+count);

            for (int g = 0; g < count; g++)
            {
                globalKeywords += globalKeywordsFound[g] + " ";
            }

            //Debug.Log(globalKeywords);
            countMaterialKeywords();
        }

        private void findKeywords(String file)
        {
            String[] filedata = File.ReadAllLines(file);

            int count = filedata.Length;
            int keywords_count;
            String[] keywords;
            String line;
            String s;
            int pragma_Index;

            keywordsFound.Clear();

            for (int i = 0; i < count; i++)
            {
                line = filedata[i];
                pragma_Index = line.IndexOf("#pragma");

                if (pragma_Index != -1)
                {
                    s = line.Substring(pragma_Index, line.Length - pragma_Index);
                    s = s.TrimStart();
                    s = s.TrimEnd();

                    if (!skipProperties(s))
                    {
                        keywords = s.Split(' ');
                        keywords_count = keywords.Length;

                        for (int k = 0; k < keywords_count; k++)
                        {
                            if (!skipKeys(keywords[k]))
                            {
                                if (!keywordsFound.Contains(keywords[k]))
                                {
                                    keywordsFound.Add(keywords[k]);
                                }

                                if (!globalKeywordsFound.Contains(keywords[k]))
                                {
                                    globalKeywordsFound.Add(keywords[k]);
                                }
                            }
                        }
                    }
                }
            }

            if (keywordsFound.Count > 0)
            {
                ShaderKeywords shaderKeywords = new ShaderKeywords();
                shaderKeywordsList.Add(shaderKeywords);

                int lastSlashIndex = file.LastIndexOf("\\") + 1;
                shaderKeywords.shaderName = file.Substring(lastSlashIndex, file.Length - lastSlashIndex - 7);
                int assetPathIndex = file.IndexOf("/Assets") + 1;
                shaderKeywords.path = file.Substring(assetPathIndex, file.Length - assetPathIndex);

                //Debug.LogError("Filename:"+shaderKeywords.shaderName+":"+keywordsFound.Count);
                keywords_count = keywordsFound.Count;

                for (int k = 0; k < keywords_count; k++)
                {
                    //Debug.Log(keywordsFound[k]);
                    shaderKeywords.keywordsUsedList.Add(keywordsFound[k]);
                }
            }
        }

        public bool skipProperties(String s)
        {
            int count = _skipProperties.Length;

            for (int p = 0; p < count; p++)
            {
                if (s.ToLower().IndexOf(_skipProperties[p]) != -1)
                {
                    return true;
                }
            }

            return false;
        }

        public bool skipKeys(String s)
        {
            if (s == "_" || s == "__" || s == "___")
            {
                return true;
            }

            int count = _skipKeys.Length;

            for (int k = 0; k < count; k++)
            {
                //Debug.LogError(s.ToLower()+":"+_skipKeys[k]);
                if (s.ToLower().IndexOf(_skipKeys[k]) != -1)
                {
                    return true;
                }
            }

            return false;
        }

        public void countMaterialKeywords()
        {
            List<String> fileList = Directory.GetFiles(Application.dataPath, "*.mat", SearchOption.AllDirectories).ToList();

            int count = fileList.Count;

            for (int i = 0; i < count; i++)
            {
                checkMaterials(fileList[i], false);
            }
        }

        public void removeMaterialKeywords()
        {
            List<String> fileList = Directory.GetFiles(Application.dataPath, "*.mat", SearchOption.AllDirectories).ToList();

            int count = fileList.Count;

            for (int i = 0; i < count; i++)
            {
                checkMaterials(fileList[i], true);
            }

            materialsFound = 0;
            materialKeywordsFound = 0;
        }

        public void checkMaterials(string file, bool write)
        {
            String materialReplacementText;
            bool updateFile = false;
            String[] keywords;
            String[] filedata = File.ReadAllLines(file);

            int count = filedata.Length;

            for (int i = 0; i < count; i++)
            {
                if (filedata[i].IndexOf("m_ShaderKeywords") != -1)
                {
                    int last = filedata[i].IndexOf(":");

                    if (last != -1 && filedata[i].Length > last + 1)
                    {
                        materialReplacementText = filedata[i].Remove(last + 1);
                        keywords = filedata[i].TrimStart().TrimEnd().Split(' ');

                        int keywordCount = keywords.Length;

                        for (int k = 1; k < keywordCount; k++)
                        {
                            if (keywords[k] != "" || keywords[k] != " ")
                            {
                                if (globalKeywordsFound.Contains(keywords[k]))
                                {

                                    materialReplacementText += " " + keywords[k];
                                }
                                else
                                {
                                    updateFile = true;
                                    materialKeywordsFound++;
                                }
                            }
                        }
                        //filedata[i] = filedata[i].Remove(last + 1);
                        //Debug.Log(file);
                        //Debug.Log(filedata[i]);

                        if (updateFile)
                        {
                            materialsFound++;
                            //Debug.LogError(materialReplacementText);

                            if (write)
                            {
                                filedata[i] = materialReplacementText;
                            }
                        }
                    }
                }
            }

            if (write)
            {
                File.WriteAllLines(file, filedata);
            }
        }
    }
}