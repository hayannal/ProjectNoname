using UnityEngine;
using UnityEditor;

namespace Ferr {
	public class BlendShaderStandardGUI : BlendShaderGUI {
		MaterialProperty _gloss;
		MaterialProperty _metal;
		MaterialProperty _specColor;
		bool _metallic = false;

		public override void OnGUI(MaterialEditor aMaterialEditor, MaterialProperty[] aProps) {
			base.OnGUI(aMaterialEditor, aProps);
		}

		protected override void FindProperties(MaterialProperty[] aProps) {
			_gloss     = FindProperty("_Smoothness", aProps);
			_metal     = FindProperty("_Metallic",   aProps, false);
			_specColor = FindProperty("_SpecColor",  aProps, false);
		
			_metallic  = _metal != null;

			base.FindProperties(aProps);
		}
		protected override void ShaderPropertiesGUI(MaterialEditor aEditor, Material aMat) {
			EditorGUI.BeginChangeCheck();
			KeywordGUI(aEditor, aMat);
		
			if (_metallic) aEditor.ShaderProperty(_metal,     _metal.displayName    );
			else           aEditor.ShaderProperty(_specColor, _specColor.displayName);
			aEditor.ShaderProperty(_gloss, _gloss.displayName);

			ChannelGUI(aEditor);
		}
	}
}