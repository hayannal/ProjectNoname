using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyBoxGaugeCanvas : MonoBehaviour
{
	public static DailyBoxGaugeCanvas instance;

	public Image[] gaugeList;
	public GameObject secondGaugeRootObject;
	public Image[] secondGaugeList;
	public Text remainTimeText;
	public RectTransform remainTimeTransform;
	public RectTransform remainTimeSubTransform;

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
		RefreshGauge();
	}

	void Update()
	{
		if (PlayerData.instance.clientOnly)
			return;

		UpdateRemainTime();
		UpdateRefresh();
	}

	public void RefreshGauge()
	{
		int current = PlayerData.instance.sealCount;
		bool opened = PlayerData.instance.sharedDailyBoxOpened;

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