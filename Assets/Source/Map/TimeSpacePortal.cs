﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class TimeSpacePortal : MonoBehaviour
{
	public static TimeSpacePortal instance;

	Vector3 _rootOffsetPosition = new Vector3(0.0f, 0.0f, -75.0f);

	public Canvas worldCanvas;
	public RectTransform alarmRootTransform;

	void Awake()
	{
		// 로비에 있는 것만 instance로 등록한다.
		if (instance == null)
			instance = this;
	}

	bool _started = false;
	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();

		// 여기서 체크없이 하니까 OpenTimeSpacePortal때도 이펙트 나오기 전인데 느낌표부터 보이게 된다.
		if (OpenTimeSpacePortal.instance == null)
			SetAutoRefreshAlarmRemainTime();

		_started = true;
	}

	void OnEnable()
	{
		// 로비에 있는 포탈에만 표시하기 위해 검사
		if (instance != this)
			return;

		if (_started)
			SetAutoRefreshAlarmRemainTime();
	}

	void Update()
	{
		UpdateRefreshAlarm();
	}

	public void SetAutoRefreshAlarmRemainTime(float remainTime = 1.0f)
	{
		_autoRefreshAlarmRemainTime = remainTime;
	}

	float _autoRefreshAlarmRemainTime;
	void UpdateRefreshAlarm()
	{
		if (_autoRefreshAlarmRemainTime > 0.0f)
		{
			_autoRefreshAlarmRemainTime -= Time.deltaTime;
			if (_autoRefreshAlarmRemainTime <= 0.0f)
			{
				// 하필 CommonCanvasGroup.instance가 생성되지 않았다면 대기 시간을 늘려둔다.
				if (CommonCanvasGroup.instance == null)
				{
					_autoRefreshAlarmRemainTime += 0.1f;
					return;
				}
			}
			if (_autoRefreshAlarmRemainTime <= 0.0f)
			{
				_autoRefreshAlarmRemainTime = 0.0f;
				RefreshAlarmObject();
			}
		}
	}

	public void RefreshAlarmObject()
	{
		// 로비에 있는 포탈에만 표시하기 위해 검사
		if (instance != this)
			return;

		// 전투에서 아이템을 습득하거나 장비박스를 굴리거나 상점에서 구매했다면 newEquip이 켜있을 것이다. 로그인땐 켜있지 않을거다.
		if (TimeSpaceData.instance.grantNewEquip)
		{
			AlarmObject.Show(alarmRootTransform);
			worldCanvas.gameObject.SetActive(true);
		}
		else
		{
			AlarmObject.Hide(alarmRootTransform);
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (_processing)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;
		if (GatePillar.instance.processing)
			return;
		if (NodeWarPortal.instance != null && NodeWarPortal.instance.processing)
			return;
		if (RandomBoxScreenCanvas.instance != null && RandomBoxScreenCanvas.instance.gameObject.activeSelf)
			return;

		Timing.RunCoroutine(MoveProcess());
	}

	GameObject _timeSpaceGroundPrefab = null;
	bool _processing = false;
	public bool processing { get { return _processing; } }
	IEnumerator<float> MoveProcess()
	{
		if (_processing)
			yield break;

		_processing = true;
		if (TimeSpaceGround.instance == null && _timeSpaceGroundPrefab == null)
		{
			AddressableAssetLoadManager.GetAddressableGameObject("TimeSpaceGround", "Map", (prefab) =>
			{
				_timeSpaceGroundPrefab = prefab;
			});
		}

		yield return Timing.WaitForSeconds(0.1f);
		//changeEffectParticleRootObject.SetActive(true);

		FadeCanvas.instance.FadeOut(0.2f, 0.7f, true);
		yield return Timing.WaitForSeconds(0.2f);

		if (TitleCanvas.instance != null)
			TitleCanvas.instance.gameObject.SetActive(false);
		if (TimeSpaceGround.instance == null)
		{
			while (_timeSpaceGroundPrefab == null)
				yield return Timing.WaitForOneFrame;
			Instantiate<GameObject>(_timeSpaceGroundPrefab, _rootOffsetPosition, Quaternion.identity);
			LobbyCanvas.instance.OnEnterTimeSpace(true);
		}
		else
		{
			TimeSpaceGround.instance.gameObject.SetActive(!TimeSpaceGround.instance.gameObject.activeSelf);
			LobbyCanvas.instance.OnEnterTimeSpace(TimeSpaceGround.instance.gameObject.activeSelf);
		}

		FadeCanvas.instance.FadeIn(0.4f);

		_processing = false;
	}

	public void HomeProcessByCanvas()
	{
		TimeSpaceGround.instance.gameObject.SetActive(false);
		LobbyCanvas.instance.OnEnterTimeSpace(false);
		FadeCanvas.instance.FadeInOnly(0.5f, 0.9f, true);
	}
}