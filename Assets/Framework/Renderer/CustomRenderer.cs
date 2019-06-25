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

	[Tooltip("Auto Adjust Resolution Factor On Start Function")]
	public bool autoAdjustFactorByDpi = true;

	RFX4_MobileBloom m_bloomComponent;
	PostProcessBase[] m_postProcessList;

#if USE_CUSTOM_RENDERER

	void Awake()
    {
		instance = this;
		m_bloomComponent = GetComponent<RFX4_MobileBloom>();
		m_postProcessList = GetComponents<PostProcessBase>();
	}

	float[] _dpiList = { 240.0f, 320.0f, 480.0f, 640.0f };
	float[] _ratioList = { 0.9f, 0.75f, 0.5f, 0.4f };
	void Start()
	{
		#region AUTO_ADJUST_BY_DPI
#if UNITY_EDITOR
		if (Application.isPlaying == false)
			return;
#endif
		if (autoAdjustFactorByDpi == false)
			return;

		float dpi = Screen.dpi;
		if (dpi == 0.0f)
			return;
		int dpiInt = Mathf.RoundToInt(dpi);

		float selectedRatio = 0.0f;
		for (int i = 0; i < _dpiList.Length; ++i)
		{
			int rountToInt = Mathf.RoundToInt(_dpiList[i]);
			if (dpiInt == rountToInt)
			{
				selectedRatio = _ratioList[i];
				break;
			}
		}
		if (selectedRatio != 0.0f)
		{
			RenderTextureResolutionFactor = selectedRatio;
			return;
		}

		for (int i = 0; i < _dpiList.Length; ++i)
		{
			if (i == 0 && dpi < _dpiList[i])
			{
				selectedRatio = _ratioList[i];
				break;
			}
			if (i == _dpiList.Length - 1 && dpi > _dpiList[i])
			{
				selectedRatio = _ratioList[i];
				break;
			}

			if (i == _dpiList.Length - 1)
				continue;

			if (dpi > _dpiList[i] && dpi < _dpiList[i + 1])
			{
				selectedRatio = Mathf.Lerp(_ratioList[i], _ratioList[i + 1], (dpi - _dpiList[i]) / (_dpiList[i + 1] - _dpiList[i]));
				break;
			}
		}
		if (selectedRatio != 0.0f)
		{
			RenderTextureResolutionFactor = selectedRatio;
			return;
		}
		#endregion
	}

	RenderTexture m_firstRT;

	void OnPreRender()
    {
		int width = Mathf.RoundToInt(Screen.width * RenderTextureResolutionFactor);
		int height = Mathf.RoundToInt(Screen.height * RenderTextureResolutionFactor);
		m_firstRT = RenderTexture.GetTemporary(width, height, 24, m_bloomComponent.SupportedHdrFormat());
        Camera.main.targetTexture = m_firstRT;
    }

	public int needGrab_refCount { get; set; }
    void OnPostRender()
    {
        Camera.main.targetTexture = null;
		m_bloomComponent.UpdateBloom(m_firstRT);

		// 원래라면 grab 하나 새로 파서 들고있어야하는게 맞는데 티가 안나는거 같아서 이렇게 firstRT를 넘겨본다.
		if (needGrab_refCount > 0)
		{
			Shader.SetGlobalTexture("_GrabTexture", m_firstRT);
			Shader.SetGlobalTexture("_GrabTextureMobile", m_firstRT);
		}

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
