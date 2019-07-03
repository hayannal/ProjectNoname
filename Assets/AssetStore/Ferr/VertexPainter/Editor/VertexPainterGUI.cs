using UnityEditor;
using UnityEngine;

namespace Ferr { 
	public class VertexPainterGUI {
		public const  float cButtonHeight = 20;
		public static Color cLineColor    = new Color(0, 0, 0, 0.4f);

		static GUIContent _brush    = new GUIContent("Brush", "Use [], ;' or <> shortcuts to adjust brush parameters");
		static GUIContent _size     = new GUIContent("Size", "[] Adjust the size of the paintbrush area");
		static GUIContent _strength = new GUIContent("Strength", ";' Adjust the strength of the paintbrush stroke");
		static GUIContent _falloff  = new GUIContent("Hardness", "<> Adjust the hardness of the paintbrush's edges as it falls off");

		static GUIContent _color    = new GUIContent("Color", "Use the 'c' shortcut to activate the vertex color picker");

		static GUIContent _tools    = new GUIContent("Tools", "Yo, this sections is just miscellaneous stuff, but I'm glad you found the tooltips :)");
		static GUIContent _fill     = new GUIContent(VertexPainterIcons.Instance.fill, "Fills the entire mesh with the same vertex color");
		static GUIContent _help     = new GUIContent("Help?", "Shortcut reference, links, email, hopefully useful things");
		static GUIContent _exportmesh = new GUIContent("Export Mesh", "Export current painted mesh with color");
		static string _backfaceTip = "Toggle for excluding or including verts that face away from the camera";

		internal static void RenderGUI(IBlendPaintType aPainter) {
			Rect r;

			if (aPainter != null) { 
				if (aPainter.ShowBrushSettings) { 
					EditorGUILayout.Space();
					VertexEditorUtil.DrawUILine(cLineColor, 2, 0);
					EditorGUILayout.LabelField(_brush, VertexPainterIcons.Instance.centeredLabel);
					VertexEditorUtil.DrawUILine(cLineColor, 2, 0);
					EditorGUILayout.Space();

					DrawBrushPreview(aPainter);

					// show brush control sliders
					EditorGUILayout.LabelField(_size);
					r = EditorGUILayout.GetControlRect();
					aPainter.Size     = GUI.HorizontalSlider(r, aPainter.Size/20, 0.001f, 1) * 20;
					EditorGUILayout.LabelField(_strength);
					r = EditorGUILayout.GetControlRect();
					aPainter.Strength = GUI.HorizontalSlider(r, aPainter.Strength, 0.001f, 1);
					EditorGUILayout.LabelField(_falloff);
					r = EditorGUILayout.GetControlRect();
					aPainter.Falloff  = 1-GUI.HorizontalSlider(r, 1-aPainter.Falloff, 0, 1);
				}

				if (aPainter.ShowColorSettings) { 
					EditorGUILayout.Space();
					VertexEditorUtil.DrawUILine(cLineColor, 2, 0);
					EditorGUILayout.LabelField(_color, VertexPainterIcons.Instance.centeredLabel);
					VertexEditorUtil.DrawUILine(cLineColor, 2, 0);
					EditorGUILayout.Space();

					// color picker + hsv
					aPainter.Color = EditorGUILayout.ColorField(aPainter.Color);
					EditorGUILayout.Space();

					float h, s, v;
					float nh, ns, nv;
					Color.RGBToHSV(aPainter.Color, out h, out s, out v);
					r = EditorGUILayout.GetControlRect();
					GUI.DrawTexture(new Rect(r.x, r.y, r.width, 4), VertexPainterIcons.Instance.MakeHSV(0, 64, 4, 0, 1, s, s, v, v));
					nh  = GUI.HorizontalSlider(r, h, 0, 1);
					r = EditorGUILayout.GetControlRect();
					GUI.DrawTexture(new Rect(r.x, r.y, r.width, 4), VertexPainterIcons.Instance.MakeHSV(1, 64, 4, h, h, 0, 1, v, v));
					ns  = GUI.HorizontalSlider(r, s, 0, 1);
					r = EditorGUILayout.GetControlRect();
					GUI.DrawTexture(new Rect(r.x, r.y, r.width, 4), VertexPainterIcons.Instance.MakeHSV(2, 64, 4, h, h, s, s, 0, 1));
					nv  = GUI.HorizontalSlider(r, v, 0, 1);

					if (nh != h || ns != s || nv != v) {
						aPainter.Color = Color.HSVToRGB(nh, ns, nv);
					}
				}

				aPainter.DrawToolGUI();
			}

			EditorGUILayout.Space();
			VertexEditorUtil.DrawUILine(cLineColor, 2, 0);
			EditorGUILayout.LabelField(_tools, VertexPainterIcons.Instance.centeredLabel);
			VertexEditorUtil.DrawUILine(cLineColor, 2, 0);
			EditorGUILayout.Space();

			if (GUILayout.Button(_fill, VertexPainterIcons.Instance.button, GUILayout.Height(cButtonHeight))) {
				VertexPainterWindow.Instance.DoFill();
			}
			if (aPainter != null) {
				aPainter.Backfaces = GUILayout.Toggle(aPainter.Backfaces, new GUIContent(aPainter.Backfaces ? VertexPainterIcons.Instance.backfaceOn : VertexPainterIcons.Instance.backfaceOff, _backfaceTip), VertexPainterIcons.Instance.button, GUILayout.Height(cButtonHeight));
			}
			if (GUILayout.Button(_help, VertexPainterIcons.Instance.button, GUILayout.Height(cButtonHeight))) {
				EditorWindow.GetWindow<Ferr.VertexPainterHelp>("Help");
			}
		}

		private static void DrawBrushPreview(IBlendPaintType aPainter) {
			Rect r;
			EditorGUILayout.BeginHorizontal();
			GUILayout      .FlexibleSpace();
			EditorGUILayout.LabelField("", GUILayout.Height(32));
			GUILayout      .FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			r = GUILayoutUtility.GetLastRect();
			r.yMin-=1;
			r.xMax += 4;
			EditorGUI.DrawRect(r, VertexPainterIcons.Instance.lineColor);
			r.xMin += r.width/2 - 16;
			EditorGUI.LabelField(r, new GUIContent(VertexPainterIcons.Instance.MakeBrush(32, Mathf.Lerp(0.2f, 1, aPainter.Strength), aPainter.Falloff, Mathf.Lerp(0.5f, 1.0f, Mathf.Clamp01(aPainter.Size/20)))));
		}
	}
}