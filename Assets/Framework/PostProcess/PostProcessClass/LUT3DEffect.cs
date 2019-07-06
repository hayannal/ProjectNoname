using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(CustomRenderer))]
public class LUT3DEffect : PostProcessBase
{
	public Texture3D lutTexture;

	const string shaderName = "PostProcess/LUT3D";

	private Material _lutMaterial = null;
	public Material lutMaterial
	{
		get
		{
			if (_lutMaterial == null)
				_lutMaterial = CheckShaderAndCreateMaterial(Shader.Find(shaderName));

			return _lutMaterial;
		}
	}

	//void OnDisable()
	void OnDestroy()
	{
		if (_lutMaterial != null)
		{
			DestroyImmediate(_lutMaterial);
			_lutMaterial = null;
		}
	}

	public override void OnPostProcess(RenderTexture source, RenderTexture destination)
	{
		lutMaterial.SetTexture("_LUTTex", lutTexture);

		Graphics.Blit(source, destination, lutMaterial, 0);
	}
}
