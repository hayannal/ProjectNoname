using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ferr { 
	public class VertexTexPainter : VertexColorPainter {
		Renderer _renderer;

		static GUIContent _tex1 = new GUIContent("", "'1' Paints on the RED channel, using _MainTex");
		static GUIContent _tex2 = new GUIContent("", "'2' Paints on the GREEN channel, using _MainTex2");
		static GUIContent _tex3 = new GUIContent("", "'3' Paints on the BLUE channel, using _MainTex3");
		static GUIContent _tex4 = new GUIContent("", "'4' Paints on the ALPHA channel, using _MainTex4");

		public override string Name { get { return "Blend"; } }

		public override int CheckPriority(GameObject aOfObject) {
			Renderer r = aOfObject.GetComponent<Renderer>();
			if (r != null && (r.sharedMaterial.HasProperty("_MainTex2") || r.sharedMaterial.shader.name.Contains("Blend")))
				return (int)PaintPriority.Specific;
			return (int)PaintPriority.None;
		}
		public override void OnSelect(List<GameObject> aObjects) {
			base.OnSelect(aObjects);

			if (aObjects.Count > 0) {
				_renderer = aObjects[0].GetComponent<Renderer>();
			}
		}
		public override void DrawToolGUI() {
			Rect      r;
			Texture[] textures = new Texture[4];
			if (_renderer != null) {
				Material m = _renderer.sharedMaterial;
				textures[0] = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") as Texture : EditorGUIUtility.whiteTexture;
				textures[1] = m.HasProperty("_MainTex2") ? m.GetTexture("_MainTex2") as Texture : EditorGUIUtility.whiteTexture;
				textures[2] = m.HasProperty("_MainTex3") ? m.GetTexture("_MainTex3") as Texture : EditorGUIUtility.whiteTexture;
				textures[3] = m.HasProperty("_MainTex4") ? m.GetTexture("_MainTex4") as Texture : EditorGUIUtility.whiteTexture;
			}
			
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(_tex1, VertexPainterIcons.Instance.button, GUILayout.Height(VertexPainterGUI.cButtonHeight)))
				Color = new Color(1, 0, 0, 0);
			r = GUILayoutUtility.GetLastRect();
			if (textures[0] != null) GUI.DrawTexture(r, textures[0], ScaleMode.ScaleAndCrop, false);
			if (Color == new Color(1, 0, 0, 0)) {
				r.y = r.yMax;
				r.height = 2;
				GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
			}

			if (GUILayout.Button(_tex2, VertexPainterIcons.Instance.button, GUILayout.Height(VertexPainterGUI.cButtonHeight)))
				Color = new Color(0, 1, 0, 0);
			r = GUILayoutUtility.GetLastRect();
			if (textures[1] != null) GUI.DrawTexture(r, textures[1], ScaleMode.ScaleAndCrop, false);
			if (Color == new Color(0, 1, 0, 0)) {
				r.y = r.yMax;
				r.height = 2;
				GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
			}

			if (GUILayout.Button(_tex3, VertexPainterIcons.Instance.button, GUILayout.Height(VertexPainterGUI.cButtonHeight)))
				Color = new Color(0, 0, 1, 0);
			r = GUILayoutUtility.GetLastRect();
			if (textures[2] != null) GUI.DrawTexture(r, textures[2], ScaleMode.ScaleAndCrop, false);
			if (Color == new Color(0, 0, 1, 0)) {
				r.y = r.yMax;
				r.height = 2;
				GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
			}

			if (GUILayout.Button(_tex4, VertexPainterIcons.Instance.button, GUILayout.Height(VertexPainterGUI.cButtonHeight)))
				Color = new Color(0, 0, 0, 1);
			r = GUILayoutUtility.GetLastRect();
			if (textures[3] != null) GUI.DrawTexture(r, textures[3], ScaleMode.ScaleAndCrop, false);
			if (Color == new Color(0, 0, 0, 1)) {
				r.y = r.yMax;
				r.height = 2;
				GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
			}

			EditorGUILayout.EndHorizontal(); 
		}
	}
}