#define USE_CUSTOM_RENDERER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CustomRenderer : MonoBehaviour
{
	public static CustomRenderer instance;

	[Range(0.2f, 1)]
	[Tooltip("Camera render texture resolution")]
	public float RenderTextureResolutionFactor = 0.5f;

	RFX4_MobileBloom m_bloomComponent;
	PostProcessBase[] m_postProcessList;

#if USE_CUSTOM_RENDERER

	void Awake()
    {
		instance = this;
		m_bloomComponent = GetComponent<RFX4_MobileBloom>();
		m_postProcessList = GetComponents<PostProcessBase>();
	}

	RenderTexture m_firstRT;

	void OnPreRender()
    {
		int width = Mathf.RoundToInt(Screen.width * RenderTextureResolutionFactor);
		int height = Mathf.RoundToInt(Screen.height * RenderTextureResolutionFactor);
		m_firstRT = RenderTexture.GetTemporary(width, height, 24, m_bloomComponent.SupportedHdrFormat());
        Camera.main.targetTexture = m_firstRT;
    }

    void OnPostRender()
    {
        Camera.main.targetTexture = null;
		m_bloomComponent.UpdateBloom(m_firstRT);

		PostProcess();

		m_bloomComponent.OnPostRenderAdditiveBloom(m_firstRT, null as RenderTexture);
		RenderTexture.ReleaseTemporary(m_firstRT);
    }

	#region PostProcess

	RenderTexture m_secondRT;
	void PostProcess()
	{
		if (m_postProcessList == null || m_postProcessList.Length == 0)
			return;

		int activeCount = 0;
		for (int i = 0; i < m_postProcessList.Length; ++i)
		{
			if (m_postProcessList[i].enabled)
			{
				++activeCount;
				break;
			}
		}
		if (activeCount == 0)
			return;


		for (int i = 0; i < m_postProcessList.Length; ++i)
		{
			if (m_postProcessList[i].enabled == false)
				continue;

			if (m_postProcessList[i].UseSecondRT())
				m_secondRT = RenderTexture.GetTemporary(m_firstRT.width, m_firstRT.height, m_firstRT.depth, m_firstRT.format);

			m_postProcessList[i].OnPostProcess(m_firstRT, m_secondRT);

			if (m_postProcessList[i].UseSecondRT())
			{
				RenderTexture.ReleaseTemporary(m_firstRT);
				m_firstRT = m_secondRT;
			}
		}
	}

	#endregion
#endif
}
