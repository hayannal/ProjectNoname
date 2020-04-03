#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ArtIsDark
{
    public partial class BBStyles
    {
        private bool debugMode;
        static List<string> layers;
        static string[] layerNames;

        public bool generateTitle(GameObject GO, string title, int versionMajor, int versionMinor)
        {
            GUILayout.Space(10);
            GUI.backgroundColor = inactiveColor;

            if (versionMajor > 0)
            {
                if (GUILayout.Button(new GUIContent(string.Format("   BB " + title + " V{0}.{1}", versionMajor, versionMinor)), titleStyle))
                {
                    debugMode = !debugMode;
                }
            }
            else
            {
                if (GUILayout.Button("   BB " + title, titleStyle))
                {
                    debugMode = !debugMode;
                }
            }
            GUILayout.Label(new GUIContent("       " + GO.name), subTitleStyle);
            if (debugMode)
            {
                GUI.backgroundColor = selectedColor;
                GUILayout.Label(new GUIContent("showing base"), buttonStyle);
                GUILayout.Space(10);
            }
            GUI.backgroundColor = Color.white;
            return debugMode;
        }
        //
        private Color _inactiveColor;
        public Color inactiveColor
        {
            get
            {
                if (_inactiveColor == Color.clear)
                {
                    _inactiveColor = new Color(.1f, .1f, .1f);
                }
                return _inactiveColor;
            }
        }

        private Color _inactiveColorLight;
        public Color inactiveColorLight
        {
            get
            {
                if (_inactiveColorLight == Color.clear)
                {
                    _inactiveColorLight = new Color(.2f, .2f, .2f);
                }
                return _inactiveColorLight;
            }
        }
        //
        private Color _selectedColor;
        public Color selectedColor
        {
            get
            {
                if (_selectedColor == Color.clear)
                {
                    _selectedColor = new Color(.1f, .4f, .9f);
                }
                return _selectedColor;
            }
        }
        //
        private Color _deleteColor;
        public Color deleteColor
        {
            get
            {
                if (_deleteColor == Color.clear)
                {
                    _deleteColor = new Color(0.7f, .2f, 0.2f);
                }
                return _deleteColor;
            }
        }
        //
        private Color _buttonColor;
        public Color buttonColor
        {
            get
            {
                if (_buttonColor == Color.clear)
                {
                    _buttonColor = new Color(0.0f, .6f, 0.15f);
                }
                return _buttonColor;
            }
        }
        private GUIStyle _large;
        public GUIStyle large
        {
            get
            {
                if (_large == null)
                {
                    Color c = new Color(0.44f, 0.44f, 0.28f);

                    _large = new GUIStyle(EditorStyles.label);
                    _large.fontStyle = FontStyle.BoldAndItalic;
                    _large.fontSize = 18;
                    _large.fixedHeight = 42;
                    _large.alignment = TextAnchor.MiddleLeft;
                    _large.normal.textColor = c;
                    _large.onNormal.textColor = c;
                    _large.hover.textColor = c;
                    _large.onHover.textColor = c;
                    _large.focused.textColor = c;
                    _large.onFocused.textColor = c;
                    _large.active.textColor = c;
                    _large.onActive.textColor = c;
                    _large.stretchWidth = true;
                }
                return _large;
            }
        }
        private GUIStyle _titleStyle;
        public GUIStyle titleStyle
        {
            get
            {
                if (_titleStyle == null)
                {
                    Color c = new Color(0.44f, 0.44f, 0.28f);

                    _titleStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _titleStyle.fontStyle = FontStyle.BoldAndItalic;
                    _titleStyle.fontSize = 18;
                    _titleStyle.fixedHeight = 42;
                    _titleStyle.alignment = TextAnchor.MiddleLeft;
                    _titleStyle.normal.textColor = c;
                    _titleStyle.onNormal.textColor = c;
                    _titleStyle.hover.textColor = c;
                    _titleStyle.onHover.textColor = c;
                    _titleStyle.focused.textColor = c;
                    _titleStyle.onFocused.textColor = c;
                    _titleStyle.active.textColor = c;
                    _titleStyle.onActive.textColor = c;
                    _titleStyle.stretchWidth = true;
                }
                return _titleStyle;
            }
        }
        private GUIStyle _subTitleStyle;
        public GUIStyle subTitleStyle
        {
            get
            {
                if (_subTitleStyle == null)
                {
                    Color c = new Color(0.44f, 0.44f, 0.28f);

                    _subTitleStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _subTitleStyle.fontStyle = FontStyle.Italic;
                    _subTitleStyle.fontSize = 11;
                    _subTitleStyle.fixedHeight = 26;
                    _subTitleStyle.alignment = TextAnchor.UpperLeft;
                    _subTitleStyle.normal.textColor = c;
                    _subTitleStyle.onNormal.textColor = c;
                    _subTitleStyle.hover.textColor = c;
                    _subTitleStyle.onHover.textColor = c;
                    _subTitleStyle.focused.textColor = c;
                    _subTitleStyle.onFocused.textColor = c;
                    _subTitleStyle.active.textColor = c;
                    _subTitleStyle.onActive.textColor = c;
                    _subTitleStyle.stretchWidth = true;
                }
                return _subTitleStyle;
            }
        }
        //
        private GUIStyle _headerStyle;
        public GUIStyle headerStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    Color c = new Color(0.44f, 0.44f, 0.28f);

                    _headerStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _headerStyle.fontStyle = FontStyle.Normal;
                    _headerStyle.fontSize = 11;
                    _headerStyle.fixedHeight = 22;
                    _headerStyle.alignment = TextAnchor.MiddleLeft;
                    _headerStyle.normal.textColor = c;
                    _headerStyle.onNormal.textColor = c;
                    _headerStyle.hover.textColor = c;
                    _headerStyle.onHover.textColor = c;
                    _headerStyle.focused.textColor = c;
                    _headerStyle.onFocused.textColor = c;
                    _headerStyle.active.textColor = c;
                    _headerStyle.onActive.textColor = c;
                    _headerStyle.stretchWidth = true;
                }
                return _headerStyle;
            }
        }

        private GUIStyle _rayGreenStyle;
        public GUIStyle rayGreenStyle
        {
            get
            {
                if (_rayGreenStyle == null)
                {
                    Color c = new Color(0f, 0.8f, 0f);

                    _rayGreenStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _rayGreenStyle.fontStyle = FontStyle.Normal;
                    _rayGreenStyle.fontSize = 13;
                    _rayGreenStyle.fixedHeight = 22;
                    _rayGreenStyle.alignment = TextAnchor.MiddleLeft;
                    _rayGreenStyle.normal.textColor = c;
                    _rayGreenStyle.onNormal.textColor = c;
                    _rayGreenStyle.hover.textColor = c;
                    _rayGreenStyle.onHover.textColor = c;
                    _rayGreenStyle.focused.textColor = c;
                    _rayGreenStyle.onFocused.textColor = c;
                    _rayGreenStyle.active.textColor = c;
                    _rayGreenStyle.onActive.textColor = c;
                    _rayGreenStyle.stretchWidth = true;
                }
                return _rayGreenStyle;
            }
        }

        private GUIStyle _rayRedStyle;
        public GUIStyle rayRedStyle
        {
            get
            {
                if (_rayRedStyle == null)
                {
                    Color c = new Color(0.8f, 0f, 0f);

                    _rayRedStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _rayRedStyle.fontStyle = FontStyle.Normal;
                    _rayRedStyle.fontSize = 13;
                    _rayRedStyle.fixedHeight = 22;
                    _rayRedStyle.alignment = TextAnchor.MiddleLeft;
                    _rayRedStyle.normal.textColor = c;
                    _rayRedStyle.onNormal.textColor = c;
                    _rayRedStyle.hover.textColor = c;
                    _rayRedStyle.onHover.textColor = c;
                    _rayRedStyle.focused.textColor = c;
                    _rayRedStyle.onFocused.textColor = c;
                    _rayRedStyle.active.textColor = c;
                    _rayRedStyle.onActive.textColor = c;
                    _rayRedStyle.stretchWidth = true;
                }
                return _rayRedStyle;
            }
        }

        private GUIStyle _hurrayStyle;
        public GUIStyle hurrayStyle
        {
            get
            {
                if (_hurrayStyle == null)
                {
                    Color c = new Color(0.5f, .5f, .2f);

                    _hurrayStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _hurrayStyle.fontStyle = FontStyle.Normal;
                    _hurrayStyle.fontSize = 22;
                    _hurrayStyle.fixedHeight = 22;
                    _hurrayStyle.alignment = TextAnchor.MiddleLeft;
                    _hurrayStyle.normal.textColor = c;
                    _hurrayStyle.onNormal.textColor = c;
                    _hurrayStyle.hover.textColor = c;
                    _hurrayStyle.onHover.textColor = c;
                    _hurrayStyle.focused.textColor = c;
                    _hurrayStyle.onFocused.textColor = c;
                    _hurrayStyle.active.textColor = c;
                    _hurrayStyle.onActive.textColor = c;
                    _hurrayStyle.stretchWidth = true;
                }
                return _hurrayStyle;
            }
        }

        private GUIStyle _headerStyleLoop;
        public GUIStyle headerStyleLoop
        {
            get
            {
                if (_headerStyleLoop == null)
                {
                    Color c = new Color(0.23f, 0.42f, 0.18f);

                    _headerStyleLoop = new GUIStyle(EditorStyles.toolbarButton);
                    _headerStyleLoop.fontStyle = FontStyle.Bold;
                    _headerStyleLoop.fontSize = 11;
                    _headerStyleLoop.fixedHeight = 22;
                    _headerStyleLoop.alignment = TextAnchor.MiddleLeft;
                    _headerStyleLoop.normal.textColor = c;
                    _headerStyleLoop.onNormal.textColor = c;
                    _headerStyleLoop.hover.textColor = c;
                    _headerStyleLoop.onHover.textColor = c;
                    _headerStyleLoop.focused.textColor = c;
                    _headerStyleLoop.onFocused.textColor = c;
                    _headerStyleLoop.active.textColor = c;
                    _headerStyleLoop.onActive.textColor = c;
                    _headerStyleLoop.stretchWidth = true;
                }
                return _headerStyleLoop;
            }
        }

        private GUIStyle _headerStyleRight;
        public GUIStyle headerStyleRight
        {
            get
            {
                if (_headerStyleRight == null)
                {
                    Color c = new Color(0.44f, 0.44f, 0.28f);

                    _headerStyleRight = new GUIStyle(EditorStyles.toolbarButton);
                    _headerStyleRight.fontStyle = FontStyle.Bold;
                    _headerStyleRight.fontSize = 10;
                    _headerStyleRight.fixedHeight = 22;
                    _headerStyleRight.alignment = TextAnchor.MiddleRight;
                    _headerStyleRight.normal.textColor = c;
                    _headerStyleRight.onNormal.textColor = c;
                    _headerStyleRight.hover.textColor = c;
                    _headerStyleRight.onHover.textColor = c;
                    _headerStyleRight.focused.textColor = c;
                    _headerStyleRight.onFocused.textColor = c;
                    _headerStyleRight.active.textColor = c;
                    _headerStyleRight.onActive.textColor = c;
                    _headerStyleRight.stretchWidth = true;
                }
                return _headerStyleRight;
            }
        }

        private GUIStyle _headerStyleLeft;
        public GUIStyle headerStyleLeft
        {
            get
            {
                if (_headerStyleLeft == null)
                {
                    Color c = new Color(0.44f, 0.44f, 0.28f);

                    _headerStyleLeft = new GUIStyle(EditorStyles.toolbarButton);
                    _headerStyleLeft.fontStyle = FontStyle.Bold;
                    _headerStyleLeft.fontSize = 10;
                    _headerStyleLeft.fixedHeight = 22;
                    _headerStyleLeft.alignment = TextAnchor.MiddleLeft;
                    _headerStyleLeft.normal.textColor = c;
                    _headerStyleLeft.onNormal.textColor = c;
                    _headerStyleLeft.hover.textColor = c;
                    _headerStyleLeft.onHover.textColor = c;
                    _headerStyleLeft.focused.textColor = c;
                    _headerStyleLeft.onFocused.textColor = c;
                    _headerStyleLeft.active.textColor = c;
                    _headerStyleLeft.onActive.textColor = c;
                    _headerStyleLeft.stretchWidth = true;
                }
                return _headerStyleLeft;
            }
        }

        private GUIStyle _headerStyleLeftWhite;
        public GUIStyle headerStyleLeftWhite
        {
            get
            {
                if (_headerStyleLeftWhite == null)
                {
                    Color c = new Color(.6f, .6f, .6f);

                    _headerStyleLeftWhite = new GUIStyle(EditorStyles.toolbarButton);
                    _headerStyleLeftWhite.fontStyle = FontStyle.Bold;
                    _headerStyleLeftWhite.fontSize = 10;
                    _headerStyleLeftWhite.fixedHeight = 22;
                    _headerStyleLeftWhite.alignment = TextAnchor.MiddleLeft;
                    _headerStyleLeftWhite.normal.textColor = c;
                    _headerStyleLeftWhite.onNormal.textColor = c;
                    _headerStyleLeftWhite.hover.textColor = c;
                    _headerStyleLeftWhite.onHover.textColor = c;
                    _headerStyleLeftWhite.focused.textColor = c;
                    _headerStyleLeftWhite.onFocused.textColor = c;
                    _headerStyleLeftWhite.active.textColor = c;
                    _headerStyleLeftWhite.onActive.textColor = c;
                    _headerStyleLeftWhite.stretchWidth = true;
                }
                return _headerStyleLeftWhite;
            }
        }

        private GUIStyle _leftWhite;
        public GUIStyle leftWhite
        {
            get
            {
                if (_leftWhite == null)
                {

                    _leftWhite = new GUIStyle(EditorStyles.toolbarButton);
                    _leftWhite.fontStyle = FontStyle.Normal;
                    _leftWhite.fontSize = 10;
                    _leftWhite.fixedHeight = 22;
                    _leftWhite.alignment = TextAnchor.MiddleLeft;
                    _leftWhite.stretchWidth = true;
                }
                return _leftWhite;
            }
        }

        private GUIStyle _headerStyleLeftWhiteShallow;
        public GUIStyle headerStyleLeftWhiteShallow
        {
            get
            {
                if (_headerStyleLeftWhiteShallow == null)
                {
                    Color c = new Color(.6f, .6f, .6f);

                    _headerStyleLeftWhiteShallow = new GUIStyle(EditorStyles.toolbarButton);
                    _headerStyleLeftWhiteShallow.fontStyle = FontStyle.Bold;
                    _headerStyleLeftWhiteShallow.fontSize = 10;
                    _headerStyleLeftWhiteShallow.fixedHeight = 18;
                    _headerStyleLeftWhiteShallow.alignment = TextAnchor.MiddleLeft;
                    _headerStyleLeftWhiteShallow.normal.textColor = c;
                    _headerStyleLeftWhiteShallow.onNormal.textColor = c;
                    _headerStyleLeftWhiteShallow.hover.textColor = c;
                    _headerStyleLeftWhiteShallow.onHover.textColor = c;
                    _headerStyleLeftWhiteShallow.focused.textColor = c;
                    _headerStyleLeftWhiteShallow.onFocused.textColor = c;
                    _headerStyleLeftWhiteShallow.active.textColor = c;
                    _headerStyleLeftWhiteShallow.onActive.textColor = c;
                    _headerStyleLeftWhiteShallow.stretchWidth = true;
                }
                return _headerStyleLeftWhiteShallow;
            }
        }

        //
        private GUIStyle _headerCenteredStyle;
        public GUIStyle headerCenteredStyle
        {
            get
            {
                if (_headerCenteredStyle == null)
                {
                    Color c = new Color(0.44f, 0.44f, 0.28f);

                    _headerCenteredStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _headerCenteredStyle.fontStyle = FontStyle.Bold;
                    _headerCenteredStyle.fontSize = 11;
                    _headerCenteredStyle.fixedHeight = 22;
                    _headerCenteredStyle.alignment = TextAnchor.MiddleCenter;
                    _headerCenteredStyle.normal.textColor = c;
                    _headerCenteredStyle.onNormal.textColor = c;
                    _headerCenteredStyle.hover.textColor = c;
                    _headerCenteredStyle.onHover.textColor = c;
                    _headerCenteredStyle.focused.textColor = c;
                    _headerCenteredStyle.onFocused.textColor = c;
                    _headerCenteredStyle.active.textColor = c;
                    _headerCenteredStyle.onActive.textColor = c;
                    _headerCenteredStyle.stretchWidth = true;
                }
                return _headerCenteredStyle;
            }
        }
        //
        private GUIStyle _toggleStyle;
        public GUIStyle toggledStyle
        {
            get
            {
                if (_toggleStyle == null)
                {
                    Color c = new Color(0.54f, 0.54f, 0.38f);

                    _toggleStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _toggleStyle.fontStyle = FontStyle.Bold;
                    _toggleStyle.fontStyle = FontStyle.Bold;
                    _toggleStyle.fontSize = 16;
                    _toggleStyle.fixedHeight = 22;
                    _toggleStyle.alignment = TextAnchor.MiddleCenter;
                    _toggleStyle.normal.textColor = c;
                    _toggleStyle.onNormal.textColor = c;
                    _toggleStyle.hover.textColor = c;
                    _toggleStyle.onHover.textColor = c;
                    _toggleStyle.focused.textColor = c;
                    _toggleStyle.onFocused.textColor = c;
                    _toggleStyle.active.textColor = c;
                    _toggleStyle.onActive.textColor = c;
                    _toggleStyle.stretchWidth = true;
                }
                return _toggleStyle;
            }
        }
        //
        private GUIStyle _headerStyleDark;
        public GUIStyle headerStyleDark
        {
            get
            {
                if (_headerStyleDark == null)
                {
                    Color c = Color.black;

                    _headerStyleDark = new GUIStyle(EditorStyles.toolbarButton);
                    _headerStyleDark.fontStyle = FontStyle.Bold;
                    _headerStyleDark.fontSize = 11;
                    _headerStyleDark.fixedHeight = 22;
                    _headerStyleDark.alignment = TextAnchor.MiddleLeft;
                    _headerStyleDark.normal.textColor = c;
                    _headerStyleDark.onNormal.textColor = c;
                    _headerStyleDark.hover.textColor = c;
                    _headerStyleDark.onHover.textColor = c;
                    _headerStyleDark.focused.textColor = c;
                    _headerStyleDark.onFocused.textColor = c;
                    _headerStyleDark.active.textColor = c;
                    _headerStyleDark.onActive.textColor = c;
                    _headerStyleDark.stretchWidth = true;
                }
                return _headerStyleDark;
            }
        }
        //
        private GUIStyle _headerStyleLight;
        public GUIStyle headerStyleLight
        {
            get
            {
                if (_headerStyleDark == null)
                {
                    Color c = Color.gray;

                    _headerStyleLight = new GUIStyle(EditorStyles.toolbarButton);
                    _headerStyleLight.fontStyle = FontStyle.Bold;
                    _headerStyleLight.fontSize = 11;
                    _headerStyleLight.fixedHeight = 22;
                    _headerStyleLight.alignment = TextAnchor.MiddleLeft;
                    _headerStyleLight.normal.textColor = c;
                    _headerStyleLight.onNormal.textColor = c;
                    _headerStyleLight.hover.textColor = c;
                    _headerStyleLight.onHover.textColor = c;
                    _headerStyleLight.focused.textColor = c;
                    _headerStyleLight.onFocused.textColor = c;
                    _headerStyleLight.active.textColor = c;
                    _headerStyleLight.onActive.textColor = c;
                    _headerStyleLight.stretchWidth = true;
                }
                return _headerStyleLight;
            }
        }
        //
        private GUIStyle _pillButtonStyle;
        public GUIStyle pillButtonStyle
        {
            get
            {
                if (_pillButtonStyle == null)
                {
                    Color c = Color.gray;

                    _pillButtonStyle = new GUIStyle(EditorStyles.miniButtonLeft);
                    _pillButtonStyle.fontStyle = FontStyle.Bold;
                    _pillButtonStyle.fontSize = 11;
                    _pillButtonStyle.fixedHeight = 20;
                    _pillButtonStyle.alignment = TextAnchor.MiddleLeft;
                    _pillButtonStyle.normal.textColor = c;
                    _pillButtonStyle.onNormal.textColor = c;
                    _pillButtonStyle.hover.textColor = c;
                    _pillButtonStyle.onHover.textColor = c;
                    _pillButtonStyle.focused.textColor = c;
                    _pillButtonStyle.onFocused.textColor = c;
                    _pillButtonStyle.active.textColor = c;
                    _pillButtonStyle.onActive.textColor = c;
                    _pillButtonStyle.stretchWidth = true;
                }
                return _pillButtonStyle;
            }
        }
        //
        private GUIStyle _subHeaderStyle;
        public GUIStyle subHeaderStyle
        {
            get
            {
                if (_subHeaderStyle == null)
                {
                    _subHeaderStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _subHeaderStyle.fontStyle = FontStyle.Normal;
                    _subHeaderStyle.fontSize = 11;
                    _subHeaderStyle.fixedHeight = 22;
                    _subHeaderStyle.alignment = TextAnchor.MiddleCenter;
                    _subHeaderStyle.stretchWidth = true;
                }
                return _subHeaderStyle;
            }
        }
        //
        private GUIStyle _subHeaderLeftStyle;
        public GUIStyle subHeaderLeftStyle
        {
            get
            {
                if (_subHeaderLeftStyle == null)
                {
                    _subHeaderLeftStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _subHeaderLeftStyle.fontStyle = FontStyle.Normal;
                    _subHeaderLeftStyle.fontSize = 11;
                    _subHeaderLeftStyle.fixedHeight = 22;
                    _subHeaderLeftStyle.alignment = TextAnchor.MiddleLeft;
                    _subHeaderLeftStyle.stretchWidth = true;
                }
                return _subHeaderLeftStyle;
            }
        }
        //
        private GUIStyle _buttonStyle;
        public GUIStyle buttonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
                    Color c = new Color(0.6f, 0.6f, 0.6f);

                    _buttonStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _buttonStyle.fontStyle = FontStyle.Bold;
                    _buttonStyle.fontSize = 11;
                    _buttonStyle.fixedHeight = 22;
                    _buttonStyle.alignment = TextAnchor.MiddleCenter;
                    _buttonStyle.normal.textColor = c;
                    _buttonStyle.onNormal.textColor = c;
                    _buttonStyle.hover.textColor = c;
                    _buttonStyle.onHover.textColor = c;
                    _buttonStyle.focused.textColor = c;
                    _buttonStyle.onFocused.textColor = c;
                    _buttonStyle.active.textColor = c;
                    _buttonStyle.onActive.textColor = c;
                }
                return _buttonStyle;
            }
        }

        private GUIStyle _buttonStyleDim;
        public GUIStyle buttonStyleDim
        {
            get
            {
                if (_buttonStyleDim == null)
                {
                    Color c = new Color(0.38f, 0.38f, 0.24f);

                    _buttonStyleDim = new GUIStyle(EditorStyles.toolbarButton);
                    _buttonStyleDim.fontStyle = FontStyle.Bold;
                    _buttonStyleDim.fontSize = 11;
                    _buttonStyleDim.fixedHeight = 22;
                    _buttonStyleDim.alignment = TextAnchor.MiddleCenter;
                    _buttonStyleDim.normal.textColor = c;
                    _buttonStyleDim.onNormal.textColor = c;
                    _buttonStyleDim.hover.textColor = c;
                    _buttonStyleDim.onHover.textColor = c;
                    _buttonStyleDim.focused.textColor = c;
                    _buttonStyleDim.onFocused.textColor = c;
                    _buttonStyleDim.active.textColor = c;
                    _buttonStyleDim.onActive.textColor = c;
                }
                return _buttonStyleDim;
            }
        }

        private GUIStyle _buttonStyleThick;
        public GUIStyle buttonStyleThick
        {
            get
            {
                if (_buttonStyle == null)
                {
                    Color c = new Color(0.6f, 0.6f, 0.6f);

                    _buttonStyleThick = new GUIStyle(EditorStyles.toolbarButton);
                    _buttonStyleThick.fontStyle = FontStyle.Bold;
                    _buttonStyleThick.fontSize = 11;
                    _buttonStyleThick.fixedHeight = 60;
                    _buttonStyleThick.alignment = TextAnchor.MiddleCenter;
                    _buttonStyleThick.normal.textColor = c;
                    _buttonStyleThick.onNormal.textColor = c;
                    _buttonStyleThick.hover.textColor = c;
                    _buttonStyleThick.onHover.textColor = c;
                    _buttonStyleThick.focused.textColor = c;
                    _buttonStyleThick.onFocused.textColor = c;
                    _buttonStyleThick.active.textColor = c;
                    _buttonStyleThick.onActive.textColor = c;
                }
                return _buttonStyleThick;
            }
        }

        private GUIStyle _buttonStyleShallow;
        public GUIStyle buttonStyleShallow
        {
            get
            {
                if (_buttonStyleShallow == null)
                {
                    Color c = new Color(0.6f, 0.6f, 0.6f);

                    _buttonStyleShallow = new GUIStyle(EditorStyles.toolbarButton);
                    _buttonStyleShallow.fontStyle = FontStyle.Bold;
                    _buttonStyleShallow.fontSize = 10;
                    _buttonStyleShallow.fixedHeight = 18;
                    _buttonStyleShallow.alignment = TextAnchor.MiddleCenter;
                    _buttonStyleShallow.normal.textColor = c;
                    _buttonStyleShallow.onNormal.textColor = c;
                    _buttonStyleShallow.hover.textColor = c;
                    _buttonStyleShallow.onHover.textColor = c;
                    _buttonStyleShallow.focused.textColor = c;
                    _buttonStyleShallow.onFocused.textColor = c;
                    _buttonStyleShallow.active.textColor = c;
                    _buttonStyleShallow.onActive.textColor = c;
                }
                return _buttonStyleShallow;
            }
        }

        private GUIStyle _buttonStyleTinyShallow;
        public GUIStyle buttonStyleTinyShallow
        {
            get
            {
                if (_buttonStyleTinyShallow == null)
                {
                    Color c = new Color(0.6f, 0.6f, 0.6f);

                    _buttonStyleTinyShallow = new GUIStyle(EditorStyles.toolbarButton);
                    _buttonStyleTinyShallow.fontStyle = FontStyle.Bold;
                    _buttonStyleTinyShallow.fontSize = 8;
                    _buttonStyleTinyShallow.fixedHeight = 18;
                    _buttonStyleTinyShallow.alignment = TextAnchor.MiddleCenter;
                    _buttonStyleTinyShallow.normal.textColor = c;
                    _buttonStyleTinyShallow.onNormal.textColor = c;
                    _buttonStyleTinyShallow.hover.textColor = c;
                    _buttonStyleTinyShallow.onHover.textColor = c;
                    _buttonStyleTinyShallow.focused.textColor = c;
                    _buttonStyleTinyShallow.onFocused.textColor = c;
                    _buttonStyleTinyShallow.active.textColor = c;
                    _buttonStyleTinyShallow.onActive.textColor = c;
                }
                return _buttonStyleTinyShallow;
            }
        }

        private GUIStyle _buttonStyleTinyShallowLeft;
        public GUIStyle buttonStyleTinyShallowLeft
        {
            get
            {
                if (_buttonStyleTinyShallowLeft == null)
                {
                    Color c = new Color(0.6f, 0.6f, 0.6f);

                    _buttonStyleTinyShallowLeft = new GUIStyle(EditorStyles.toolbarButton);
                    _buttonStyleTinyShallowLeft.fontStyle = FontStyle.Bold;
                    _buttonStyleTinyShallowLeft.fontSize = 8;
                    _buttonStyleTinyShallowLeft.fixedHeight = 18;
                    _buttonStyleTinyShallowLeft.alignment = TextAnchor.MiddleLeft;
                    _buttonStyleTinyShallowLeft.normal.textColor = c;
                    _buttonStyleTinyShallowLeft.onNormal.textColor = c;
                    _buttonStyleTinyShallowLeft.hover.textColor = c;
                    _buttonStyleTinyShallowLeft.onHover.textColor = c;
                    _buttonStyleTinyShallowLeft.focused.textColor = c;
                    _buttonStyleTinyShallowLeft.onFocused.textColor = c;
                    _buttonStyleTinyShallowLeft.active.textColor = c;
                    _buttonStyleTinyShallowLeft.onActive.textColor = c;
                }
                return _buttonStyleTinyShallowLeft;
            }
        }

        private GUIStyle _tinyShallowLeft;
        public GUIStyle tinyShallowLeft
        {
            get
            {
                if (_tinyShallowLeft == null)
                {
                    Color c = new Color(0.32f, 0.32f, 0.18f);

                    _tinyShallowLeft = new GUIStyle(EditorStyles.toolbarButton);
                    _tinyShallowLeft.fontStyle = FontStyle.Bold;
                    _tinyShallowLeft.fontSize = 9;
                    _tinyShallowLeft.fixedHeight = 22;
                    _tinyShallowLeft.alignment = TextAnchor.MiddleLeft;
                    _tinyShallowLeft.normal.textColor = c;
                    _tinyShallowLeft.onNormal.textColor = c;
                    _tinyShallowLeft.hover.textColor = c;
                    _tinyShallowLeft.onHover.textColor = c;
                    _tinyShallowLeft.focused.textColor = c;
                    _tinyShallowLeft.onFocused.textColor = c;
                    _tinyShallowLeft.active.textColor = c;
                    _tinyShallowLeft.onActive.textColor = c;
                }
                return _tinyShallowLeft;
            }
        }

        private GUIStyle _textAreaTiny;
        public GUIStyle textAreaTiny
        {
            get
            {
                if (_textAreaTiny == null)
                {
                    _textAreaTiny = new GUIStyle(EditorStyles.textArea);
                    _textAreaTiny.fontStyle = FontStyle.Normal;
                    _textAreaTiny.fontSize = 10;
                    _textAreaTiny.alignment = TextAnchor.MiddleLeft;
                    _textAreaTiny.wordWrap = true;
                }
                return _textAreaTiny;
            }
        }

        private GUIStyle _labelWrappedTiny;
        public GUIStyle labelWrappedTiny
        {
            get
            {
                if (_labelWrappedTiny == null)
                {
                    _labelWrappedTiny = new GUIStyle(EditorStyles.label);
                    _labelWrappedTiny.fontStyle = FontStyle.Normal;
                    _labelWrappedTiny.fontSize = 10;
                    _labelWrappedTiny.alignment = TextAnchor.MiddleLeft;
                    _labelWrappedTiny.wordWrap = true;
                }
                return _labelWrappedTiny;
            }
        }

        private GUIStyle _labelTiny;
        public GUIStyle labelTiny
        {
            get
            {
                if (_labelTiny == null)
                {
                    _labelTiny = new GUIStyle(EditorStyles.label);
                    _labelTiny.fontStyle = FontStyle.Normal;
                    _labelTiny.fontSize = 9;
                    _labelTiny.alignment = TextAnchor.MiddleLeft;
                    _labelTiny.wordWrap = true;
                }
                return _labelTiny;
            }
        }

        private GUIStyle _dividerGreenStyle;
        public GUIStyle dividerGreenStyle
        {
            get
            {
                if (_dividerGreenStyle == null)
                {
                    Color c = new Color(0.6f, 0.6f, 0.6f);

                    _dividerGreenStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _dividerGreenStyle.fontStyle = FontStyle.Bold;
                    _dividerGreenStyle.fontSize = 2;
                    _dividerGreenStyle.fixedHeight = 5;
                    _dividerGreenStyle.alignment = TextAnchor.MiddleCenter;
                    _dividerGreenStyle.normal.textColor = c;
                    _dividerGreenStyle.onNormal.textColor = c;
                    _dividerGreenStyle.hover.textColor = c;
                    _dividerGreenStyle.onHover.textColor = c;
                    _dividerGreenStyle.focused.textColor = c;
                    _dividerGreenStyle.onFocused.textColor = c;
                    _dividerGreenStyle.active.textColor = c;
                    _dividerGreenStyle.onActive.textColor = c;
                }
                return _dividerGreenStyle;
            }
        }
        //
        private GUIStyle _buttonStyleLeft;
        public GUIStyle buttonStyleLeft
        {
            get
            {
                if (_buttonStyle == null)
                {
                    Color c = new Color(0.6f, 0.6f, 0.6f);

                    _buttonStyleLeft = new GUIStyle(EditorStyles.toolbarButton);
                    _buttonStyleLeft.fontStyle = FontStyle.Bold;
                    _buttonStyleLeft.fontSize = 11;
                    _buttonStyleLeft.fixedHeight = 24;
                    _buttonStyleLeft.alignment = TextAnchor.MiddleLeft;
                    _buttonStyleLeft.normal.textColor = c;
                    _buttonStyleLeft.onNormal.textColor = c;
                    _buttonStyleLeft.hover.textColor = c;
                    _buttonStyleLeft.onHover.textColor = c;
                    _buttonStyleLeft.focused.textColor = c;
                    _buttonStyleLeft.onFocused.textColor = c;
                    _buttonStyleLeft.active.textColor = c;
                    _buttonStyleLeft.onActive.textColor = c;
                }
                return _buttonStyleLeft;
            }
        }
        //
        private GUIStyle _leftLabel;
        public GUIStyle leftLabel
        {
            get
            {
                if (_leftLabel == null)
                {

                }
                return _leftLabel;
            }
        }
        //
        private GUIStyle _boxStyle;
        public GUIStyle boxStyle
        {
            get
            {
                if (_boxStyle == null)
                {
                    _boxStyle = new GUIStyle(EditorStyles.helpBox);
                    _boxStyle.fontStyle = FontStyle.Normal;
                    _boxStyle.fontSize = 11;
                    _boxStyle.alignment = TextAnchor.MiddleCenter;
                    _boxStyle.stretchWidth = true;
                }
                return _boxStyle;
            }
        }
        //
        private GUIStyle _centeredTextStyle;
        public GUIStyle centeredTextStyle
        {
            get
            {
                if (_centeredTextStyle == null)
                {
                    _centeredTextStyle = new GUIStyle(EditorStyles.label);
                    _centeredTextStyle.fontStyle = FontStyle.Normal;
                    _centeredTextStyle.fontSize = 11;
                    _centeredTextStyle.fixedHeight = 24;
                    _centeredTextStyle.alignment = TextAnchor.MiddleCenter;
                    _centeredTextStyle.stretchWidth = true;
                }
                return _centeredTextStyle;
            }
        }

        public static LayerMask LayerMaskField(LayerMask selected, GUIStyle style)
        {
            if (layers == null)
            {
                layers = new List<string>();
                layerNames = new string[4];
            }
            else
            {
                layers.Clear();
            }

            int emptyLayers = 0;

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);

                if (layerName != "")
                {
                    for (; emptyLayers > 0; emptyLayers--) layers.Add("Layer " + (i - emptyLayers));
                    layers.Add(layerName);
                }
                else
                {
                    emptyLayers++;
                }
            }

            if (layerNames.Length != layers.Count)
            {
                layerNames = new string[layers.Count];
            }
            for (int i = 0; i < layerNames.Length; i++) layerNames[i] = layers[i];

            selected.value = EditorGUILayout.MaskField("", selected.value, layerNames, style);

            return selected;
        }

        public static List<string> getLayerMaskNames(LayerMask selected)
        {
            if (layers == null)
            {
                layers = new List<string>();
            }
            else
            {
                layers.Clear();
            }

            int emptyLayers = 0;

            for (int i = 0; i < 32; i++)
            {
                bool inlayermask = selected == (selected | (1 << i));
                string name = LayerMask.LayerToName(i);

                if (inlayermask && name != "")
                {
                    layers.Add(name);
                }
            }

            return layers;
        }
    }
}
#endif