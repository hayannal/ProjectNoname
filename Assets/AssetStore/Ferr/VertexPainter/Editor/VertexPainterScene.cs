using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ferr {
	public class VertexPainterScene {
		#region Constants
		const float cSizeJump     = 0.2f;
		const float cStrJump      = 0.3f;
		const float cFalloffJump  = 0.1f;
		#endregion

		// Oh my god Unity, what is this arc rotation mess you've done?
		static bool _mouseDown   = false;
		static bool _arcRotating = false;
		public static bool ArcRotating { get { return _arcRotating; } }

		internal static void RenderGUI(SceneView aView, IBlendPaintType aPainter) {
			if (ArcRotating || Event.current.type != EventType.Repaint)
				return;
			
			RaycastHit hit;
			if (ClosestPoint(out hit)) {
				Color t          = Handles.color;
				float strPercent = (1-1/((aPainter.Strength+0.05f)*20));

				if (aPainter.ShowBrushSettings) {
					// Draw the brush ring
					Handles.color = VertexPainterSettings.BrushColor;
					if (VertexPainterSettings.StrengthPreviewMode == VertexPainterSettings.StrengthPreviewModeType.Dashes && strPercent <0.95f) {
						float scale = HandleUtility.GetHandleSize(hit.point) * 0.5f;
						VertexEditorUtil.DashCircle(hit.point, hit.normal, aPainter.Size, Mathf.Lerp(.2f, .8f, strPercent*strPercent), Mathf.Lerp(.6f, 0.0f, strPercent*strPercent), scale);
					} else {
						Handles.DrawWireDisc(hit.point, hit.normal, aPainter.Size);
					}

					// draw the inner falloff ring
					if (VertexPainterSettings.ShowFalloffRing) {
						Handles.color = new Color(aPainter.Color.r, aPainter.Color.g, aPainter.Color.b, 1f);
						Handles.DrawWireDisc(hit.point, hit.normal, Mathf.Max(0.1f, (aPainter.Size - 0.05f) * (1-aPainter.Falloff)));
					}

					// Draw strength indicator spike
					if (VertexPainterSettings.StrengthPreviewMode == VertexPainterSettings.StrengthPreviewModeType.Spike) {
						VertexEditorUtil.GradientLine(hit.point + hit.normal * (1-strPercent), hit.point + hit.normal, Color.Lerp(Color.black, VertexPainterSettings.BrushColor, strPercent), VertexPainterSettings.BrushColor);
						Handles.color = Color.Lerp(Color.black, VertexPainterSettings.BrushColor, strPercent);
						Handles.SphereHandleCap(0, hit.point + hit.normal * (1-strPercent), Quaternion.identity, 0.075f, Event.current.type);
					}
				}
		        
				// show the preview verts
				if (VertexPainterSettings.VertPreviewMode != VertexPainterSettings.VertPreviewModeType.None) {
					List<GameObject> objs = VertexPainterWindow.Instance.PaintObjects;
					aPainter.RenderScenePreview(aView.camera, hit, objs);
				}
				Handles.color = t;
			}
		}
		static bool ClosestPoint(out RaycastHit aHit) {
			Ray        ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			RaycastHit hit;
			float      closest  = float.MaxValue;
			bool       result = false;
			aHit = default(RaycastHit);

			List<GameObject> objs = VertexPainterWindow.Instance.PaintObjects;
			for (int i = 0; i < objs.Count; i++) {
				Collider c = objs[i].GetComponent<Collider>();
				if (c!=null && c.Raycast(ray, out hit, float.MaxValue)) {
					if (hit.distance < closest) {
						closest = hit.distance;
						aHit    = hit;
						hit     = default(RaycastHit);
						result  = true;
					}
				}
			}
			return result;
		}

		internal static void Input(SceneView aView, IBlendPaintType aPainter) {
			if (Event.current.alt)
				_arcRotating = true;
			
			if (Event.current.type == EventType.KeyDown) {
				if (Event.current.keyCode == KeyCode.LeftBracket ) { aPainter.Size     -= Mathf.Max(aPainter.Size,     0.2f) * cSizeJump; DoRepaint(); Event.current.Use(); }
				if (Event.current.keyCode == KeyCode.RightBracket) { aPainter.Size     += Mathf.Max(aPainter.Size,     0.2f) * cSizeJump; DoRepaint(); Event.current.Use(); }
				if (Event.current.keyCode == KeyCode.Semicolon   ) { aPainter.Strength -= Mathf.Max(aPainter.Strength, 0.2f) * cStrJump; aPainter.Strength = Mathf.Clamp(aPainter.Strength, 0.01f, 1); DoRepaint(); Event.current.Use(); }
				if (Event.current.keyCode == KeyCode.Quote       ) { aPainter.Strength += Mathf.Max(aPainter.Strength, 0.2f) * cStrJump; aPainter.Strength = Mathf.Clamp(aPainter.Strength, 0.01f, 1); DoRepaint(); Event.current.Use(); }
				if (Event.current.keyCode == KeyCode.Comma       ) { aPainter.Falloff  += cFalloffJump;                 aPainter.Falloff  = Mathf.Clamp01(aPainter.Falloff);  DoRepaint(); Event.current.Use(); }
				if (Event.current.keyCode == KeyCode.Period      ) { aPainter.Falloff  -= cFalloffJump;                 aPainter.Falloff  = Mathf.Clamp01(aPainter.Falloff);  DoRepaint(); Event.current.Use(); }
				
				if (Event.current.keyCode == KeyCode.LeftBracket  && Event.current.modifiers == EventModifiers.Shift) { aPainter.Size     = 0.25f; DoRepaint(); Event.current.Use(); }
				if (Event.current.keyCode == KeyCode.RightBracket && Event.current.modifiers == EventModifiers.Shift) { aPainter.Size     = 2;     DoRepaint(); Event.current.Use(); }
				if (Event.current.keyCode == KeyCode.Semicolon    && Event.current.modifiers == EventModifiers.Shift) { aPainter.Strength = 0.1f;  DoRepaint(); Event.current.Use(); }
				if (Event.current.keyCode == KeyCode.Quote        && Event.current.modifiers == EventModifiers.Shift) { aPainter.Strength = 1.0f;  DoRepaint(); Event.current.Use(); }
				if (Event.current.keyCode == KeyCode.Comma        && Event.current.modifiers == EventModifiers.Shift) { aPainter.Falloff  = 0.0f;  DoRepaint(); Event.current.Use(); }
				if (Event.current.keyCode == KeyCode.Period       && Event.current.modifiers == EventModifiers.Shift) { aPainter.Falloff  = 1.0f;  DoRepaint(); Event.current.Use(); }
				
			} else if (Event.current.type == EventType.ScrollWheel) {
				float scroll = Event.current.delta.y;

				// Correct scroll value on Mac OS. Scroll shifts to horizontal when shift is held down.
				#if UNITY_EDITOR_OSX
				if (Event.current.shift) {
					scroll = Event.current.delta.x;
				}
				#endif

				if (Event.current.shift && Event.current.control) {
					aPainter.Falloff  += (scroll/6) * cFalloffJump;
					aPainter.Falloff = Mathf.Clamp01(aPainter.Falloff);
					DoRepaint(); 
					Event.current.Use();
				} else if (Event.current.shift) {
					aPainter.Strength -= (scroll/6) * Mathf.Max(aPainter.Strength, 0.2f) * cStrJump;
					aPainter.Strength = Mathf.Clamp(aPainter.Strength, 0.01f, 1);
					DoRepaint();
					Event.current.Use();
				} else if (Event.current.control) {
					aPainter.Size     -= (scroll/6) * Mathf.Max(aPainter.Size, 0.2f) * cSizeJump;
					aPainter.Size     = Mathf.Max(0.1f, aPainter.Size);
					DoRepaint();
					Event.current.Use();
				}
			}
			else if (Event.current.type == EventType.MouseDown) {
				_mouseDown = true;
				if (Event.current.button == 0 && !ArcRotating) {
					RaycastHit hit;
					if (ClosestPoint(out hit)) {
						VertexPainterWindow.Instance.BeginPaint(hit);
						VertexPainterWindow.Instance.DoPaint   (hit);
					} else {
						VertexPainterWindow.Instance.BeginPaint(default(RaycastHit));
					}
					Event.current.Use();
				}
			}
			else if (Event.current.type == EventType.MouseDrag) {
				if (Event.current.button == 0 && !ArcRotating) {
					RaycastHit hit;
					if (ClosestPoint(out hit)) {
						VertexPainterWindow.Instance.DoPaint(hit);
					}
					Event.current.Use();
				}
			}
			else if (Event.current.type == EventType.MouseUp) {
				if (Event.current.button == 0 && !ArcRotating) {
					RaycastHit hit;
					if (ClosestPoint(out hit)) {
						VertexPainterWindow.Instance.EndPaint(hit);
					} else {
						VertexPainterWindow.Instance.EndPaint(default(RaycastHit));
					}
					Event.current.Use();
				}
				_mouseDown   = false;
				_arcRotating = Event.current.alt;
			}
			else if (Event.current.type == EventType.MouseMove) {
				HandleUtility.Repaint();
			}
			else if (Event.current.type == EventType.Layout) {
				HandleUtility.AddDefaultControl(aView.GetHashCode());
			}

			if (_arcRotating && !_mouseDown && !Event.current.alt)
				_arcRotating = false;
		}

		static void DoRepaint() {
			VertexPainterWindow.Instance.Repaint();
		}
	}
}