using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ferr { 
	[System.Serializable]
	public class VertexColorPainter : ScriptableObject, IBlendPaintType {
		[SerializeField] protected float _strength  = 0.5f;
		[SerializeField] protected float _falloff   = 1;
		[SerializeField] protected float _size      = 1;
		[SerializeField] protected bool  _backfaces = false;
		[SerializeField] protected Color _color     = new Color(1,0,0,0);

		protected List<Object> _addedObjects = new List<Object>();
		protected Dictionary<Object, Color[]> _undoColors = new Dictionary<Object, Color[]>();

		public float Strength  { get { return _strength;  } set { _strength  = value; } }
		public float Falloff   { get { return _falloff;   } set { _falloff   = value; } }
		public float Size      { get { return _size;      } set { _size      = value; } }
		public bool  Backfaces { get { return _backfaces; } set { _backfaces = value; } }
		public Color Color     { get { return _color;     } set { _color     = value; } }

		public virtual Texture2D Cursor            { get { return VertexPainterIcons.Instance.cursor; } }
		public virtual Vector2   CursorHotspot     { get { return Vector2.zero; } }
		public virtual bool      ShowColorSettings { get { return true; } }
		public virtual bool      ShowBrushSettings { get { return true; } }
		public virtual string    Name              { get { return "Color"; } }

		public virtual void OnSelect  (List<GameObject> aObjects) {
			for (int i = 0; i < aObjects.Count; i++) {
				IBlendPaintable paintable = aObjects[i].GetComponent<IBlendPaintable>();
				if (paintable != null) {
					paintable.OnPainterSelected(this);
				}

				Collider c = aObjects[i].GetComponent<Collider>();
				if (c == null) {
					c = aObjects[i].AddComponent<MeshCollider>();
					c.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
					_addedObjects.Add(c);
				}
				UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(c, false);
			}
		}
		public virtual void OnUnselect(List<GameObject> aObjects) {
			for (int i = _addedObjects.Count-1; i >= 0; i--) {
				if (_addedObjects[i] != null)
					DestroyImmediate(_addedObjects[i]);
			}
			_addedObjects.Clear();

			for (int i = 0; i < aObjects.Count; i++) {
				IBlendPaintable paintable = null;
				if (aObjects[i] != null)
					 paintable = aObjects[i].GetComponent<IBlendPaintable>();
				if (paintable != null) {
					paintable.OnPainterUnselected(this);
				}
			}
		}

		public virtual void PaintObjectsBegin(List<GameObject> aObjects, RaycastHit aHit, RaycastHit? aPreviousHit) {
			_undoColors.Clear();
			for (int i = 0; i < aObjects.Count; i++) {
				PaintBegin(aObjects[i], aHit, aPreviousHit);
			}
		}
		public virtual void PaintObjects     (List<GameObject> aObjects, RaycastHit aHit, RaycastHit? aPreviousHit) {
			for (int i = 0; i < aObjects.Count; i++) {
				Paint(aObjects[i], aHit, aPreviousHit);
			}
		}
		public virtual void PaintObjectsEnd  (List<GameObject> aObjects, RaycastHit aHit, RaycastHit? aPreviousHit) {
			for (int i = 0; i < aObjects.Count; i++) {
				PaintEnd(aObjects[i], aHit, aPreviousHit);
			}
			_undoColors.Clear();
		}
		
		public virtual void PaintBegin(GameObject aObject, RaycastHit aHit, RaycastHit? aPreviousHit) {
			MeshFilter paintMesh = aObject.GetComponent<MeshFilter>();
			Undo.RecordObject(paintMesh, "Paint Mesh " + Name);

			ProceduralMeshUtil.EnsureProceduralMesh(paintMesh);

			// setup initial colors so we can send it to the undo system later
			_undoColors.Add(paintMesh, paintMesh.sharedMesh.colors);
		}
		public virtual void Paint     (GameObject aObject, RaycastHit aHit, RaycastHit? aPreviousHit) {
			MeshFilter paintMesh = aObject.GetComponent<MeshFilter>();
			Vector3    objScale  = aObject.transform.lossyScale;
			Vector3    point     = aObject.transform.InverseTransformPoint(aHit.point);
			Vector3    dir       = aObject.transform.InverseTransformDirection(UnityEditor.SceneView.currentDrawingSceneView.camera.transform.forward);
			
			Vector3[] verts  = paintMesh.sharedMesh.vertices;
			Vector3[] norms  = paintMesh.sharedMesh.normals;
			Color  [] colors = paintMesh.sharedMesh.colors;
		
			if (colors.Length != verts.Length) colors = new Color[verts.Length];
		
			for (int v = 0; v < verts.Length; v++) {
				float falloff  = GetPointInfluence(objScale, point, dir, verts[v], norms[v]);
				Color newColor = Color.Lerp(colors[v], _color, _strength * falloff);
				colors[v] = newColor;
			}
			paintMesh.sharedMesh.colors = colors;
		}
		public virtual void PaintEnd  (GameObject aObject, RaycastHit aHit, RaycastHit? aPreviousHit) {
			MeshFilter paintMesh = aObject.GetComponent<MeshFilter>();
			Color[]    newColors = paintMesh.sharedMesh.colors;

			// restore the initial colors before RecordObject, so the entire change is recorded
			if (_undoColors.ContainsKey(paintMesh)) {
				paintMesh.sharedMesh.colors = _undoColors[paintMesh];
				Undo.RecordObject(paintMesh.sharedMesh, "Paint Mesh " + Name);
				paintMesh.sharedMesh.colors = newColors;
			}
		}

		public virtual float GetPointInfluence(Vector3 aObjScale, Vector3 aHitPt, Vector3 aHitDirection, Vector3 aVert, Vector3 aVertNormal) {
			Vector3 diff = aVert - aHitPt;
			diff = new Vector3(
				diff.x * aObjScale.x,
				diff.y * aObjScale.y,
				diff.z * aObjScale.z);

			float sqrMagnitude = diff.sqrMagnitude;
			if (sqrMagnitude > (_size * _size) || (!_backfaces && Vector3.Dot(aHitDirection, aVertNormal) > 0))
				return 0;
			return Mathf.Lerp(1, 1-_falloff, Mathf.Sqrt(sqrMagnitude) / _size);
		}

		public virtual void RenderScenePreview(Camera aSceneCamera, RaycastHit aHit, List<GameObject> aObjects) {
			for (int i = 0; i < aObjects.Count; i++) {
				RenderScenePreview(aSceneCamera, aHit, aObjects[i]);
			}
		}
		public virtual void RenderScenePreview(Camera aSceneCamera, RaycastHit aHit, GameObject aObject) {
			Handles.color = Color.red;
			MeshFilter filter = aObject.GetComponent<MeshFilter>();
			Vector3    camDir = aSceneCamera.transform.forward;
		
			float size = HandleUtility.GetHandleSize(filter.transform.position) * 0.05f;
			Vector3[] verts  = filter.sharedMesh.vertices;
			Vector3[] norms  = filter.sharedMesh.normals;
			Color  [] color  = filter.sharedMesh.colors;
			Vector3   hitPt  = filter.transform.InverseTransformPoint(aHit.point);
			Vector3   dir    = filter.transform.InverseTransformDirection(camDir);
			Vector3   scale  = filter.transform.lossyScale;
			            
			if (color.Length != verts.Length)
				color = new Color[verts.Length];
			            
			for (int v = 0; v < verts.Length; v++) {
				float influence = GetPointInfluence(scale, hitPt, dir, verts[v], norms[v]);
				if (influence > 0) {
					if (color != null && VertexPainterSettings.VertPreviewMode == VertexPainterSettings.VertPreviewModeType.Colors) {
						Handles.color = new Color(color[v].r, color[v].g, color[v].b, 1);
					} else if (VertexPainterSettings.VertPreviewMode == VertexPainterSettings.VertPreviewModeType.Falloff) {
						Handles.color = Color.Lerp(Color.black, Color.red, influence/Size);
					}
					EditorTools.CircleCapBase(0, filter.transform.TransformPoint(verts[v]), Quaternion.identity, size * influence, Event.current.type);
				}
			}
		}

		public virtual int  CheckPriority(GameObject aOfObject) {
			if (aOfObject.GetComponent<MeshFilter>() != null)
				return (int)PaintPriority.General;
			return (int)PaintPriority.None;
		}
		public virtual void DrawToolGUI() {
		}
		public virtual bool GUIInput() {
			bool result = false;
			if (Event.current.type == EventType.KeyDown) {
				Color c = new Color(0, 0, 0, 0);
				if (Event.current.keyCode == KeyCode.Alpha1) {
					c.r = 1;
				} else if (Event.current.keyCode == KeyCode.Alpha2) {
					c.g = 1;
				} else if (Event.current.keyCode == KeyCode.Alpha3) {
					c.b = 1;
				} else if (Event.current.keyCode == KeyCode.Alpha4) {
					c.a = 1;
				}

				if (c.r != 0 || c.g != 0 || c.b != 0 || c.a != 0) {
					Color = c;
					Event.current.Use();
					result = true;
				}
			}
			return result;
		}
	}
}