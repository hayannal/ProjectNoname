using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(CustomRenderer))]
public class CameraBlurEffect : PostProcessBase
{
	[Range(0.0f, 1.0f)]
	public float blendFactor = 0.5f;

	public AnimationCurve blurStrengthCurve;

	const string shaderName = "PostProcess/CameraBlur";

	public RenderTexture _accumBlurTarget;

	private Material _cameraBlurMaterial = null;
	public Material cameraBlurMaterial
	{
		get
		{
			if (_cameraBlurMaterial == null)
				_cameraBlurMaterial = CheckShaderAndCreateMaterial(Shader.Find(shaderName));

			return _cameraBlurMaterial;
		}
	}

	//void OnDisable()
	void OnDestroy()
	{
		if (_cameraBlurMaterial != null)
		{
			DestroyImmediate(_cameraBlurMaterial);
			_cameraBlurMaterial = null;
		}
		if (_accumBlurTarget != null)
		{
			_accumBlurTarget.Release();
			_accumBlurTarget = null;
		}
	}

	bool _firstBlit = false;
	void OnEnable()
	{
		_firstBlit = true;
	}

	void OnDisable()
	{
	}

	public override bool UseSecondRT () { return false; }

	public override void OnPostProcess(RenderTexture source, RenderTexture destination)
	{
		InitializeRenderTexture(ref _accumBlurTarget, source);

		if (_firstBlit)
		{
			Graphics.Blit(source, _accumBlurTarget);
			//Graphics.Blit(source, destination);
			_firstBlit = false;
			return;
		}

		//Graphics.Blit(source, destination);

		cameraBlurMaterial.SetFloat("_BlendFactor", blendFactor);
		Graphics.Blit(_accumBlurTarget, source, cameraBlurMaterial, 0);

		Graphics.Blit(source, _accumBlurTarget);
	}
}
