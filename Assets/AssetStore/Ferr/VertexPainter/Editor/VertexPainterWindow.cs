using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Ferr {
	public enum PaintPriority {
		None     = 10,
		Capable  = 2,
		General  = 1,
		Specific = 0
	}
	public class VertexPainterWindow : EditorWindow {
		enum Mode {
			None,
			WaitingToPaint,
			Painting,
			Picking
		}

		private static VertexPainterWindow _instance;
		public  static VertexPainterWindow Instance {
			get { return _instance; }
		}

		[SerializeField] Object           _activePainter;
		[SerializeField] List<GameObject> _paintObjects    = new List<GameObject>();
		[SerializeField] List<GameObject> _selectedObjects = new List<GameObject>();
		[SerializeField] Mode             _paintMode       = Mode.None;
		[SerializeField] Object           _toolPicker;
		[SerializeField] Object           _toolPainter;
		[SerializeField] List<Object>     _paintTools      = new List<Object>();

		RaycastHit?           _previousPaint  = null;
		List<IBlendPaintType> _availableTools = new List<IBlendPaintType>();
		bool                  _setSize        = false;
		
		public IBlendPaintType   ActivePainter { get { return _activePainter as IBlendPaintType; } }
		public List<GameObject>  PaintObjects  { get { return _paintObjects; } }
		public bool              Painting      { get { return _paintMode == Mode.Painting || _paintMode == Mode.Picking; } set { if (Painting != value) { SetMode(value ? Mode.WaitingToPaint : Mode.None); } } }
		
		protected IBlendPaintType ToolPainter { get { return (IBlendPaintType)_toolPainter; } }
		protected IBlendPaintType ToolPicker  { get { if (_toolPicker  == null) { _toolPicker  = ScriptableObject.CreateInstance<VertexColorPicker>(); ((VertexColorPicker)_toolPicker).Initialize(ToolPainter); } return (IBlendPaintType)_toolPicker; } }

		[MenuItem("Window/Ferr Vertex Painter", false, 2010)]
		[MenuItem("Tools/Ferr/Vertex Painter",  false, 10)]
		private static void CreateWindow() { 
			bool exists = _instance != null;

			VertexPainterWindow window = GetWindow<VertexPainterWindow>();
			window.titleContent = new GUIContent("", VertexPainterIcons.Instance.iconSmall);
			window.SetMode(Mode.WaitingToPaint);
			
			if (!exists) {
				window._setSize = true;
			}
		}
		
		private void OnEnable() {
			minSize = new Vector2(64, 200);

			LoadPaintTools();

			SceneView.onSceneGUIDelegate += OnScene;
			Selection.selectionChanged   += OnSelectionChanged; 
			_instance = this;

			OnSelectionChanged();
		}
		private void OnDisable() {
			SceneView.onSceneGUIDelegate -= OnScene;
			Selection.selectionChanged   -= OnSelectionChanged;
			_instance = null;

			SetMode(Mode.None);
		}

		private void OnGUI() {
			CheckSize();
			CheckWindowCommands();

			EditorGUILayout.BeginVertical();

			RenderIcon();
			RenderToolSelect();

			VertexPainterGUI.RenderGUI(ActivePainter);

			EditorGUILayout.EndVertical();
		}
		private void OnScene(SceneView aView) {
			CheckWindowCommands();

			if (!Painting || ActivePainter == null)
				return;

			if (!VertexPainterScene.ArcRotating) {
				Cursor.SetCursor(ActivePainter.Cursor, ActivePainter.CursorHotspot, CursorMode.Auto);
				EditorGUIUtility.AddCursorRect(new Rect(0, 0, aView.position.width, aView.position.height), MouseCursor.CustomCursor);
			}

			VertexPainterScene.Input    (aView, ActivePainter);
			VertexPainterScene.RenderGUI(aView, ActivePainter);

			// if an Undo/Redo occurred, Unity doesn't properly refresh the visual mesh, so we force it to like this!
			if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed") {
				for (int i = 0; i < _selectedObjects.Count; i++) {
					MeshFilter f = _selectedObjects[i].GetComponent<MeshFilter>();
					if (f != null && f.sharedMesh != null) {
						f.sharedMesh.vertices = f.sharedMesh.vertices;
					}
				}
			}
		}

		private void CheckSize() {
			if (_setSize) {
				_setSize = false;
				position = new Rect(position.x, position.y, minSize.x, minSize.y*2);
			}
		}
		private void CheckWindowCommands() {
			if (Event.current.type == EventType.KeyDown) {
				if (Event.current.keyCode == KeyCode.C && _paintMode == Mode.Painting && _paintObjects.Count > 0) {
					SetMode(Mode.Picking);
					Event.current.Use();
					
				}
				if (Event.current.keyCode == KeyCode.P && (Painting || _selectedObjects.Count > 0)) {
					Painting = !Painting;
					Repaint();
					Event.current.Use();
				}
			} else if (Event.current.type == EventType.KeyUp) {
				if (Event.current.keyCode == KeyCode.C && _paintMode == Mode.Picking) {
					SetMode(Mode.WaitingToPaint);
					Event.current.Use();
				}
			}

			if (ActivePainter != null && Painting && ActivePainter.GUIInput()) {
				Repaint();
			}
		}

		private static void RenderIcon() {
			Rect r;
			EditorGUILayout.BeginHorizontal();
			GUILayout      .FlexibleSpace();
			EditorGUILayout.LabelField("", GUILayout.Height(50));
			GUILayout      .FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			r = GUILayoutUtility.GetLastRect();
			r.yMin-=1;
			r.xMax += 4;
			EditorGUI.DrawRect(r, VertexPainterIcons.Instance.lineColor);
			r.xMin += r.width/2 - 24;
			r.yMin += 0;
			EditorGUI.LabelField(r, new GUIContent(VertexPainterIcons.Instance.iconLarge));
		}
		void RenderToolSelect() {
			VertexEditorUtil.DrawUILine(VertexPainterGUI.cLineColor, 2, 0);

			// assemble a list of names
			string[] options = new string[_availableTools.Count];
			int      index   = -1;
			for (int i = 0; i < _availableTools.Count; i++) {
				if (_availableTools[i].GetType() == ActivePainter.GetType()) {
					index = i;
					break;
				}
			}
			for (int i = 0; i < _availableTools.Count; i++)
				options[i] = _availableTools[i].Name;

			// draw a selection dropdown with the tool list
			if (index != -1) {
				int newIndex = EditorGUILayout.Popup(index, options, VertexPainterIcons.Instance.dropdown);
				if (newIndex != index) {
					SetActivePainter(_availableTools[newIndex]);
				}
			} else {
				EditorGUILayout.LabelField(ActivePainter == null ? "None" : ActivePainter.Name, VertexPainterIcons.Instance.centeredLabel);
			}

			VertexEditorUtil.DrawUILine(VertexPainterGUI.cLineColor, 2, 0);
			EditorGUILayout.Space();

			GUIContent label = new GUIContent(Painting ? "Disable" : "Enable", "'p' Toggle painting mode on or off");
			bool paint = GUILayout.Toggle(Painting, label, VertexPainterIcons.Instance.button, GUILayout.Height(VertexPainterGUI.cButtonHeight));
			if (paint != Painting)
				Painting = paint;
		}

		void OnSelectionChanged() {
			// filter out any non-paintable prefab types
			_selectedObjects.Clear();
			for (int i = 0; i < Selection.gameObjects.Length; i++) {
				PrefabType t = PrefabUtility.GetPrefabType(Selection.gameObjects[i]);
				if (t != PrefabType.ModelPrefab && t != PrefabType.Prefab)
					_selectedObjects.Add(Selection.gameObjects[i]);
			}
			// figure out what tools are available for this selection, and what's the best match
			_availableTools = GetAvailableTools(_selectedObjects);
			IBlendPaintType bestPainter = GetBestTool(_selectedObjects);
			
			SetActivePainter(bestPainter);

			// check if we're waiting for objects, and if we have some objects now!
			if (_paintMode == Mode.WaitingToPaint && _paintObjects.Count > 0)
				SetMode(Mode.Painting);
		}

		public void DoFill() {
			for (int i = 0; i < _paintObjects.Count; i++) {
				FillColor(_paintObjects[i]);
			}
			SceneView.RepaintAll();
		}
		public void BeginPaint(RaycastHit aHit) {
			ActivePainter.PaintObjectsBegin(_paintObjects, aHit, null);
		}
		public void DoPaint(RaycastHit aHit) {
			// TODO: add distance based updates, and/or an airbrush mode
			ActivePainter.PaintObjects(_paintObjects, aHit, _previousPaint);
			SceneView.RepaintAll();
			_previousPaint = aHit;
		}
		public void EndPaint(RaycastHit aHit) {
			ActivePainter.PaintObjectsEnd(_paintObjects, aHit, _previousPaint);
			_previousPaint = null;
		}

		public void FillColor(GameObject aObject) {
			MeshFilter filter = aObject.GetComponent<MeshFilter>();
			Color32[]  colors = new Color32[filter.sharedMesh.vertexCount];
			Color32    col    = ActivePainter.Color;

			Undo.RecordObject(filter, "Fill Mesh Color");
			ProceduralMeshUtil.EnsureProceduralMesh(filter);

			for (int v = 0; v < colors.Length; v++) {
				colors[v] = col;
			}
			filter.sharedMesh.colors32 = colors;
		}
	
		void SetMode(Mode aMode) {
			if (aMode == _paintMode)
				return;

			if (_paintMode == Mode.Painting)
				OnPaintModeEnd();
			else if (_paintMode == Mode.Picking)
				OnPickerModeEnd();
			else if (_paintMode == Mode.None)
				OnNoModeEnd();
			else if (_paintMode == Mode.WaitingToPaint)
				OnWaitModeEnd();

			_paintMode = aMode;

			if (_paintMode == Mode.Painting)
				OnPaintModeBegin();
			else if (_paintMode == Mode.Picking)
				OnPickerModeBegin();
			else if (_paintMode == Mode.None)
				OnNoModeBegin();
			else if (_paintMode == Mode.WaitingToPaint)
				OnWaitModeBegin();
		}

		void OnPickerModeBegin() {
			((VertexColorPicker)ToolPicker).Initialize(ToolPainter);
			SetActivePainter(ToolPicker, true);
		}
		void OnPickerModeEnd() {
			SetActivePainter(ToolPainter);
			Repaint();
		}
		void OnPaintModeBegin() {
			PaintToolBegin();

			if (_paintObjects.Count == 0)
				SetMode(Mode.WaitingToPaint);
		}
		void OnPaintModeEnd() {
			PaintToolEnd();
		}
		void OnWaitModeBegin() {
			Tools.hidden = false;
			if (CanPaintObjects(ActivePainter, _selectedObjects).Count > 0)
				SetMode(Mode.Painting);
		}
		void OnWaitModeEnd() {
			Tools.hidden = true;
		}
		void OnNoModeBegin() {
			Tools.hidden = false;
		}
		void OnNoModeEnd() {
			Tools.hidden = true;
		}

		void PaintToolBegin() {
			_paintObjects = new List<GameObject>(_selectedObjects);

			// remove any items from the selection list that don't match this painter
			if (ActivePainter == null) {
				_paintObjects.Clear();
			} else {
				_paintObjects = CanPaintObjects(ActivePainter, _paintObjects);
				ActivePainter.OnSelect(_paintObjects);
			}
		}
		void PaintToolEnd() {
			if (ActivePainter != null)
				ActivePainter.OnUnselect(_paintObjects);
			_paintObjects.Clear();
		}

		void LoadPaintTools() {
			Assembly assembly = Assembly.GetExecutingAssembly();
			Type[]   types    = assembly.GetTypes();
			Type     painter  = typeof(IBlendPaintType);

			for (int i=_paintTools.Count-1; i>=0; i-=1) {
				if (_paintTools[i] == null)
					_paintTools.RemoveAt(i);
			}
			
			for (int i = 0; i < types.Length; i++) {
				if (painter.IsAssignableFrom(types[i])) {
					bool add = true; 
					for (int t = 0; t < _paintTools.Count; ++t) {
						if (_paintTools[t].GetType() == types[i]) {
							add = false;
							break;
						}
					}
					if (add) {
						_paintTools.Add(CreateInstance( types[i] ));
					}
				}
			}
		}

		void SetActivePainter(IBlendPaintType aPainter, bool aTemporary = false) {
			PaintToolEnd();
			
			_activePainter = (Object)aPainter;
			if (!aTemporary)
				_toolPainter = (Object)aPainter;

			PaintToolBegin();
			Repaint();
		}
		List<GameObject> CanPaintObjects(IBlendPaintType aPainter, List<GameObject> aObjects) {
			List<GameObject> result = new List<GameObject>();
			if (aPainter == null)
				return result;

			result.AddRange(aObjects);

			int priority = (int)PaintPriority.None;
			for (int i = 0; i < result.Count; i++) {
				int curr = aPainter.CheckPriority(result[i]);
				if (curr < priority)
					priority = curr;
			}
			for (int i = result.Count-1; i>=0; i-=1) {
				int curr = ActivePainter.CheckPriority(result[i]);
				if (curr > priority)
					result.RemoveAt(i);
			}

			return result;
		}
		IBlendPaintType GetBestTool(List<GameObject> aObjects) {
			IBlendPaintType result = null;
			int             best   = (int)PaintPriority.None;
			for (int t = 0; t < _paintTools.Count; t++) {
				for (int i = 0; i < aObjects.Count; i++) {
					int curr = ((IBlendPaintType)_paintTools[t]).CheckPriority(aObjects[i]);
					if (curr < best) {
						best = curr;
						result = (IBlendPaintType)_paintTools[t];
					}
				}
			}
			return result;
		}
		List<IBlendPaintType> GetAvailableTools(List<GameObject> aObjects) {
			List<IBlendPaintType> result = new List<IBlendPaintType>();
			for (int t = 0; t < _paintTools.Count; t++) {
				for (int i = 0; i < aObjects.Count; i++) {
					if (((IBlendPaintType)_paintTools[t]).CheckPriority(aObjects[i]) != (int)PaintPriority.None) {
						result.Add((IBlendPaintType)_paintTools[t]);
						break;
					}
				}
			}
			return result;
		}
	}
}