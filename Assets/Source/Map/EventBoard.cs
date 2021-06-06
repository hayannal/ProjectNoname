using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventBoard : MonoBehaviour
{
	public static EventBoard instance;

	public Canvas worldCanvas;
	public Canvas alarmWorldCanvas;
	public GameObject onObject;
	public GameObject offObject;
	public RectTransform alarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		worldCanvas.worldCamera = alarmWorldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		_position = transform.position;
	}

	void OnEnable()
	{
		RefreshBoardOnOff();
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
		AddressableAssetLoadManager.GetAddressableGameObject("EventBoardIndicator", "Canvas", (prefab) =>
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




	public void RefreshBoardOnOff()
	{
		bool activeEvent = (CumulativeEventData.instance.GetActiveEventCount() > 0);
		onObject.SetActive(activeEvent);
		offObject.SetActive(!activeEvent);

		// 알람 체크
	}
}