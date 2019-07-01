using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ferr { 
	public class VertexColorPicker : VertexColorPainter {
		IBlendPaintType _painter;

		public override string    Name              { get { return "Picker"; } }
		public override Texture2D Cursor            { get { return VertexPainterIcons.Instance.eyedropper; } }
		public override Vector2   CursorHotspot     { get { return new Vector2(0,8); } }
		public override bool      ShowBrushSettings { get { return false; } }

		public void Initialize(IBlendPaintType aPainter) {
			_painter   = aPainter;
			_falloff   = 1;
			_backfaces = false;
			_size      = 0;
		}

		public override void PaintObjects(List<GameObject> aObjects, RaycastHit aHit, RaycastHit? aPreviousHit) {
			Vector3 vertPos;
			Color = GetClosest(aObjects, aHit.point, out vertPos);
			_painter.Color = Color;
			VertexPainterWindow.Instance.Repaint();
		}
		public override void PaintBegin(GameObject aObject, RaycastHit aHit, RaycastHit? aPreviousHit) {
		}
		public override void PaintEnd  (GameObject aObject, RaycastHit aHit, RaycastHit? aPreviousHit) {
		}
		public override void Paint     (GameObject aObject, RaycastHit aHit, RaycastHit? aPreviousHit) {
		}

		Color GetClosest(List<GameObject> aObjects, Vector3 aAt, out Vector3 aVertPos) {
			float closest = float.MaxValue;
			Color result  = Color.white;
			aVertPos = aAt;

			for (int i = 0; i < aObjects.Count; i++) {
				MeshFilter paintMesh = aObjects[i].GetComponent<MeshFilter>();
				Vector3    objScale  = aObjects[i].transform.lossyScale;
				Vector3    point     = aObjects[i].transform.InverseTransformPoint(aAt);
				Vector3    dir       = aObjects[i].transform.InverseTransformDirection(UnityEditor.SceneView.currentDrawingSceneView.camera.transform.forward);

				Vector3[] verts = paintMesh.sharedMesh.vertices;
				Vector3[] norms = paintMesh.sharedMesh.normals;
				Color  [] colors = paintMesh.sharedMesh.colors;

				if (colors.Length != verts.Length) colors = new Color[verts.Length];
			
				for (int v = 0; v < verts.Length; v++) {
					float falloff = GetPointInfluence(objScale, point, dir, verts[v], norms[v]);
					if (falloff < closest) {
						closest = falloff;
						result = colors[v];
						aVertPos = paintMesh.transform.TransformPoint(verts[v]);
					}
				}
			}
			return result;
		}

		public override float GetPointInfluence(Vector3 aObjScale, Vector3 aHitPt, Vector3 aHitDirection, Vector3 aVert, Vector3 aVertNormal) {
			Vector3 diff = aVert - aHitPt;
			diff = new Vector3(
				diff.x * aObjScale.x,
				diff.y * aObjScale.y,
				diff.z * aObjScale.z);

			float sqrMagnitude = diff.sqrMagnitude;
			if ((!_backfaces && Vector3.Dot(aHitDirection, aVertNormal) > 0))
				return float.MaxValue;
			return sqrMagnitude;
		}
	
		public override void RenderScenePreview(Camera aSceneCamera, RaycastHit aHit, List<GameObject> aObjects) {
			Vector3 vertPos = Vector3.zero;
			Color   result  = GetClosest(aObjects, aHit.point, out vertPos);

			Handles.color = Color.black;
			EditorTools.CircleCapBase(0, vertPos, Quaternion.identity, HandleUtility.GetHandleSize(vertPos)*0.2f, Event.current.type);
			result.a = 1;
			Handles.color = result;
			EditorTools.CircleCapBase(0, vertPos, Quaternion.identity, HandleUtility.GetHandleSize(vertPos)*0.15f, Event.current.type);
		}
		public override int CheckPriority(GameObject aOfObject) {
			return (int)PaintPriority.None;
		}
	}
}