using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class GatePillar : MonoBehaviour
{
	public static GatePillar instance;

	public GameObject meshColliderObject;
	public GameObject particleRootObject;
	public GameObject changeEffectParticleRootObject;

	// 동적 로드하는 것들을 외부로 뺄까 했는데 크기가 크지 않고 자주 쓰이는거라면 굳이 뺄필요가 없어서 안빼기로 한다.
	ObjectIndicatorCanvas _objectIndicatorCanvas;
	public GameObject descriptionObjectIndicatorPrefab;
	public float descriptionObjectIndicatorShowDelayTime = 5.0f;
	public float energyGaugeShowDelayTime = 2.0f;

	void Awake()
	{
		instance = this;
	}

	float _spawnTime;
	void OnEnable()
	{
		_spawnTime = Time.time;
		if (string.IsNullOrEmpty(StageManager.instance.currentGatePillarPreview) == false)
		{
			AddressableAssetLoadManager.GetAddressableAsset("ImageObjectIndicator", "Object", (prefab) =>
			{
				_objectIndicatorCanvas = UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(prefab);
				_objectIndicatorCanvas.targetTransform = cachedTransform;
			});
			return;
		}
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && PlayerData.instance.tutorialChapter == false)
		{
			// 일부러 조금 뒤에 보이게 한다. 초기 로딩 줄이기 위해.
			_energyGaugeShowRemainTime = energyGaugeShowDelayTime;
			return;
		}
		_descriptionObjectIndicatorShowRemainTime = descriptionObjectIndicatorShowDelayTime;
	}

	void OnDisable()
	{
		particleRootObject.SetActive(false);
		changeEffectParticleRootObject.SetActive(false);

		if (_objectIndicatorCanvas != null)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_objectIndicatorCanvas = null;
		}
	}

	float _descriptionObjectIndicatorShowRemainTime;
	float _energyGaugeShowRemainTime;
	void Update()
	{
		if (_descriptionObjectIndicatorShowRemainTime > 0.0f)
		{
			_descriptionObjectIndicatorShowRemainTime -= Time.deltaTime;
			if (_descriptionObjectIndicatorShowRemainTime <= 0.0f)
			{
				_descriptionObjectIndicatorShowRemainTime = 0.0f;
				_objectIndicatorCanvas = UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(descriptionObjectIndicatorPrefab);
				_objectIndicatorCanvas.targetTransform = cachedTransform;
			}
		}
		if (_energyGaugeShowRemainTime > 0.0f)
		{
			_energyGaugeShowRemainTime -= Time.deltaTime;
			if (_energyGaugeShowRemainTime <= 0.0f)
			{
				_energyGaugeShowRemainTime = 0.0f;
				AddressableAssetLoadManager.GetAddressableAsset("EnergyGauge", "Object", (prefab) =>
				{
					BattleInstanceManager.instance.GetCachedObject(prefab, null);
				});
				return;
			}
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		if (_processing)
			return;

		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;
			HitObject hitObject = BattleInstanceManager.instance.GetHitObjectFromCollider(col);
			if (hitObject == null)
				continue;
			if (hitObject.statusStructForHitObject.teamID == (int)Team.eTeamID.DefaultMonster)
				continue;
			if (hitObject.createTime < _spawnTime)
				continue;

			Timing.RunCoroutine(NextMapProcess());
			break;
		}
	}

	bool _processing = false;
	IEnumerator<float> NextMapProcess()
	{
		if (_processing)
			yield break;

		_processing = true;

		yield return Timing.WaitForSeconds(0.2f);
		changeEffectParticleRootObject.SetActive(true);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(gameObject);
#endif
		CustomRenderer.instance.bloom.AdjustDirtIntensity(1.5f);

		yield return Timing.WaitForSeconds(0.5f);

		FadeCanvas.instance.FadeOut(0.2f);
		yield return Timing.WaitForSeconds(0.2f);

		if (MainSceneBuilder.instance.lobby)
		{
			while (MainSceneBuilder.instance.IsDoneLateInitialized() == false)
				yield return Timing.WaitForOneFrame;
			if (TitleCanvas.instance != null)
				TitleCanvas.instance.gameObject.SetActive(false);
			MainSceneBuilder.instance.OnExitLobby();
		}
		while (StageManager.instance.IsDoneLoadAsyncNextStage() == false)
			yield return Timing.WaitForOneFrame;
		CustomRenderer.instance.bloom.ResetDirtIntensity();
		StageManager.instance.MoveToNextStage();
		gameObject.SetActive(false);

		FadeCanvas.instance.FadeIn(0.4f);

		_processing = false;
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
