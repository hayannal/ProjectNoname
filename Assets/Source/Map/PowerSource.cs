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

	void OnEnable()
	{
		_spawnedGatePillar = false;
		_guideMessageShowRemainTime = 5.0f;
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

		AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
		healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("PowerSourceHeal");
		healAffectorValue.fValue3 += affectorProcessor.actor.actorStatus.GetValue(eActorStatus.PowerSourceHealRate);
		affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, affectorProcessor.actor, false);
		affectorProcessor.actor.actorStatus.AddSP(affectorProcessor.actor.actorStatus.GetValue(eActorStatus.MaxSp) * BattleInstanceManager.instance.GetCachedGlobalConstantFloat("PowerSourceSpHeal"));
		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.healEffectPrefab, affectorProcessor.actor.cachedTransform.position, Quaternion.identity, affectorProcessor.actor.cachedTransform);

		BattleManager.instance.OnClearStage();
		_spawnedGatePillar = true;

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
	void Update()
	{
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
		AddressableAssetLoadManager.GetAddressableGameObject("PowerSourceIndicator", "Object", (prefab) =>
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
