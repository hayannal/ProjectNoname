using UnityEngine;
using UnityEditor;

namespace Ferr {
	public class VertexPainterHelp : EditorWindow {
		void OnGUI() {
			EditorGUI.TextField(new Rect(0, 0, position.width, position.height),
@"-Ferr Vertex Painter-
Email support@simbryocorp.com for additional questions!
And check out the online docs:
http://ferrlib.com/tool/vertexpainter
______________________

Select multiple meshes when
in paint mode to paint 
multiple objects simultaneously!

Shortcuts:
p : toggle paint mode

While in paint mode:
c : quick color select

[ & ] : brush size
; & ' : brush strength
< & > : brush hardness
1 - 4 : quick channel select

Ctrl+Scroll : brush size
Shift+Scroll : brush strength
Ctrl+Shift+Scroll : brush hardness
");
		}
	}
}