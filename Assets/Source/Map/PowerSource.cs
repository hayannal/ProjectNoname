using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using ActorStatusDefine;

public class PowerSource : MonoBehaviour
{
	public static string Index2Address(int powerSource)
	{
		switch (powerSource)
		{
			case 0: return "PowerSourceMagic";
			case 1: return "PowerSourceMachine";
			case 2: return "PowerSourceNature";
			case 3: return "PowerSourceQigong";
		}
		return "";
	}

	public static string Index2Name(int powerSource)
	{
		switch (powerSource)
		{
			case 0: return UIString.instance.GetString("GameUI_Magic");
			case 1: return UIString.instance.GetString("GameUI_Machine");
			case 2: return UIString.instance.GetString("GameUI_Nature");
			case 3: return UIString.instance.GetString("GameUI_Qigong");
		}
		return "";
	}

	public static string Index2SmallName(int powerSource)
	{
		switch (powerSource)
		{
			case 0: return UIString.instance.GetString("GameUI_MagicSmall");
			case 1: return UIString.instance.GetString("GameUI_MachineSmall");
			case 2: return UIString.instance.GetString("GameUI_NatureSmall");
			case 3: return UIString.instance.GetString("GameUI_QigongSmall");
		}
		return "";
	}

	void OnEnable()
	{
		_spawnedGatePillar = false;

		if (MainSceneBuilder.s_buildReturnScrollUsedScene)
		{
			// 귀환중이라면 예외처리
			_spawnedGatePillar = true;
		}
		else if (ClientSaveData.instance.IsLoadingInProgressGame())
		{
			if (ClientSaveData.instance.GetCachedPowerSource())
				_spawnedGatePillar = true;
			else
				_guideMessageShowRemainTime = 5.0f;
		}
		else
		{
			ClientSaveData.instance.OnChangedPowerSource(false);
			_guideMessageShowRemainTime = 5.0f;
		}

		// 귀환 주문서를 구입하지 않은 사람도 8챕터 가면 세이브 포인트가 보여야한다.
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.SecondDailyBox))
			_returnScrollPointShowRemainTime = 0.1f;
	}

	void OnDisable()
	{
		// 파워소스가 리턴스크롤 포인트를 만들기때문에 파워소스 없앨때 같이 없애줘야한다.
		if (_returnScrollObject != null)
		{
			_returnScrollObject.SetActive(false);
			_returnScrollObject = null;
		}
	}

	bool _spawnedGatePillar;
	void OnCollisionEnter(Collision collision)
	{
		if (_spawnedGatePillar)
			return;

		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;
			OnTriggerEnter(col);
			break;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (_spawnedGatePillar)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		// 힐을 적용하기 전에 최대 hp 상태인지 확인하고 보너스 팩을 준다.
		if (affectorProcessor.actor.actorStatus.GetHPRatio() >= 1.0f)
		{
			PlayerActor playerActor = affectorProcessor.actor as PlayerActor;
			if (playerActor != null)
			{
				LevelPackDataManager.instance.AddLevelPack(playerActor.actorId, "MaxHpPowerSource");
				playerActor.skillProcessor.AddLevelPack("MaxHpPowerSource", false, 0);
				ClientSaveData.instance.OnChangedLevelPackData(LevelPackDataManager.instance.GetCachedLevelPackData());
			}
			FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.MaxHpIncrease, affectorProcessor.actor);
		}

		AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
		healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("PowerSourceHeal");
		healAffectorValue.fValue3 += affectorProcessor.actor.actorStatus.GetValue(eActorStatus.PowerSourceHealAddRate);
		affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, affectorProcessor.actor, false);
		affectorProcessor.actor.actorStatus.AddSP(affectorProcessor.actor.actorStatus.GetValue(eActorStatus.MaxSp) * BattleInstanceManager.instance.GetCachedGlobalConstantFloat("PowerSourceSpHeal"));
		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.healEffectPrefab, affectorProcessor.actor.cachedTransform.position, Quaternion.identity, affectorProcessor.actor.cachedTransform);

		BattleManager.instance.OnClearStage();
		_spawnedGatePillar = true;

		ClientSaveData.instance.OnChangedPowerSource(true);
		ClientSaveData.instance.OnChangedHpRatio(affectorProcessor.actor.actorStatus.GetHPRatio());
		ClientSaveData.instance.OnChangedSpRatio(affectorProcessor.actor.actorStatus.GetSPRatio());

		QuestData.instance.OnQuestEvent(QuestData.eQuestClearType.PowerSource);

		Timing.RunCoroutine(ScreenHealEffectProcess());
	}

	IEnumerator<float> ScreenHealEffectProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 0.8f);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;

		if (_objectIndicatorCanvas != null)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_objectIndicatorCanvas = null;
		}

		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("PowerSourceUI_Heal"), 2.5f);
		FadeCanvas.instance.FadeIn(1.5f);
	}

	float _guideMessageShowRemainTime;
	float _returnScrollPointShowRemainTime;
	GameObject _returnScrollObject;
	void Update()
	{
		if (_returnScrollPointShowRemainTime > 0.0f)
		{
			_returnScrollPointShowRemainTime -= Time.deltaTime;
			if (_returnScrollPointShowRemainTime <= 0.0f)
			{
				_returnScrollPointShowRemainTime = 0.0f;

				AddressableAssetLoadManager.GetAddressableGameObject("ReturnScrollPoint", "Map", (prefab) =>
				{
					_returnScrollObject = BattleInstanceManager.instance.GetCachedObject(prefab, StageManager.instance.currentReturnScrollSpawnPosition, Quaternion.identity);
				});
			}
		}

		if (_spawnedGatePillar == true)
			return;

		if (_guideMessageShowRemainTime > 0.0f)
		{
			_guideMessageShowRemainTime -= Time.deltaTime;
			if (_guideMessageShowRemainTime <= 0.0f)
			{
				_guideMessageShowRemainTime = 0.0f;

				// 가이드 문구를 마인드 텍스트대신 인디케이터로 바꾼다.
				//BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("PowerSourceUI_ComeHere"), 4.0f);
				ShowIndicator();
			}
		}
	}

	ObjectIndicatorCanvas _objectIndicatorCanvas;
	void ShowIndicator()
	{
		AddressableAssetLoadManager.GetAddressableGameObject("PowerSourceIndicator", "Canvas", (prefab) =>
		{
			// 로딩하는 중간에 맵이동시 다음맵으로 넘어가서 인디케이터가 뜨는걸 방지. 이미 회복 받았으면 뜨지않게 방지.
			if (this == null) return;
			if (gameObject == null) return;
			if (gameObject.activeSelf == false) return;
			if (_spawnedGatePillar) return;

			_objectIndicatorCanvas = UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(prefab);
			_objectIndicatorCanvas.targetTransform = transform;
		});
	}
}
