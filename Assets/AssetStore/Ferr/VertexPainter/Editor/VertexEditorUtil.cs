using UnityEditor;
using UnityEngine;

namespace Ferr { 
	public class VertexEditorUtil {
		public static void DrawUILine(Color aColor, int aThickness = 2, int aPadding = 10) {
			Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(aPadding+aThickness));
			r.height = aThickness;
			r.y+=aPadding/2;
			r.x-=2;
			r.width +=6;
			EditorGUI.DrawRect(r, aColor);
		}
		public static void GradientLine(Vector3 aP1, Vector3 aP2, Color aC1, Color aC2) {
			for (int i = 1; i < 10; ++i) {
				float percent     = i    / 9f;
				float prevPercent = (i-1) / 9f;
				Handles.color = Color.Lerp(aC1, aC2, prevPercent);
				Handles.DrawLine(Vector3.Lerp(aP1, aP2, prevPercent), Vector3.Lerp(aP1, aP2, percent));
			}
		}
		public static void DashCircle(Vector3 aPt, Vector3 aNormal, float aRadius, float aDashLength, float aGapLength, float aDashScale) {
			float circumfrence = Mathf.PI*2*aRadius;
			float stepLength   = aDashLength+aGapLength;
			
			int stepCount = Mathf.Max(12, (int)(circumfrence / stepLength / (aDashScale*0.75f)));
			stepLength = (Mathf.PI*2) / stepCount;

			float dashStep = (aDashLength/(aDashLength + aGapLength)) * stepLength;
			float gapStep  = (aGapLength /(aDashLength + aGapLength)) * stepLength;

			float angle = 0;
			Quaternion rot = Quaternion.LookRotation(aNormal);
			for (int i = 0; i <= stepCount; ++i) {
				Vector3 v1 = new Vector3(Mathf.Cos(angle) * aRadius, Mathf.Sin(angle) * aRadius, 0);
				angle += dashStep;
				Vector3 v2 = new Vector3(Mathf.Cos(angle) * aRadius, Mathf.Sin(angle) * aRadius, 0);
				angle += gapStep;
				Handles.DrawLine(aPt + rot * v1, aPt + rot * v2);
			}
		}
	}
}