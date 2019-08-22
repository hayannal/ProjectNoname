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

	public RFX4_MobileBloom bloom { get { return m_bloomComponent; } }

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
		//
		// 티가 안나는 이유가 있었다. 바로 GrabPass를 호출해서 직전에 굽고 있었으니 전달하든 말든 상관없이 이펙트가 잘 나왔던 것.
		// 자세한 내용은 엄청 기니 시간될때 읽을 것.
		//
		// 우선 사건의 시작은 다음과 같다.
		// GatePillar에 쓸 이펙트가 필요해서 하나 사왔는데 여기에 GrabPass쓰는 왜곡이 달려있길래
		// (당연히 지금 커스텀 렌더러 구조에서 GrabPass를 쓰면 느릴거라 생각해서) GrabPass를 없애고
		// OnPostRender에서 프레임마다 GlobalTexture로 _GrabTexture를 보내고 있으니 자동으로 왜곡이 잘 나올거라 생각했는데
		// 왜곡이 제대로 안나오는 것이었다.(중첩되서 점점 하얗게 되는 현상이 나타났다.)
		// 하얗게 뜨는 현상은 왜곡 객체와 비왜곡 객체를 동시에 그릴때 나타나는 현상이다.(RFX4 신버전에선 이전의 페카 방식이 아니란 얘기다.)
		//
		// RFX4의 왜곡 이펙트들은 잘 나오는데 뭐가 다른거지 하고 RFX4 왜곡 쉐이더 및 이펙트들을 꺼내서 비교해보기로 했는데..
		// 꺼내보니 RFX4 이펙트들이 쓰는 쉐이더에 GrabPass가 들어있던 것이다.
		// 아니 도대체 이 느린걸 쓰면서 모바일 쉐이더라고 속이고 판건가 라는 생각부터 별 생각이 다 들어서 프레임 디버거를 돌려보았더니
		// 두둥..
		// GrabPass의 크기가 내가 백버퍼 줄인만큼의 작은 크기였다.
		// 게다가 GrabPass에다가 텍스처 이름을 넘기고 있어서 프레임당 1회만 호출되고 있었다. (이름을 주석처리하면 한프레임에 3~4회씩 왜곡 개수만큼 호출된다.)
		//
		// 곰곰히 생각해보니 메시이펙트 전버전도 그렇고 페카때 렌더러 개발했던거 기억해보면
		// GrabPass를 피하기 위해 왜곡 객체와 그렇지 않은걸 구분하기 위해
		// 카메라든 레이어든 심지어 쉐이더 feature든 여러가지 방법을 써서 왜곡 아닌걸 그린 후 임시버퍼에 저장하고 이후 왜곡을 그리는 식의 처리를 했었는데
		// 이 절차들이 아무리 간소화해도 은근 까다로웠다. (당연히 GrabPass 한줄 붙이는거에 비해선 손이 많이 갔다.)
		// 근데 RFX4 신버전도 그렇고 메시 이펙트 신버전도 그렇고
		// 저런 쉐이더 셋팅 코드들이 사라져서 의아해했었는데 - 예전 메시이펙트와 달리 키워드로 구분하는게 사라져있다.
		// (이런 구분코드가 없으니 GrabPass를 삭제하면 하얗게 되는 현상이 나왔던거다)
		//
		// 결국 GrabPass를 부활시켜서 쓴거였고 이럴 수 있었던건 언제서부턴가인지 GrabPass의 크기를 조절할 수 있게 되면서였던거 같다.
		// 크기가 조절되니 최적화에 크게 문제가 되지 않는다고 생각해서
		// 어차피 왜곡객체 비왜곡객체 나눠그리는것도 그리 썩 편하지 않으니
		// 차라리 GrabPass를 쓰는쪽으로 바꾼거 같다. 크기도 해상도 낮추는만큼 확 낮추면 그닥 느려지지 않는다.
		// (굽는 타이밍까지 제어할 수 없는건 아쉽지만, 어차피 10프레임이든 20프레임 정도로 구우면 두번중 한번 건너뛰는 정도라서 꼭 필요한건 아니다.
		//  게다가 이경우 프레임이 들쭉 날쭉 해진다.)
		// 
		// 궁금해서 GrabPass의 크기를 제어하는 코드가 뭔지 살펴보니
		// Camera.main.targetTexture = m_firstRT;
		// 카메라의 타겟 텍스처에 작은 RT를 넣으면 알아서 GrabPass의 크기도 작게 되는거였다.
		// 이렇게 단순한걸 왜 예전 버전에선 안해준거지..
		// 아무튼 이게 되면서 더이상 GrabPass를 안쓸 이유가 사라졌다.
		// 대신 프레임당 여러번 그리는건 그래도 안되니, GrabPass쓰는곳에 꼭
		// GrabPass {
		// "_GrabTexture"
		// }
		// 이런식으로 이름을 정해주는 코드를 넣어야한다.
		//
		// 그런데 참고로 UI-Refraction에서는 GrabPass를 추가하니
		// 렌더링 블럭이 달라서 그런지 하단에 또 하나의 GrabPass가 추가되었다.
		// 그래서 UI용으로는 이전처럼 _GrabTextureMobile 넘겨서 쓰기로 한다.
		// UI용은 이상하게 따로 m_GrabRT로 저장해두지 않고 m_firstRT를 쓰더라도(Release하는 버퍼인데도) 잘 나온다. UI는 뭔가 다른가..
		//
		// 그리고 사실 사온 이펙트도 GrabPass로 되어있으니 잘 나오던거였는데
		// 씬뷰와 달리 게임뷰에서 모양이 작아서 안보였던 것이었다.
		// 왜곡량을 늘리면 잘 보인다.
		// 대신 GrabPass에 텍스처 이름을 안넘기고있어서 그것만 넘기는거로 수정해두기로 한다.
		if (needGrab_refCount > 0)
		{
			// 이런식의 저장이 더 이상 필요없어지게 된거다.
			//Graphics.Blit(m_firstRT, m_GrabRT);

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
