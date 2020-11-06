using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LoadingCanvas : MonoBehaviour
{
	public static LoadingCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(Resources.Load<GameObject>("UI/LoadingCanvas")).GetComponent<LoadingCanvas>();
			}
			return _instance;
		}
	}
	static LoadingCanvas _instance = null;

	public GameObject progressObject;
	public Image progressImage;
	public Text progressText;
	public Text backgroundDownloadText;
	public DOTweenAnimation objectFadeTweenAnimation;
	public DOTweenAnimation backgroundFadeTweenAnimation;

	float _enableTime;
	void OnEnable()
	{
		_enableTime = Time.realtimeSinceStartup;
	}

	void OnDisable()
	{
		float lifeTime = Time.realtimeSinceStartup - _enableTime;
		Debug.LogFormat("Loading Time : {0:0.###}", lifeTime);
	}

	#region Progress
	float _targetValue = 0.0f;
	float _fillSpeed;
	public void SetProgressBarPoint(float value, float fillSpeed = 0.3f, bool immediateFill = false)
	{
		if (_targetValue != 0.0f && progressImage.fillAmount < _targetValue)
			progressImage.fillAmount = _targetValue;
		_targetValue = value;
		if (immediateFill)
			progressImage.fillAmount = _targetValue;
		else
			_fillSpeed = fillSpeed;
	}

	public bool skipProgressAnimation { get; set; }
	void Update()
	{
		if (skipProgressAnimation)
			return;

		if (progressImage.fillAmount >= 1.0f)
			return;

		if (progressImage.fillAmount < _targetValue)
		{
			progressImage.fillAmount += _fillSpeed * Time.deltaTime;
			if (progressImage.fillAmount > 1.0f)
				progressImage.fillAmount = 1.0f;
		}
	}
	#endregion

	public bool onlyObjectFade { get; set; }

	public void FadeOutObject()
	{
		progressObject.SetActive(false);
		objectFadeTweenAnimation.DOPlay();
	}
	
	public void OnCompleteObjectFade()
	{
		if (onlyObjectFade)
		{
			if (TitleCanvas.instance != null)
				TitleCanvas.instance.ShowTitle();
			gameObject.SetActive(false);
		}
		else
			backgroundFadeTweenAnimation.DOPlay();
	}

	public void OnCompleteBackgroundFade()
	{
		gameObject.SetActive(false);

		// 타이틀 안나올때의 로비 진입 이벤트. 여기가 시작점이다.
		if (PlayerData.instance.checkRestartScene)
		{
			PlayerData.instance.checkRestartScene = false;

			// 네트워크 오류 등으로 인한 재접속시에는 앱 구동시 타이틀 나오는때처럼 모든걸 검사해야한다.
			OnEnterLobby();
			return;
		}

		// 평소에는 게임 구동 후 씬전환때만 들어오기 때문에 Event 체크만 해도 된다.
		EventManager.instance.OnLobby();
	}

	public void OnEnterLobby()
	{
		// 우선순위 높은거부터 처리할게 있는지 판단한다.
		// Event를 진행할게 있다면 튕겨서 재접한 상황은 아니라 정상적인 종료나 패배일거다. 처음 켤때만 호출되는 곳이니 서버 이벤트만 있는지 검사하면 된다.
		// 클라이벤트는 씬 재시작시 어차피 사라지기 때문에 서버이벤트 여부만 판단해도 충분하다.
		if (EventManager.instance.IsStandbyServerEvent())
			EventManager.instance.OnLobby();
		else if (ClientSaveData.instance.IsCachedInProgressGame())
			LobbyCanvas.instance.CheckClientSaveData();
		else
		{
			// 아무것도 처리할게 없을때 언어옵션이 한국어라면 약관 띄울 준비를 한다.
			// 약관창 뜬 상태에서 네트워크 오류로 씬 재시작시 checkRestartScene플래그가 켜지기 때문에 다시 이쪽으로 들어오게 될거다.
			if (PlayerData.instance.termsConfirmed == false && ContentsManager.IsTutorialChapter() == false && OptionManager.instance.language == "KOR")
			{
				UIInstanceManager.instance.ShowCanvasAsync("TermsConfirmCanvas", () =>
				{
					TermsConfirmCanvas.instance.ShowCanvas(null);
				});
			}
		}
	}
}
