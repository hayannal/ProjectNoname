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

		// lobbyDownloadState가 켜있을때는 튜토맵을 로딩할거기 때문에 1챕터 클라이벤트도 표시를 하지 않고 다음번 제대로 씬이 로딩될때 보여주도록 한다.
		if (PlayerData.instance.lobbyDownloadState)
		{
			DownloadManager.instance.ShowLobbyDownloadInfo();
			return;
		}

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

		// 화면 잠구고 진행하는 서버 이벤트일 경우엔 나머지 일들을 수행할 필요가 없다. 그러니 미리 체크해서 기억시켜두고
		bool lockScreenServerEvent = false;
		if (EventManager.instance.IsStandbyServerEvent(EventManager.eServerEvent.chaos) || EventManager.instance.IsStandbyServerEvent(EventManager.eServerEvent.node))
			lockScreenServerEvent = true;

		// research나 balance같이 화면 잠구는 이벤트가 아니라면 플래그만 걸어두고 나머지를 진행하면 된다.
		if (EventManager.instance.IsStandbyServerEvent())
			EventManager.instance.OnLobby();

		if (lockScreenServerEvent)
		{ }
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
					TermsConfirmCanvas.instance.ShowCanvas(() =>
					{
						// 보통의 경우엔 안해도 되지만, 예외처리 하나가 있다.
						// 튜토를 진행하는데 하필 데이터는 다 받은 상태고(보통은 로그아웃으로 인한 걸거다.) 이때 이미 타이틀캔버스는 보이지 않아도 되는 케이스라서
						// 튜토 깨자마자 1챕터로 넘어올텐데 예외처리를 하기 위해 checkRestartScene은 걸려있는 상태일거다.
						// 약관창 처리를 위해서 이렇게 해둔건데 문제는 NewChapter 이벤트가 큐에 쌓여져있어서
						// 다음번 씬 이동시에 나올텐데 이걸 바로 나오게 하기 위해서 여기에서 처리해둔다.
						bool showTitleCanvas = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf);
						if (EventManager.instance.IsStandbyClientEvent(EventManager.eClientEvent.NewChapter) && showTitleCanvas == false)
							EventManager.instance.OnCompleteLobbyEvent();
					});
				});
			}
		}
	}
}
