using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureChest : MonoBehaviour
{
	public static TreasureChest instance;

	public Transform openCharacterTransform;

	const float gaugeShowDelayTime = 0.2f;

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
	}

	void Start()
	{
		// 차후 알람이 들어가게되면 자동으로 보여주게 처리해야한다.
		bool alarmInShop = false;
		if (alarmInShop)
		{
			ShowIndicator();
		}

		if (ContentsManager.IsTutorialChapter() == false && DownloadManager.instance.IsDownloaded())
		{
			// 일부러 조금 뒤에 보이게 한다. 초기 로딩 줄이기 위해.
			_gaugeShowRemainTime = gaugeShowDelayTime;
		}
	}

	float _gaugeShowRemainTime;
	void Update()
	{
		if (_gaugeShowRemainTime > 0.0f)
		{
			_gaugeShowRemainTime -= Time.deltaTime;
			if (_gaugeShowRemainTime <= 0.0f)
			{
				_gaugeShowRemainTime = 0.0f;
				AddressableAssetLoadManager.GetAddressableGameObject("DailyBoxGaugeCanvas", "Object", (prefab) =>
				{
					BattleInstanceManager.instance.GetCachedObject(prefab, null);
				});
			}
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
