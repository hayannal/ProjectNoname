using UnityEngine;
using UnityEditor;

namespace Ferr { 
	static class VertexPainterSettings {
		internal enum VertPreviewModeType {
			Colors,
			Falloff,
			None
		}
		internal enum StrengthPreviewModeType {
			Dashes,
			Spike,
			None
		}

		const string cPrefix = "FerrVertPaint_";

		private static bool _prefsLoaded = false;
		
		private static VertPreviewModeType     _vertPreviewMode;
		private static StrengthPreviewModeType _strengthPreviewMode;
		private static Color                   _brushColor;
		private static bool                    _showFalloffRing;
		
		public static VertPreviewModeType     VertPreviewMode     { get { LoadPrefs(); return _vertPreviewMode; } }
		public static StrengthPreviewModeType StrengthPreviewMode { get { LoadPrefs(); return _strengthPreviewMode; } }
		public static Color                   BrushColor          { get { LoadPrefs(); return _brushColor; } }
		public static bool                    ShowFalloffRing     { get { LoadPrefs(); return _showFalloffRing; } }
		
		[PreferenceItem("Ferr VertPaint")]
		static void PreferencesGUI() 
		{
			LoadPrefs();
			
			_vertPreviewMode     = (VertPreviewModeType    )EditorGUILayout.EnumPopup("Vertex Preview Mode",   _vertPreviewMode    );
			_strengthPreviewMode = (StrengthPreviewModeType)EditorGUILayout.EnumPopup("Strength Preview Mode", _strengthPreviewMode);
			_brushColor          = EditorGUILayout.ColorField("Brush Color",       _brushColor     );
			_showFalloffRing     = EditorGUILayout.Toggle    ("Show Falloff Ring", _showFalloffRing);
			
			if (GUI.changed) {
				SavePrefs();
			}
		}
		
		static void LoadPrefs() {
			if (_prefsLoaded) return;
			_prefsLoaded   = true;
			
			_vertPreviewMode     = (VertPreviewModeType    )EditorPrefs.GetInt(cPrefix+"vertPreviewMode",     (int)VertPreviewModeType.Colors    );
			_strengthPreviewMode = (StrengthPreviewModeType)EditorPrefs.GetInt(cPrefix+"strengthPreviewMode", (int)StrengthPreviewModeType.Dashes);
			_brushColor.r    = EditorPrefs.GetFloat(cPrefix+"brushColor_r", 1);
			_brushColor.g    = EditorPrefs.GetFloat(cPrefix+"brushColor_g", 1);
			_brushColor.b    = EditorPrefs.GetFloat(cPrefix+"brushColor_b", 1);
			_brushColor.a    = EditorPrefs.GetFloat(cPrefix+"brushColor_a", 1);
			_showFalloffRing = EditorPrefs.GetBool (cPrefix+"showFalloffRing", true);
		}
		static void SavePrefs() {
			if (!_prefsLoaded) return;
			
			EditorPrefs.SetInt (cPrefix+"vertPreviewMode",     (int)_vertPreviewMode    );
			EditorPrefs.SetInt (cPrefix+"strengthPreviewMode", (int)_strengthPreviewMode);
			
			EditorPrefs.SetFloat(cPrefix+"brushColor_r", _brushColor.r);
			EditorPrefs.SetFloat(cPrefix+"brushColor_g", _brushColor.g);
			EditorPrefs.SetFloat(cPrefix+"brushColor_b", _brushColor.b);
			EditorPrefs.SetFloat(cPrefix+"brushColor_a", _brushColor.a);
			
			EditorPrefs.SetBool(cPrefix+"showFalloffRing", _showFalloffRing);
		}
	}
}