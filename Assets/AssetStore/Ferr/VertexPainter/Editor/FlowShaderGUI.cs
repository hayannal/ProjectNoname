using UnityEditor;
using UnityEngine;

namespace Ferr {
	public class FlowShaderGUI : ShaderGUI {
		MaterialProperty _crossTime;
		MaterialProperty _speed;
		MaterialProperty _metal;
		MaterialProperty _smoothness;

		MaterialProperty _diffuse;
		MaterialProperty _bump;
		MaterialProperty _spec;
		MaterialProperty _emit;

#if UNITY_5_6_OR_NEWER
		MaterialProperty     _emitColor;
		ColorPickerHDRConfig _pickerConfig = new ColorPickerHDRConfig(0f, 99f, 1 / 99f, 3f);
#endif

		protected virtual void FindProperties(MaterialProperty[] aProps) {
			_crossTime  = FindProperty("_CrossTime", aProps);
			_speed      = FindProperty("_Speed", aProps);
			_metal      = FindProperty("_Metallic", aProps);
			_smoothness = FindProperty("_Glossiness", aProps);

			_diffuse   = FindProperty("_MainTex", aProps);
			_bump      = FindProperty("_BumpMap", aProps);
			_spec      = FindProperty("_MetallicGlossMap", aProps);
			_emit      = FindProperty("_EmissionMap", aProps);
#if UNITY_5_6_OR_NEWER
			_emitColor = FindProperty("_EmissionColor", aProps);
#endif
		}

		public override void OnGUI(MaterialEditor aMaterialEditor, MaterialProperty[] aProps) {
			Material mat = aMaterialEditor.target as Material;
			FindProperties(aProps);
			ShaderPropertiesGUI(aMaterialEditor, mat);
		}
		protected void ShaderPropertiesGUI(MaterialEditor aEditor, Material aMat) {
			aEditor.ShaderProperty(_crossTime,  _crossTime.displayName);
			aEditor.ShaderProperty(_speed,      _speed.displayName);
			aEditor.ShaderProperty(_metal,      _metal.displayName);
			aEditor.ShaderProperty(_smoothness, _smoothness.displayName);

			aEditor.TexturePropertySingleLine(new GUIContent(_diffuse.displayName), _diffuse);
			aEditor.TexturePropertySingleLine(new GUIContent(_bump.displayName), _bump);
			aEditor.TexturePropertySingleLine(new GUIContent(_spec.displayName), _spec);

#if UNITY_5_6_OR_NEWER
			if (aEditor.EmissionEnabledProperty()) {
				bool hadEmissionTexture = _emit.textureValue != null;

				aEditor.TexturePropertyWithHDRColor(new GUIContent("Color"), _emit, _emitColor, _pickerConfig, false);

				float brightness = _emitColor.colorValue.maxColorComponent;
				if (_emit.textureValue != null && !hadEmissionTexture && brightness <= 0f)
					_emitColor.colorValue = Color.white;

				aEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
			}
			MaterialEditor.FixupEmissiveFlag(aMat);
			bool shouldEmissionBeEnabled = (aMat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
			if (shouldEmissionBeEnabled) aMat.EnableKeyword("_EMISSION");
			else aMat.DisableKeyword("_EMISSION");
#endif

			EditorGUI.BeginChangeCheck();
			aEditor.TextureScaleOffsetProperty(_diffuse);
			if (EditorGUI.EndChangeCheck())
				_emit.textureScaleAndOffset = _diffuse.textureScaleAndOffset;
		}
		public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader) {
			if (material.HasProperty("_Emission")) {
				material.SetColor("_EmissionColor", material.GetColor("_Emission"));
			}
			base.AssignNewShaderToMaterial(material, oldShader, newShader);
		}
	}
}