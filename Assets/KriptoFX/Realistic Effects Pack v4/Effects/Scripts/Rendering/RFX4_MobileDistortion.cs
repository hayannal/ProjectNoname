#define USE_CUSTOM_RENDERER

using UnityEngine;
using UnityEngine.Rendering;

public class RFX4_MobileDistortion : MonoBehaviour
{
    public bool IsActive = true;

    private CommandBuffer buf;
    private Camera cam;
    private bool bufferIsAdded;

    void Awake()
    {
        cam = GetComponent<Camera>();
        CreateBuffer();
    }

    void CreateBuffer()
    {
       // CreateCommandBuffer(Camera.main, CameraEvent.BeforeForwardAlpha, "_GrabTextureMobile");
        var cam = Camera.main;
        buf = new CommandBuffer();
        buf.name = "_GrabOpaqueColor";

        int screenCopyId = Shader.PropertyToID("_ScreenCopyOpaqueColor");
		//var scale = IsSupportedHdr() ? -2 : -1;
#if USE_CUSTOM_RENDERER
		int width = -1;
		int height = -1;
		if (CustomRenderer.instance != null)
		{
			width = Mathf.RoundToInt(Screen.width * CustomRenderer.instance.RenderTextureResolutionFactor);
			height = Mathf.RoundToInt(Screen.height * CustomRenderer.instance.RenderTextureResolutionFactor);
		}
#else
		var scale = -1;
#endif
		var rtFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565)
            ? RenderTextureFormat.RGB565
            : RenderTextureFormat.Default;
#if USE_CUSTOM_RENDERER
		buf.GetTemporaryRT(screenCopyId, width, height, 0, FilterMode.Bilinear, rtFormat);
#else
		buf.GetTemporaryRT(screenCopyId, scale, scale, 0, FilterMode.Bilinear, rtFormat);
#endif
		//buf.get
		buf.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyId);

        buf.SetGlobalTexture("_GrabTexture", screenCopyId);
        buf.SetGlobalTexture("_GrabTextureMobile", screenCopyId);
        //buf.SetGlobalFloat("_GrabTextureMobileScale", (1.0f / scale) * -1);
       // cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, buf);
    }

    void OnEnable()
    {
        AddBuffer();
    }

    void OnDisable()
    {
        RemoveBuffer();
    }

    void AddBuffer()
    {
        cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, buf);
        bufferIsAdded = true;
    }

    void RemoveBuffer()
    {
        cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, buf);
        bufferIsAdded = false;
    }

    void Update()
    {
        if (IsActive)
        {
            if (!bufferIsAdded)
            {
                AddBuffer();
            }
        }
        else
        {
            if(bufferIsAdded) RemoveBuffer();
        }
    }

    bool IsSupportedHdr()
    {
#if UNITY_5_6_OR_NEWER
    return Camera.main.allowHDR;
#else
        return Camera.main.hdr;
#endif
    }
}
