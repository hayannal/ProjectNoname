using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(CustomRenderer))]
public class RGBSplitEffect : PostProcessBase
{
	[Range(0.0f, 10.0f)]
	public float splitPower = 3.0f;
	public Vector4 splitValue = new Vector4(1.0f, 1.0f, 1.0f, 0.0f);

	public AnimationCurve splitPowerCurve;

	const string shaderName = "PostProcess/RGBSplit";

	private Material _rgbSplitMaterial = null;
	public Material rgbSplitMaterial
	{
		get
		{
			if (_rgbSplitMaterial == null)
				_rgbSplitMaterial = CheckShaderAndCreateMaterial(Shader.Find(shaderName));

			return _rgbSplitMaterial;
		}
	}

	//void OnDisable()
	void OnDestroy()
	{
		if (_rgbSplitMaterial != null)
		{
			DestroyImmediate(_rgbSplitMaterial);
			_rgbSplitMaterial = null;
		}
	}

	public override void OnPostProcess(RenderTexture source, RenderTexture destination)
	{
		rgbSplitMaterial.SetFloat("_SplitPower", splitPower);
		rgbSplitMaterial.SetVector("_SplitValue", splitValue);

		Graphics.Blit(source, destination, rgbSplitMaterial, 1);
	}
}
