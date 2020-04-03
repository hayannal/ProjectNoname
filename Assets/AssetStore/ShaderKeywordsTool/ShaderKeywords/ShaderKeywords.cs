using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ArtIsDark
{
    [Serializable]

    public class ShaderKeywords
    {
        public string shaderName;
        public string path;
        public List<string> keywordsUsedList = new List<string>();
        public bool expand;
        public bool selected;
    }
}