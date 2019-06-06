using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(CustomRenderer))]
public class RadialBlurEffect : PostProcessBase
{
	[Range(0.0f, 5.0f)]
	public float blurStrength = 0.5f;

	[Range(0.0f, 2.0f)]
	public float blurWidth = 0.5f;

	public AnimationCurve blurStrengthCurve;

	const string shaderName = "PostProcess/RadialBlur";

	private Material _radialBlurMaterial = null;
	public Material radialBlurMaterial
	{
		get
		{
			if (_radialBlurMaterial == null)
				_radialBlurMaterial = CheckShaderAndCreateMaterial(Shader.Find(shaderName));

			return _radialBlurMaterial;
		}
	}

	//void OnDisable()
	void OnDestroy()
	{
		if (_radialBlurMaterial != null)
		{
			DestroyImmediate(_radialBlurMaterial);
			_radialBlurMaterial = null;
		}
	}

	public override void OnPostProcess(RenderTexture source, RenderTexture destination)
	{
		radialBlurMaterial.SetFloat("_BlurStrength", blurStrength);
		radialBlurMaterial.SetFloat("_BlurWidth", blurWidth);

		Graphics.Blit(source, destination, radialBlurMaterial, 0);
	}
}
