using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessBase : MonoBehaviour {

	protected Material CheckShaderAndCreateMaterial(Shader s)
	{
		if (s == null || !s.isSupported)
			return null;

		var material = new Material(s);
		material.hideFlags = HideFlags.DontSave;
		return material;
	}

	protected void InitializeRenderTexture(ref RenderTexture current, RenderTexture target)
	{
		if (current == null)
			current = new RenderTexture(target.width, target.height, 0, target.format);
		else
		{
			if (current.width != target.width || current.height != target.height)
			{
				current.Release();
				current = new RenderTexture(target.width, target.height, 0, target.format);
			}
		}
	}

	void Start() {}

	public virtual bool UseSecondRT() { return true; }

	public virtual void OnPostProcess(RenderTexture source, RenderTexture destination)
	{
	}
}
