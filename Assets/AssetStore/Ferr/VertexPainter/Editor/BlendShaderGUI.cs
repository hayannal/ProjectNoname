using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

namespace Ferr {
	public class BlendShaderGUI : ShaderGUI {
		string[] _blendModes      = new string[] { "BLEND_HEIGHT", "BLEND_HARD", "BLEND_SOFT" };
		string[] _blendModeDesc   = new string[] { "Height",       "Hard",       "Soft"       };
		string[] _textureModes    = new string[] { "BLEND_TEX_2", "BLEND_TEX_3", "BLEND_TEX_4" };
		string[] _textureModeDesc = new string[] { "2 Textures",  "3 Textures",  "4 Textures"  };
		int   [] _textureCounts   = new int   [] { 2,             3,             4             };
		string[] _textureNames    = new string[] { "Red", "Green", "Blue", "Alpha" };
		string[] _uvModes         = new string[] { "BLEND_WORLDUV_Off", "BLEND_WORLDUV" };
		string[] _uvModeDesc      = new string[] { "Normal", "From World Coordinates" };

		MaterialProperty _blendStrength;
		MaterialProperty[] _diffuse;
		MaterialProperty[] _bump;
		MaterialProperty[] _spec;
		MaterialProperty[] _color;

		int  _blendMode    = 0;
		int  _texIndex     = 1;
		int  _uvIndex      = 0;

		PreviewRenderUtility _previewRenderUtility;
		Mesh                 _previewMesh;
		Vector2              _previewDir = new Vector2(180, -20);

		protected virtual void FindProperties(MaterialProperty[] aProps) {
			_blendStrength = FindProperty("_BlendStrength", aProps);
		
			_diffuse = new MaterialProperty[0];
			_bump    = new MaterialProperty[0];
			_spec    = new MaterialProperty[0];
			_color   = new MaterialProperty[0];
			for (int i=0; i<4; i+=1) {
				string num = i==0?"":(i+1).ToString();
			
				MaterialProperty prop = FindProperty("_MainTex"+num, aProps, false);
				if (prop != null) ArrayUtility.Add(ref _diffuse, prop);
				prop = FindProperty("_BumpMap"+num, aProps, false);
				if (prop != null) ArrayUtility.Add(ref _bump, prop);
				prop = FindProperty("_SpecTex"+num, aProps, false);
				if (prop != null) ArrayUtility.Add(ref _spec, prop);
				prop = FindProperty("_Color"+num, aProps, false);
				if (prop != null) ArrayUtility.Add(ref _color, prop);
			}
		}
	
		public override void OnGUI(MaterialEditor aMaterialEditor, MaterialProperty[] aProps) {
			Material mat = aMaterialEditor.target as Material;
			FindProperties     (aProps);
			ShaderPropertiesGUI(aMaterialEditor, mat);
		}
		protected virtual void ShaderPropertiesGUI(MaterialEditor aEditor, Material aMat) {
			KeywordGUI(aEditor, aMat);
			ChannelGUI(aEditor);
		}
	
		protected void ChannelGUI(MaterialEditor aEditor) {
			for (int i=0; i<_textureCounts[_texIndex]; i+=1) {
				GUILayout.Label(_textureNames[i] + " Channel", EditorStyles.boldLabel);
			
				if (_diffuse.Length > i) aEditor.TexturePropertySingleLine(new GUIContent(_diffuse[i].displayName), _diffuse[i]);
				if (_bump   .Length > i) aEditor.TexturePropertySingleLine(new GUIContent(_bump   [i].displayName), _bump[i]);
				if (_spec   .Length > i) aEditor.TexturePropertySingleLine(new GUIContent(_spec   [i].displayName), _spec[i]);
				if (_color  .Length > i) aEditor.ColorProperty            (_color[i], _color[i].displayName);
				if (_diffuse.Length > i) aEditor.TextureScaleOffsetProperty(_diffuse[i]);
			
				GUILayout.Space(8);
			}
		}
		protected void KeywordGUI(MaterialEditor aEditor, Material aMat) {
			EditorGUI.BeginChangeCheck();
			List<string> keys = new List<string>(aMat.shaderKeywords);
		
			for (int i=0; i<keys.Count; i+=1) {
				int index = Array.IndexOf(_blendModes,   keys[i]);
				_blendMode = index == -1 ? _blendMode : index;
			
				index     = Array.IndexOf(_textureModes, keys[i]);
				_texIndex  = index == -1 ? _texIndex  : index;

				index     = Array.IndexOf(_uvModes, keys[i]);
				_uvIndex   = index == -1 ? _uvIndex : index;
			}
			_blendMode = EditorGUILayout.Popup("Blend mode",    _blendMode, _blendModeDesc  );
			_texIndex  = EditorGUILayout.Popup("Texture count", _texIndex,  _textureModeDesc);
			_uvIndex   = EditorGUILayout.Popup("UV Mode",       _uvIndex,   _uvModeDesc);
			GUILayout.Space(8);
		
			if (EditorGUI.EndChangeCheck())
				SetKeywords(aMat);

			aEditor.ShaderProperty(_blendStrength, "BlendStrength");
		}
		void SetKeywords(Material aMat) {
			aMat.shaderKeywords = new string[] {
				_blendModes  [_blendMode],
				_textureModes[_texIndex ],
				_uvModes     [_uvIndex  ]
			};
		}

		#region Material preview with vertex colors
		private void ValidateData() {
			if (_previewRenderUtility == null) {
				_previewRenderUtility = new PreviewRenderUtility();
#if UNITY_2017_1_OR_NEWER
				Camera camera = _previewRenderUtility.camera;
#else
				Camera camera = _previewRenderUtility.m_Camera;
#endif
				camera.transform.position = new Vector3(0, 0, 6);
				camera.transform.rotation = Quaternion.identity;

				_previewMesh = new Mesh();

				GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				_previewMesh = UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
				GameObject.DestroyImmediate(go);

				Vector3[] colorPts = new Vector3[] {
					new Vector3(1,0,0),
					new Vector3(0.7f,0,0.7f),
					new Vector3(0,0,1),
					new Vector3(-0.7f,0,0.7f),
					new Vector3(-1,0,0),
					new Vector3(-0.7f,0,-0.7f),
					new Vector3(0,0,-1),
					new Vector3(0.7f,0,-0.7f)
				};
				Color[] colors = new Color[] {
					new Color(1,0,0,0),
					new Color(0.51f,0.5f,0,0),
					new Color(0,1,0,0),
					new Color(0,0.51f,0.5f,0),
					new Color(0,0,1,0),
					new Color(0,0,0.51f,0.5f),
					new Color(0,0,0,1),
					new Color(0.5f,0,0,0.51f)
				};
				Ferr.RecolorTree tree = new Ferr.RecolorTree(colorPts, colors);
				tree.Recolor(ref _previewMesh);
			}
		}
		public override void OnMaterialInteractivePreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background) {
			ValidateData();
			_previewDir = Drag2D(_previewDir, r);
			if (Event.current.type == EventType.Repaint) {
#if UNITY_2017_1_OR_NEWER
				Camera camera = _previewRenderUtility.camera;
#else
				Camera camera = _previewRenderUtility.m_Camera;
#endif
				camera.transform.position = -Vector3.forward * 5f;
				Quaternion quaternion = Quaternion.Euler(_previewDir.y, 0f, 0f) * Quaternion.Euler(0f, _previewDir.x, 0f);
				camera.transform.position = Quaternion.Inverse(quaternion) * camera.transform.position;
				camera.transform.LookAt(Vector3.zero);

				_previewRenderUtility.BeginPreview(r, background);
				_previewRenderUtility.DrawMesh(_previewMesh, Matrix4x4.identity, materialEditor.target as Material, 0);
				camera.Render();

				Texture resultRender = _previewRenderUtility.EndPreview();

				GUI.DrawTexture(r, resultRender, ScaleMode.StretchToFill, false);
			}
		}
		public static Vector2 Drag2D(Vector2 scrollPosition, Rect position) {
			int controlID = GUIUtility.GetControlID("Slider".GetHashCode(), FocusType.Passive);
			Event current = Event.current;
			EventType typeForControl = current.GetTypeForControl(controlID);
			if (typeForControl != EventType.MouseDown) {
				if (typeForControl != EventType.MouseDrag) {
					if (typeForControl == EventType.MouseUp) {
						if (GUIUtility.hotControl == controlID) {
							GUIUtility.hotControl = 0;
						}
						EditorGUIUtility.SetWantsMouseJumping(0);
					}
				} else if (GUIUtility.hotControl == controlID) {
					scrollPosition -= current.delta * (float)((!current.shift) ? 1 : 3) / Mathf.Min(position.width, position.height) * 140f;
					scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90f, 90f);
					current.Use();
					GUI.changed = true;
				}
			} else if (position.Contains(current.mousePosition) && position.width > 50f) {
				GUIUtility.hotControl = controlID;
				current.Use();
				EditorGUIUtility.SetWantsMouseJumping(1);
			}
			return scrollPosition;
		}
#endregion
	}
}