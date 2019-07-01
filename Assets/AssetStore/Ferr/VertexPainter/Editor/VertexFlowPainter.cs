using UnityEditor;
using UnityEngine;

namespace Ferr {
	class VertexFlowPainter : VertexColorPainter {
		public override bool   ShowColorSettings { get { return false; } }
		public override string Name { get { return "Flow"; } }

		[SerializeField] float _speed = 1;

		public override void Paint(GameObject aObject, RaycastHit aHit, RaycastHit? aPreviousHit) {
			if (!aPreviousHit.HasValue)
				return;

			MeshFilter paintMesh = aObject.GetComponent<MeshFilter>();
			Vector3    objScale  = aObject.transform.lossyScale;
			Vector3    point     = aObject.transform.InverseTransformPoint(aHit.point);
			Vector3    dir       = aObject.transform.InverseTransformDirection(UnityEditor.SceneView.currentDrawingSceneView.camera.transform.forward);
			Vector3    prevPoint = aObject.transform.InverseTransformPoint(aPreviousHit.Value.point);
			
			Vector3[] verts  = paintMesh.sharedMesh.vertices;
			Vector3[] norms  = paintMesh.sharedMesh.normals;
			Vector4[] tans   = paintMesh.sharedMesh.tangents;
			Color  [] colors = paintMesh.sharedMesh.colors;
		
			if (colors.Length != verts.Length) colors = new Color[verts.Length];
		
			for (int v = 0; v < verts.Length; v++) {
				float falloff = GetPointInfluence(aObject.transform.lossyScale, point, dir, verts[v], norms[v]);
				if (falloff <= 0)
					continue;
			
				// calculate stuff for transforming our verts into tangent/texture space
				Vector3   biNormal = Vector3.Cross(norms[v], tans[v]);
				Matrix4x4 mat      = Matrix4x4.identity;
				mat.SetRow(0, new Vector4(tans[v].x, tans[v].y, tans[v].z, 0));
				mat.SetRow(1, biNormal);
				mat.SetRow(2, norms[v]);
			
				// now figure out the direction relative to the texture~
				Vector3 texDir = mat.MultiplyVector(point - prevPoint);
				texDir.z = -texDir.y;
				texDir.y = 0;
				texDir.Normalize();
				texDir = (texDir + Vector3.one) * 0.5f;
			
				Color newColor = Color.Lerp(colors[v], new Color(texDir.x, _speed, texDir.z, 0), falloff * Strength);
				colors[v] = newColor;
			}
			paintMesh.sharedMesh.colors = colors;
		}

		public override int CheckPriority(GameObject aOfObject) {
			Renderer r = aOfObject.GetComponent<Renderer>();
			if (r == null)
				return (int)PaintPriority.None;
			if (r.sharedMaterial.shader.name.Contains("Flow"))
				return (int)PaintPriority.Specific;
			return (int)PaintPriority.Capable;
		}

		public override void RenderScenePreview(Camera aSceneCamera, RaycastHit aHit, GameObject aObject) {
			Handles.color = Color.red;
			MeshFilter filter = aObject.GetComponent<MeshFilter>();
			Vector3    camDir = aSceneCamera.transform.forward;
		
			float size = HandleUtility.GetHandleSize(filter.transform.position) * 0.05f;
			Vector3[] verts  = filter.sharedMesh.vertices;
			Vector3[] norms  = filter.sharedMesh.normals;
			Color  [] color  = filter.sharedMesh.colors;
			Vector4[] tans   = filter.sharedMesh.tangents;
			Vector3   hitPt  = filter.transform.InverseTransformPoint(aHit.point);
			Vector3   dir    = filter.transform.InverseTransformDirection(camDir);
			Vector3   scale  = filter.transform.lossyScale;
			            
			if (color.Length != verts.Length)
				color = new Color[verts.Length];
			            
			for (int v = 0; v < verts.Length; v++) {
				float influence = GetPointInfluence(scale, hitPt, dir, verts[v], norms[v]);
			
				if (influence > 0) {
					// calculate the matrix to take it from texture space into world space
					Vector3   biNormal = Vector3.Cross(norms[v], tans[v]);
					Matrix4x4 mat      = Matrix4x4.identity;
					mat.SetRow(0, new Vector4(tans[v].x, tans[v].y, tans[v].z, 0));
					mat.SetRow(1, biNormal);
					mat.SetRow(2, norms[v]);
					mat = filter.transform.localToWorldMatrix * mat.inverse;

					if (color != null && VertexPainterSettings.VertPreviewMode == VertexPainterSettings.VertPreviewModeType.Colors) {
						Handles.color = new Color(color[v].r, color[v].g, color[v].b, 1);
					} else if (VertexPainterSettings.VertPreviewMode == VertexPainterSettings.VertPreviewModeType.Falloff) {
						Handles.color = Color.Lerp(Color.black, Color.red, influence/Size);
					}
					Vector3 flow = mat.MultiplyVector(new Vector3((color[v].r-0.5f), -(color[v].b-0.5f), 0)).normalized * color[v].b * size * 5;

					Handles.DrawLine(filter.transform.TransformPoint(verts[v]), filter.transform.TransformPoint(verts[v]) + flow);
					EditorTools.CircleCapBase(0, filter.transform.TransformPoint(verts[v]), Quaternion.identity, size * influence, Event.current.type);
				}
			}
		}
		public override void DrawToolGUI() {
			Rect r;
			EditorGUILayout.LabelField("Flow Speed");
			r = EditorGUILayout.GetControlRect();
			_speed = GUI.HorizontalSlider(r, _speed, 0, 1);
		}
		public override bool GUIInput() {
			bool result = false;
			if (Event.current.type == EventType.KeyDown) {
				if (Event.current.keyCode == KeyCode.Alpha1) {
					_speed = 0;
					Event.current.Use();
					result = true;
				} else if (Event.current.keyCode == KeyCode.Alpha2) {
					_speed = .33f;
					Event.current.Use();
					result = true;
				} else if (Event.current.keyCode == KeyCode.Alpha3) {
					_speed = .66f;
					Event.current.Use();
					result = true;
				} else if (Event.current.keyCode == KeyCode.Alpha4) {
					_speed = 1;
					Event.current.Use();
					result = true;
				}
			}
			return result;
		}
	}
}