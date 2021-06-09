using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaosPurifier : MonoBehaviour
{
	public static ChaosPurifier instance;

	public Animator leverAnimator;
	public Canvas worldCanvas;
	public RectTransform alarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		_position = transform.position;
	}

	void OnEnable()
	{
		RefreshAlarmObject();

		_needUpdate = PlayerData.instance.todayFreePurifyApplied;
		if (_needUpdate)
			_dailyResetTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);
	}

	void OnDisable()
	{
		if (_objectIndicatorCanvas != null)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_objectIndicatorCanvas = null;
		}
		_spawnedIndicator = false;
	}

	Vector3 _position;
	void Update()
	{
		UpdateRemainTime();
		UpdateRefresh();

		if (_spawnedIndicator == false)
			return;
		if (_objectIndicatorCanvas == null)
			return;
		if (_objectIndicatorCanvas.gameObject.activeSelf == false)
			return;

		Vector3 diff = BattleInstanceManager.instance.playerActor.cachedTransform.position - _position;
		diff.y = 0.0f;
		if ((diff.x * diff.x + diff.z * diff.z) > 1.7f * 1.7f)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_spawnedIndicator = false;
		}
	}

	void ShowIndicator()
	{
		AddressableAssetLoadManager.GetAddressableGameObject("ChaosPurifierIndicator", "Canvas", (prefab) =>
		{
			if (this == null) return;
			if (gameObject == null) return;
			if (gameObject.activeInHierarchy == false) return;

			_objectIndicatorCanvas = UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(prefab);
			_objectIndicatorCanvas.targetTransform = transform;
		});

		_spawnedIndicator = true;
	}

	ObjectIndicatorCanvas _objectIndicatorCanvas;
	bool _spawnedIndicator;
	void OnTriggerEnter(Collider other)
	{
		if (_spawnedIndicator)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		ShowIndicator();
	}





	#region AlarmObject
	void RefreshAlarmObject()
	{
		worldCanvas.gameObject.SetActive(false);
		AlarmObject.Hide(alarmRootTransform);
		if (PlayerData.instance.todayFreePurifyApplied == false)
		{
			worldCanvas.gameObject.SetActive(true);
			AlarmObject.Show(alarmRootTransform);
		}
	}
	#endregion


	DateTime _dailyResetTime;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _dailyResetTime)
		{
		}
		else
		{
			_needUpdate = false;
			_needRefresh = true;
		}
	}

	bool _needRefresh = false;
	int _lastCurrent;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (PlayerData.instance.todayFreePurifyApplied == false)
		{
			RefreshAlarmObject();
			_needRefresh = false;
		}
	}
}