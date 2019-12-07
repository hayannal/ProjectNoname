using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureChest : MonoBehaviour
{
	public static TreasureChest instance;

	public Transform openCharacterTransform;

	void Awake()
	{
		instance = this;
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

	void Start()
	{
		// 차후 알람이 들어가게되면 자동으로 보여주게 처리해야한다.
		bool alarmInShop = false;
		if (alarmInShop)
		{
			ShowIndicator();
		}
	}

	void ShowIndicator()
	{
		AddressableAssetLoadManager.GetAddressableGameObject("TreasureChestIndicator", "Object", (prefab) =>
		{
			// 로딩하는 중간에 맵이동시 전투맵으로 들어가고 나서 TreasureChestIndicator가 나와버리게 된다. 그래서 체크로직 추가한다.
			if (this == null) return;
			if (gameObject == null) return;
			if (gameObject.activeSelf == false) return;

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
}
