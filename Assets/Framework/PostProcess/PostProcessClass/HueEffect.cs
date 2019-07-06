using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(CustomRenderer))]
public class HueEffect : PostProcessBase
{
	[Range(0.0f, 360.0f)]
	public float hue = 0.0f;
	
	const string shaderName = "PostProcess/Hue";

	private Material _hueMaterial = null;
	public Material hueMaterial
	{
		get
		{
			if (_hueMaterial == null)
				_hueMaterial = CheckShaderAndCreateMaterial(Shader.Find(shaderName));

			return _hueMaterial;
		}
	}

	//void OnDisable()
	void OnDestroy()
	{
		if (_hueMaterial != null)
		{
			DestroyImmediate(_hueMaterial);
			_hueMaterial = null;
		}
	}

	public override void OnPostProcess(RenderTexture source, RenderTexture destination)
	{
		hueMaterial.SetFloat("_Hue", hue);

		Graphics.Blit(source, destination, hueMaterial, 0);
	}
}
