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

	void Awake()
	{
		instance = this;
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

		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByResearchLevel.SecondDailyBox))
		{

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

		if (DateTime.UtcNow < _nextResetDateTime)
		{
			TimeSpan remainTime = _nextResetDateTime - DateTime.UtcNow;
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