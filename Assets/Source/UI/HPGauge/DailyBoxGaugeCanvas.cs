using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class DailyBoxGaugeCanvas : MonoBehaviour
{
	public static DailyBoxGaugeCanvas instance;

	public Image[] gaugeList;
	public GameObject secondGaugeRootObject;
	public Image[] secondGaugeList;
	public Text remainTimeText;
	public RectTransform remainTimeTransform;
	public RectTransform remainTimeSubTransform;
	public Transform sealImageTransform;
	public GameObject sealImageGainEffectObject;

	public Sprite fillSprite;
	public Sprite strokeSprite;

	public Color normalColor;
	public Color highlightColor;
	public Color disableColor;

	Vector2 _defaultRemainTimeAnchoredPosition;
	void Awake()
	{
		instance = this;
		_defaultRemainTimeAnchoredPosition = remainTimeTransform.anchoredPosition;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		cachedTransform.position = TreasureChest.instance.transform.position;
		RefreshGauge(true);
	}

	void Update()
	{
		if (PlayerData.instance.clientOnly)
			return;

		UpdateRemainTime();
		UpdateRefresh();
		UpdateGainEffect();
	}

	public void RefreshGauge(bool applyGainProcess = false)
	{
		int current = PlayerData.instance.sealCount;
		bool opened = PlayerData.instance.sharedDailyBoxOpened;

		// 이벤트를 진행해야한다면 GainProcess를 돌리지 않기로 한다.
		if (EventManager.instance.IsStandbyClientEvent(EventManager.eClientEvent.OpenSecondDailyBox) || EventManager.instance.IsStandbyServerEvent(EventManager.eServerEvent.chaos))
			applyGainProcess = false;

		// opened가 아니면서 sealGainCount가 0보다 클때는 획득 연출이 발생하는 타이밍일거다.
		if (applyGainProcess && PlayerData.instance.sealGainCount > 0 && PlayerData.instance.sealGainCount <= current && opened == false)
		{
			_reservedGainCount = PlayerData.instance.sealGainCount;
			PlayerData.instance.sealGainCount = 0;

			// 일부 조건에서는 획득 연출만 해주고 게이지는 그대로 간다.
			// 연출을 기다렸다가 꽉차면 받게 하는 식은 스텝을 기다려야해서 불편할테니 예외처리 하는거다.
			_onlyDropEffect = false;
			if (current >= gaugeList.Length)
				_onlyDropEffect = true;
			else
			{
				current -= _reservedGainCount;
				_gainStartIndex = current - 1;
			}
		}

		for (int i = 0; i < gaugeList.Length; ++i)
		{
			if (opened || i < current)
				gaugeList[i].sprite = fillSprite;
			else
				gaugeList[i].sprite = strokeSprite;

			if (opened)
				gaugeList[i].color = disableColor;
			else if (current < gaugeList.Length)
				gaugeList[i].color = normalColor;
			else
				gaugeList[i].color = highlightColor;
		}

		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.SecondDailyBox) && EventManager.instance.IsStandbyClientEvent(EventManager.eClientEvent.OpenSecondDailyBox) == false)
		{
			// 오리진 상자를 받을때 한칸을 채우느냐 혹은 인장을 8개 채울때 한칸을 채우느냐에서 후자를 선택하기로 했다.
			// 대신 서버에서 +1을 하는 시점은 오리진 상자를 받을때 처리하는거라 표시할때는
			// 현재 저장되어있는 secondDailyBoxFillCount를 받아와서 인장을 8개 채웠을때 +1 해서 보여주면 된다.
			// 이러면 기본 줄이 노란색으로 보이는 순간 한칸이 차는 것처럼 보이게 될거고
			// 3번째 채우는 순간 secondDailyBox 줄도 노란색으로 보이게 되면서 큰 상자가 나오면 된다.
			current = PlayerData.instance.secondDailyBoxFillCount;
			if (opened == false && PlayerData.instance.sealCount >= gaugeList.Length)
				current += 1;
			for (int i = 0; i < secondGaugeList.Length; ++i)
			{
				// 예외처리.
				// 3번째 채우는 순간 secondDailyBoxFillCount를 0으로 초기화 해두기때문에 이런식으로 처리해야 3번째 상자를 오픈했는지 알 수 있다.
				if ((opened && current == 0) || i < current)
					secondGaugeList[i].sprite = fillSprite;
				else
					secondGaugeList[i].sprite = strokeSprite;

				if (opened)
					secondGaugeList[i].color = disableColor;
				else if (current < secondGaugeList.Length || PlayerData.instance.sealCount < gaugeList.Length)
					secondGaugeList[i].color = normalColor;
				else
					secondGaugeList[i].color = highlightColor;
			}

			secondGaugeRootObject.SetActive(true);
			remainTimeTransform.anchoredPosition = _defaultRemainTimeAnchoredPosition;
		}
		else
		{
			secondGaugeRootObject.SetActive(false);
			remainTimeTransform.anchoredPosition = remainTimeSubTransform.anchoredPosition;
		}

		if (opened)
		{
			_nextResetDateTime = PlayerData.instance.dailyBoxResetTime;
			_needUpdate = true;
			remainTimeText.gameObject.SetActive(true);
		}
		else
		{
			remainTimeText.gameObject.SetActive(false);
		}
	}

	DateTime _nextResetDateTime;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (PlayerData.instance.sharedDailyBoxOpened == false)
			return;
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _nextResetDateTime)
		{
			TimeSpan remainTime = _nextResetDateTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			// 패킷을 보내서 일퀘 갱신을 물어보고 갱신이 되었다면 플래그가 바뀔거다.
			// 시간을 변조했다면 갱신이 안될 타이밍일테니 계속해서 00:00:00으로 표시된채 멈춰있을거다.
			_needUpdate = false;
			remainTimeText.text = "00:00:00";
			_needRefresh = true;
		}
	}

	bool _needRefresh = false;
	int _lastCurrent;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (PlayerData.instance.sharedDailyBoxOpened == false)
		{
			RefreshGauge();
			_needRefresh = false;
		}
	}




	#region Gain Effect
	int _reservedGainCount = 0;
	int _gainStartIndex = 0;
	bool _onlyDropEffect = false;
	void UpdateGainEffect()
	{
		// 예약된게 있다면 EventManager.instance.OnLobby가 호출되든 말든 동시에 진행한다.
		if (_reservedGainCount == 0)
			return;

		// 그럴리는 없겠지만 게이트 필라가 없다면 진행하면 안될거 같다.
		if (GatePillar.instance == null)
			return;
		if (GatePillar.instance.gameObject.activeSelf == false)
			return;

		// DropObjectGroup 이 만들어지지 않으면 dropSealGainPrefab이 존재하지 않는다. LateInitialize에서 만들기때문에 체크해야한다.
		if (DropObjectGroup.instance == null)
			return;

		// 전투하고 나서 로비로 돌아온 다음에 진행되기 때문에 로딩 캔버스가 풀리는 타임을 기다려야한다.
		// 이러려면 특이하게도 인스턴스는 있되 gameObject.activeSelf가 false 인 상태를 기다려야한다.
		if (LoadingCanvas.instance != null && LoadingCanvas.instance.gameObject.activeSelf == false)
		{
			// 게이트 필라 근처에서 인장을 생성해야한다.
			int createCount = _reservedGainCount;
			_reservedGainCount = 0;
			Timing.RunCoroutine(DropProcess(createCount));
		}
	}

	IEnumerator<float> DropProcess(int dropCount)
	{
		float delay = 0.2f;
		for (int i = 0; i < dropCount; ++i)
		{
			Vector2 normalizedOffset = UnityEngine.Random.insideUnitCircle.normalized;
			Vector2 randomOffset = normalizedOffset * UnityEngine.Random.Range(0.7f, 1.4f);
			Vector3 desirePosition = GatePillar.instance.cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);
			BattleInstanceManager.instance.GetCachedObject(DropObjectGroup.instance.dropSealGainPrefab, desirePosition, Quaternion.identity);

			if (i < dropCount - 1)
				yield return Timing.WaitForSeconds(delay);

			if (this == null)
				yield break;
		}
	}

	public void FillGauge()
	{
		sealImageGainEffectObject.SetActive(false);
		sealImageGainEffectObject.SetActive(true);

		// 드랍 연출만 보여주는 상태라면 게이지 변경하지 않는다.
		if (_onlyDropEffect)
			return;

		// 호출될때마다 증가시킨 후 색상 변경
		++_gainStartIndex;
		if (_gainStartIndex < gaugeList.Length)
			gaugeList[_gainStartIndex].sprite = fillSprite;
	}
	#endregion


	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}