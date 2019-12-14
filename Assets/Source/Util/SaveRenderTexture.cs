using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveRenderTexture : MonoBehaviour
{
	public RenderTexture renderTexture;

#if UNITY_EDITOR
	// Update is called once per frame
	void Update()
    {
		if (Input.GetKeyDown(KeyCode.C))
		{
			if (renderTexture == null)
				return;

			RenderTexture currentRT = RenderTexture.active;
			RenderTexture.active = renderTexture;
			//m_bloomComponent.UpdateBloom(tempRenderTexture);
			
			Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
			tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);

			byte[] bytes;
			bytes = tex.EncodeToPNG();

			string path = "screenshot.png";
			System.IO.File.WriteAllBytes(path, bytes);
			UnityEditor.AssetDatabase.ImportAsset(path);
			Debug.Log("Saved to " + path);

			RenderTexture.active = currentRT;
		}
	}
#endif
}
