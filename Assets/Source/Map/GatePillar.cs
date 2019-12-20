using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
	public float energyGaugeShowDelayTime = 0.2f;

	public Canvas worldCanvas;
	public Text floorText;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	float _spawnTime;
	void OnEnable()
	{
		// 보스 게이트필라가 추가되면서 instance가 두개 생겼다.
		// instance 구조를 뽑아버릴까 하다가
		// 항상 하나의 게이트필라만 켜지는 구조라서 OnEnable에서 덮어쓰는거로 처리해본다.
		instance = this;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		floorText.text = "";

		_spawnTime = Time.time;
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && PlayerData.instance.tutorialChapter == false)
		{
			// 일부러 조금 뒤에 보이게 한다. 초기 로딩 줄이기 위해.
			_energyGaugeShowRemainTime = energyGaugeShowDelayTime;
			return;
		}
		// 보스 인디케이터는 빠르게 등장한다.
		if (string.IsNullOrEmpty(StageManager.instance.nextMapTableData.bossName))
			_descriptionObjectIndicatorShowRemainTime = descriptionObjectIndicatorShowDelayTime;
		else
			_descriptionObjectIndicatorShowRemainTime = 0.001f;

		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false && StageManager.instance.playStage != TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter).maxStage)
			floorText.text = (StageManager.instance.playStage + 1).ToString();
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

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	float _descriptionObjectIndicatorShowRemainTime;
	float _energyGaugeShowRemainTime;
	void Update()
	{
		// 타이틀 캔버스와 상관없이 에너지 게이지를 띄워야한다면 시간 체크 후 띄운다.
		if (_energyGaugeShowRemainTime > 0.0f)
		{
			_energyGaugeShowRemainTime -= Time.deltaTime;
			if (_energyGaugeShowRemainTime <= 0.0f)
			{
				_energyGaugeShowRemainTime = 0.0f;
				AddressableAssetLoadManager.GetAddressableGameObject("EnergyGaugeCanvas", "Object", (prefab) =>
				{
					BattleInstanceManager.instance.GetCachedObject(prefab, null);
				});
				return;
			}
		}

		if (TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf && TitleCanvas.instance.maskObject.activeSelf)
			return;

		// 설명 인디케이터는 타이틀 있을 경우엔 검은화면 지나가고 시간 재는게 맞다.
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
	}

	void OnCollisionEnter(Collision collision)
	{
		if (_processing)
			return;

		foreach (ContactPoint contact in collision.contacts)
		{
			if (CheckCollider(contact.otherCollider) == false)
				continue;
			if (CheckNextMap() == false)
				continue;

			Timing.RunCoroutine(NextMapProcess());
			break;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (_processing)
			return;
		if (CheckCollider(other) == false)
			return;
		if (CheckNextMap() == false)
			return;

		Timing.RunCoroutine(NextMapProcess());
	}

	bool CheckCollider(Collider collider)
	{
		if (collider == null)
			return false;
		HitObject hitObject = BattleInstanceManager.instance.GetHitObjectFromCollider(collider);
		// _disableSelfObjectAfterCollision 켜지는 HitObject들은 하필 게이트 필라에 다다르는 순간 바로 꺼져버린다.
		// 풀링에서 빠져버리기 때문에 위 함수로 얻어올 수 없게 되는데
		// 그렇다고 hitObject 컬리더 함수에서 직접 하기엔 괜히 로직만 복잡해지고
		// 그렇다고 한프레임 살려두다 죽이는것도 지저분하다.
		// 그래서 어차피 성능에 영향 심하게 주는 함수도 아니고, 몹 다 잡고난 다음이니 GetComponent 호출하게 한다.
		if (hitObject == null)
			hitObject = collider.GetComponent<HitObject>();
		if (hitObject == null)
			return false;
		if (hitObject.statusStructForHitObject.teamId == (int)Team.eTeamID.DefaultMonster)
			return false;
		if (hitObject.GetGatePillarCompareTime() < _spawnTime)
			return false;
		if (SwapCanvas.instance != null && SwapCanvas.instance.gameObject.activeSelf)
			return false;
		return true;
	}

	public void CheckHitObject(int teamId, float gatePillarCompareTime, Collider collider)
	{
		if (collider.gameObject != meshColliderObject)
			return;
		if (teamId == (int)Team.eTeamID.DefaultMonster)
			return;
		if (gatePillarCompareTime < _spawnTime)
			return;
		if (CheckNextMap() == false)
			return;

		Timing.RunCoroutine(NextMapProcess());
	}

	bool CheckNextMap()
	{
		if (SwapCanvas.instance != null && SwapCanvas.instance.gameObject.activeSelf)
			return false;

		if (MainSceneBuilder.instance.lobby)
		{
			// check lobby energy

			// check lobby suggest
			ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
			if (OptionManager.instance.suggestedChapter != StageManager.instance.playChapter && chapterTableData != null)
			{
				CharacterData mainCharacterData = PlayerData.instance.GetCharacterData(PlayerData.instance.mainCharacterId);
				bool showSwapCanvas = false;
				if (mainCharacterData.powerLevel < chapterTableData.suggestedPowerLevel)
					showSwapCanvas = true;
				if (showSwapCanvas)
				{
					FullscreenYesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_EnterInfo"), UIString.instance.GetString("GameUI_EnterInfoDesc"), () => {
						OptionManager.instance.suggestedChapter = StageManager.instance.playChapter;
						FullscreenYesNoCanvas.instance.ShowCanvas(false, "", "", null);
						UIInstanceManager.instance.ShowCanvasAsync("SwapCanvas", null);
					}, () => {
						OptionManager.instance.suggestedChapter = StageManager.instance.playChapter;
					});
					if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
						DotMainMenuCanvas.instance.ToggleShow();
					return false;
				}
			}
		}

		return true;
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
			StageManager.instance.AddBattlePlayer(BattleInstanceManager.instance.playerActor.actorId);
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
