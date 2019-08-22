#define USE_CUSTOM_RENDERER

using UnityEngine;

public class RFX4_MobileBloom : MonoBehaviour
{
    [Range(0.2f, 1)]
    [Tooltip("Camera render texture resolution")]
    public float RenderTextureResolutoinFactor = 0.5f;

    [Range(0.05f, 2)]
    [Tooltip("Blend factor of the result image.")]
    public float bloomIntensity = 0.5f;

#if USE_CUSTOM_RENDERER
	[Range(0.1f, 3)]
	[Tooltip("Filters out pixels under this level of brightness.")]
	public float bloomThreshold = 1.3f;

	[Tooltip("Lens Dirt Texture. The texture that controls per-channel light scattering amount.")]
	public Texture2D DirtTexture;
	public float DirtIntensity = 3.0f;
	
#else
	static float Threshold = 1.3f;
#endif

	const string shaderName = "Hidden/KriptoFX/PostEffects/RFX4_Bloom";

    private const int kMaxIterations = 16;
    private readonly RenderTexture[] m_blurBuffer1 = new RenderTexture[kMaxIterations];
    private readonly RenderTexture[] m_blurBuffer2 = new RenderTexture[kMaxIterations];

#if USE_CUSTOM_RENDERER
	RenderTexture finalBloom;
#else
    RenderTexture Source;
#endif

	private Material _bloomMaterial;
    private Material bloomMaterial
    {
        get
        {
            if (_bloomMaterial == null)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null) Debug.LogError("Can't find shader " + shaderName);
                _bloomMaterial = new Material(shader);

#if USE_CUSTOM_RENDERER
				_bloomMaterial.SetTexture("_DirtTex", DirtTexture);
				_bloomMaterial.SetFloat("_DirtIntensity", DirtIntensity);
#endif
			}

			return _bloomMaterial;
        }
    }

    void Start()
    {
	}

#if USE_CUSTOM_RENDERER
#else
	//void OnRenderImage(RenderTexture Source, RenderTexture Dest)
	//{
	//    UpdateBloom(Source, Dest);
	//}

	void OnPreRender()
    {
        Source = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, SupportedHdrFormat());
        Camera.main.targetTexture = Source;
    }

    void OnPostRender()
    {
        Camera.main.targetTexture = null;
        UpdateBloom(Source, null as RenderTexture);
        RenderTexture.ReleaseTemporary(Source);
    }
#endif

#if USE_CUSTOM_RENDERER
	public RenderTextureFormat SupportedHdrFormat()
#else
	RenderTextureFormat SupportedHdrFormat()
#endif
	{
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
            return RenderTextureFormat.RGB111110Float;
        else return RenderTextureFormat.DefaultHDR;
    }



#if USE_CUSTOM_RENDERER
	public void UpdateBloom(RenderTexture source)
#else
	private void UpdateBloom(RenderTexture source, RenderTexture dest)
#endif
	{
        // source texture size
        var tw = Screen.width / 2;
        var th = Screen.height / 2;

        var rtFormat = RenderTextureFormat.Default;

        
        tw  = (int) (tw * RenderTextureResolutoinFactor);
        th = (int) (th * RenderTextureResolutoinFactor);
          
        // determine the iteration count
        var logh = Mathf.Log(th, 2) - 1;
        var logh_i = (int)logh;
        var iterations = Mathf.Clamp(logh_i, 1, kMaxIterations);

        //// update the shader properties
        var threshold = Mathf.GammaToLinearSpace(bloomThreshold);

        bloomMaterial.SetFloat("_Threshold", threshold);
      
        var sampleScale = 0.5f + logh - logh_i;
     
        bloomMaterial.SetFloat("_SampleScale",  sampleScale * 0.5f);
        bloomMaterial.SetFloat("_Intensity", Mathf.Max(0.0f, bloomIntensity));

        var prefiltered = RenderTexture.GetTemporary(tw, th, 0, rtFormat);
 
        Graphics.Blit(source, prefiltered, bloomMaterial, 0);

        //02457
        // construct A mip pyramid
        var last = prefiltered;
        for (var level = 0; level < iterations; level++)
        {
            m_blurBuffer1[level] = RenderTexture.GetTemporary(last.width / 2, last.height / 2, 0, rtFormat);
            Graphics.Blit(last, m_blurBuffer1[level], bloomMaterial, 1);
            last = m_blurBuffer1[level];
        }

        // upsample and combine loop
        for (var level = iterations - 2; level >= 0; level--)
        {
            var basetex = m_blurBuffer1[level];
            bloomMaterial.SetTexture("_BaseTex", basetex);
            m_blurBuffer2[level] = RenderTexture.GetTemporary(basetex.width, basetex.height, 0, rtFormat);
            Graphics.Blit(last, m_blurBuffer2[level], bloomMaterial, 2);
            last = m_blurBuffer2[level];
        }
#if USE_CUSTOM_RENDERER
		finalBloom = RenderTexture.GetTemporary(last.width, last.height, 0, last.format);
#else
		var finalBloom = RenderTexture.GetTemporary(last.width, last.height, 0, last.format);
#endif
		//bloomMaterial.SetTexture("_BaseTex", Source);

		Graphics.Blit(last, finalBloom, bloomMaterial, 3);
#if USE_CUSTOM_RENDERER
#else
		bloomMaterial.SetTexture("_BaseTex", source);
        Graphics.Blit(finalBloom, dest, bloomMaterial, 4);
#endif

        for (var i = 0; i < kMaxIterations; i++)
        {
            if (m_blurBuffer1[i] != null) RenderTexture.ReleaseTemporary(m_blurBuffer1[i]);
            if (m_blurBuffer2[i] != null) RenderTexture.ReleaseTemporary(m_blurBuffer2[i]);
            m_blurBuffer1[i] = null;
            m_blurBuffer2[i] = null;
        }
        RenderTexture.ReleaseTemporary(finalBloom);
        RenderTexture.ReleaseTemporary(prefiltered);
    }

#if USE_CUSTOM_RENDERER
	public void OnPostRenderAdditiveBloom(RenderTexture source, RenderTexture dest)
	{
		bloomMaterial.SetTexture("_BaseTex", source);
		Graphics.Blit(finalBloom, dest, bloomMaterial, 4);
	}
#endif
}
