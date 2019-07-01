using UnityEditor;
using UnityEngine;

namespace Ferr {
	public class BlendShaderSpecularGUI : BlendShaderGUI {
		MaterialProperty _specColor;
		MaterialProperty _shininess;

		protected override void FindProperties(MaterialProperty[] aProps) {
			_shininess = FindProperty("_Shininess", aProps);
			_specColor = FindProperty("_SpecColor", aProps);

			base.FindProperties(aProps);
		}
		protected override void ShaderPropertiesGUI(MaterialEditor aEditor, Material aMat) {
			EditorGUI.BeginChangeCheck();
			KeywordGUI(aEditor, aMat);

			aEditor.ShaderProperty(_specColor, "Specular Color");
			aEditor.ShaderProperty(_shininess, "Shininess");

			ChannelGUI(aEditor);
		}
	}
}