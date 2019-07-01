using UnityEngine;
using UnityEditor;

namespace Ferr {
	public class VertexPainterIcons {
		private static VertexPainterIcons _instance = null;
		public  static VertexPainterIcons Instance {
			get {
				if (_instance == null) _instance = new VertexPainterIcons();
				return _instance;
			}
		}
		private VertexPainterIcons() {
			button = new GUIStyle(EditorStyles.label);
			button.alignment = TextAnchor.MiddleCenter;
			button.onNormal.background = EditorTools.GetGizmo("VertexPainter/Gizmos/Enabled.png");
			button.normal  .background = EditorTools.GetGizmo("VertexPainter/Gizmos/Normal.png");

			centeredLabel = new GUIStyle(EditorStyles.label);
			centeredLabel.alignment = TextAnchor.MiddleCenter;
			centeredLabel.fontStyle = FontStyle.Bold;

			dropdown = new GUIStyle(EditorStyles.toolbarPopup);
			dropdown.alignment = TextAnchor.MiddleCenter;
			dropdown.fontStyle = FontStyle.Bold;
			dropdown.normal.background = EditorTools.GetGizmo("VertexPainter/Gizmos/Dropdown.png"); ;
		}

		public Texture2D iconSmall   = EditorTools.GetGizmo("VertexPainter/Gizmos/Icon16.png");
		public Texture2D iconLarge   = EditorTools.GetGizmo("VertexPainter/Gizmos/Icon48.png");
		public Texture2D fill        = EditorTools.GetGizmo("VertexPainter/Gizmos/Fill.png");
		public Texture2D backfaceOn  = EditorTools.GetGizmo("VertexPainter/Gizmos/BackfaceOn.png");
		public Texture2D backfaceOff = EditorTools.GetGizmo("VertexPainter/Gizmos/BackfaceOff.png");
		public Texture2D settings    = EditorTools.GetGizmo("VertexPainter/Gizmos/Settings.png");
		public Texture2D cursor      = EditorTools.GetGizmo("VertexPainter/Gizmos/Cursor.png");
		public Texture2D eyedropper  = EditorTools.GetGizmo("VertexPainter/Gizmos/EyedropCursor.png");

		public GUIStyle button;
		public GUIStyle centeredLabel;
		public GUIStyle dropdown;

		public Color borderColor = new Color(0.13f, 0.13f, 0.13f, 1);
		public Color lineColor   = new Color(0, 0, 0, 0.3f);//new Color(0.35f, 0.35f, 0.35f, 1);
		public Color outColor    = new Color(0.16f, 0.16f, 0.16f, 1);
		public Color backColor   = new Color(0.22f, 0.22f, 0.22f, 1f);
	
		private Texture2D _brushTex;
		private Color[]   _brush;
		private float     _prevStr     = -1;
		private float     _prevFalloff = -1;
		private float     _prevSize    = -1;

		private Texture2D[] hsvTex    = new Texture2D[0];
		private Color[][]   hsvColors = new Color[0][];

		public Texture2D MakeBrush(int aTexSize, float aStrength, float aFalloff, float aSize) {
			if (_brushTex == null || _brushTex.width != aTexSize) {
				_brushTex = new Texture2D(aTexSize, aTexSize);
			}
			if (_brush == null || _brush.Length != aTexSize*aTexSize) {
				_brush = new Color[aTexSize*aTexSize];
				for (int i = 0; i < _brush.Length; ++i) {
					_brush[i] = Color.white;
				}
				_brushTex.SetPixels(_brush);
				_brushTex.Apply();
			}
			if (_prevStr == aStrength && _prevFalloff == aFalloff && _prevSize == aSize) return _brushTex;

			Vector2 middle = new Vector2(0.5f, 0.5f);
			for (int y = 0; y < aTexSize; y++) {
				float py = (float)y / (aTexSize-1);
				for (int x = 0; x < aTexSize; x++) {
					float   px = (float)x / (aTexSize-1);
					Vector2 v  = new Vector2(px, py);
					float   d  = Vector2.Distance(middle, v) * 2.1f;
					float   f  = Mathf.Lerp(1, 1-aFalloff, d/aSize);
					float   a  = Mathf.Clamp01( 10*(d-aSize) );
				
					_brush[x+y*aTexSize].a = (1-a) * aStrength * f;
				}
			}
		
			_brushTex.SetPixels(_brush);
			_brushTex.Apply();
		
			_prevStr     = aStrength;
			_prevSize    = aSize;
			_prevFalloff = aFalloff;
		
			return _brushTex;
		}
		
		public Texture2D MakeHSV(int id, int aWidth, int aHeight, float h1, float h2, float s1, float s2, float v1, float v2) {
			if (hsvTex == null || hsvTex.Length < id+1) {

				System.Array.Resize(ref hsvTex,    id+1);
				System.Array.Resize(ref hsvColors, id+1);
			}

			if (hsvTex[id] == null || hsvTex[id].width != aWidth || hsvTex[id].height != aHeight) {
				hsvTex[id] = new Texture2D(aWidth, aHeight);
				hsvColors[id] = new Color[aWidth*aHeight];
			}
		
			for (int x = 0; x < aWidth; x++) {
				float px = (float)x / (aWidth-1);
				Color col = Color.HSVToRGB(
						Mathf.Lerp(h1, h2, px),
						Mathf.Lerp(s1, s2, px),
						Mathf.Lerp(v1, v2, px));
				for (int y = 0; y < aHeight; y++) {
					hsvColors[id][x+y*aWidth] = col;
				}
			}

			hsvTex[id].SetPixels(hsvColors[id]);
			hsvTex[id].Apply();

			return hsvTex[id];
		}

		public static Texture2D GetPercentage(Texture2D[] aFrom, float aPercent) {
			return aFrom[Mathf.RoundToInt((aFrom.Length-1) * Mathf.Clamp01(aPercent))];
		}
	}
}