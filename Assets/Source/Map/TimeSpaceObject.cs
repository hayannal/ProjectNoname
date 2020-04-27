using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 시공간에 있는 서브 오브젝트 두개를 위해 만든 클래스. 일괄장착 표지판 및 일괄판매 휴지통에 쓴다.
public class TimeSpaceObject : MonoBehaviour
{
	public float indicatorDistance = 2.0f;
	public string indicatorAddress;

	public enum eTimeSpaceObjectType
	{
		AutoEquip,
		Sell,
	}
	public eTimeSpaceObjectType timeSpaceObjectType;

	void Start()
	{
		_position = transform.position;
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
		if ((diff.x * diff.x + diff.z * diff.z) > indicatorDistance * indicatorDistance)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_spawnedIndicator = false;

			// 자동장착이 사라지는 순간이 제단으로 다가가고 있을때를 알리는 순간이기도 하다. 이때 인벤에 들어있는 아이템들의 아이콘을 프리로드한다.
			if (timeSpaceObjectType == eTimeSpaceObjectType.AutoEquip)
				TimeSpaceData.instance.PreloadEquipIcon();
		}
	}

	void ShowIndicator()
	{
		AddressableAssetLoadManager.GetAddressableGameObject(indicatorAddress, "Canvas", (prefab) =>
		{
			if (this == null) return;
			if (gameObject == null) return;
			if (gameObject.activeInHierarchy == false) return;

			_objectIndicatorCanvas = (TimeSpaceObjectIndicatorCanvas)UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(prefab);
			_objectIndicatorCanvas.targetTransform = transform;
			_objectIndicatorCanvas.SetType(timeSpaceObjectType);
		});

		_spawnedIndicator = true;
	}

	TimeSpaceObjectIndicatorCanvas _objectIndicatorCanvas;
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
}