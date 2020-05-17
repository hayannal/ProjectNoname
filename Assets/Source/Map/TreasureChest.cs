using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureChest : MonoBehaviour
{
	public static TreasureChest instance;

	public Transform openCharacterTransform;

	public Renderer topRenderer;
	public Renderer bottomRenderer;
	public ParticleSystem[] particleSystemList;

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
		if (TreasureChestIndicatorCanvas.IsDailyBoxType())
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
				AddressableAssetLoadManager.GetAddressableGameObject("DailyBoxGaugeCanvas", "Canvas", (prefab) =>
				{
					BattleInstanceManager.instance.GetCachedObject(prefab, null);
				});
			}
		}

		UpdateActivate();
	}

	void ShowIndicator()
	{
		AddressableAssetLoadManager.GetAddressableGameObject("TreasureChestIndicator", "Canvas", (prefab) =>
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

	#region Gacha
	// 원래는 없었다가 가차 하면서 생긴 함수. 임시로 인디케이터 하이드 시키는 기능이다.
	public bool IsShowIndicatorCanvas()
	{
		if (_objectIndicatorCanvas == null)
			return false;
		if (_objectIndicatorCanvas.gameObject == null)
			return false;
		return _objectIndicatorCanvas.gameObject.activeSelf;
	}

	public void HideIndicatorCanvas(bool hide)
	{
		_objectIndicatorCanvas.gameObject.SetActive(!hide);
	}

	float _shieldActivationDir;
	float _shieldActivationTime = 1.0f;
	int _activationTimeProperty;
	float _shieldActivationSpeed = 0.8f;
	float _shieldActivationRim = 0.2f;
	public void ActivateEffect(bool active)
	{
		if (_activationTimeProperty == 0)
			_activationTimeProperty = Shader.PropertyToID("_ActivationTime");

		_shieldActivationDir = (active) ? 1.0f : -1.0f;
		topRenderer.material.SetFloat(_activationTimeProperty, _shieldActivationTime);
		bottomRenderer.material.SetFloat(_activationTimeProperty, _shieldActivationTime);

		for (int i = 0; i < particleSystemList.Length; ++i)
		{
			ParticleSystem.EmissionModule emission = particleSystemList[i].emission;
			emission.enabled = active;
		}
	}

	void UpdateActivate()
	{
		if (_shieldActivationDir > 0.0f)
		{
			_shieldActivationTime += _shieldActivationSpeed * Time.deltaTime;
			if (_shieldActivationTime >= 1.0f)
			{
				_shieldActivationTime = 1.0f;
				_shieldActivationDir = 0.0f;
			}
			topRenderer.material.SetFloat(_activationTimeProperty, _shieldActivationTime);
			bottomRenderer.material.SetFloat(_activationTimeProperty, _shieldActivationTime);
		}
		else if (_shieldActivationDir < 0.0f)
		{
			_shieldActivationTime -= _shieldActivationSpeed * Time.deltaTime;
			if (_shieldActivationTime <= -_shieldActivationRim)
			{
				_shieldActivationTime = -_shieldActivationRim;
				_shieldActivationDir = 0.0f;
			}
			topRenderer.material.SetFloat(_activationTimeProperty, _shieldActivationTime);
			bottomRenderer.material.SetFloat(_activationTimeProperty, _shieldActivationTime);
		}
	}
	#endregion
}
