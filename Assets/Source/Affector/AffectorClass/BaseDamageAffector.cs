using UnityEngine;
using System.Collections;
using ActorStatusDefine;

public class BaseDamageAffector : AffectorBase {

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
		{
			// something else? for breakable object
			ShowHitBlink(hitParameter);
			return;
		}

		// 실명은 공격자꺼라 가장 먼저.
		// 하단의 빗맞음과 같이 처리하지 않고 최상위에서 먼저 돌린다.
		// 결과는 동일하게 Miss로 표현한다.

		// 횟수 보호막 검사가 비쥬얼상 무적이나 회피보다 먼저다.
		if (CountBarrierAffector.CheckBarrier(_affectorProcessor, hitParameter))
			return;

		// 무적 검사를 그다음
		if (InvincibleAffector.CheckInvincible(_affectorProcessor))
		{
			if (_actor.GetRigidbody() != null)
				_actor.GetRigidbody().velocity = Vector3.zero;
			FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Invincible, _actor);
			return;
		}

		// 회피 체크. IgnoreEvadeVisualAffector와 달리 실제로 빗맞음 체크하는 항목이다.
		// 저 어펙터가 없어도 이게 1이면 빗맞음 무시이며, %가 차오르지 않는 빗맞음 무시 필살기 공격을 만들때 쓸 예정이다.
		if (affectorValueLevelTableData.iValue3 == 0)
		{
			float evadeRate = _actor.actorStatus.GetValue(eActorStatus.EvadeRate);
			if (evadeRate > 0.0f && Random.value <= evadeRate)
			{
				// 안밀리게. 이 코드가 가장 간결하다.
				if (_actor.GetRigidbody() != null)
					_actor.GetRigidbody().velocity = Vector3.zero;

				// 미스 데미지 플로터 적용.
				FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Miss, _actor);
				return;
			}
		}

		//float damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack] * 1000.0f / (_actor.actorStatus.GetValue(eActorStatus.Defense) + 1000.0f);
		//float damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack] - _actor.actorStatus.GetValue(eActorStatus.Defense);
		float damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack];
		switch (affectorValueLevelTableData.iValue1)
		{
			case 0:
				damage *= affectorValueLevelTableData.fValue1;
				break;
			case 1:
				int hitSignalIndexInAction = hitParameter.statusStructForHitObject.hitSignalIndexInAction;
				float[] damageRatioList = BattleInstanceManager.instance.GetCachedMultiHitDamageRatioList(affectorValueLevelTableData.sValue1);
				if (hitSignalIndexInAction < damageRatioList.Length)
					damage *= damageRatioList[hitSignalIndexInAction];
				else
					Debug.LogErrorFormat("Invalid hitSignalIndexInAction. index = {0}", hitSignalIndexInAction);
				break;
		}

		if ((int)eActorStatus.CriticalRate < hitParameter.statusBase.valueList.Length)
		{
			float criticalRate = hitParameter.statusBase.valueList[(int)eActorStatus.CriticalRate];
			if (criticalRate > 0.0f && Random.value <= criticalRate)
			{
				float criticalDamageRate = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("DefaultCriticalDamageRate");
				criticalDamageRate += hitParameter.statusBase.valueList[(int)eActorStatus.CriticalDamageAddRate];
				damage *= (1.0f + criticalDamageRate);
			}
		}

		if (_actor is MonsterActor && (int)eActorStatus.NormalMonsterDamageIncreaseAddRate < hitParameter.statusBase.valueList.Length)
		{
			MonsterActor monsterActor = _actor as MonsterActor;
			if (monsterActor != null)
			{
				float damageIncreaseAddRate = hitParameter.statusBase.valueList[monsterActor.bossMonster ? (int)eActorStatus.BossMonsterDamageIncreaseAddRate : (int)eActorStatus.NormalMonsterDamageIncreaseAddRate];
				if (damageIncreaseAddRate != 0.0f)
					damage *= (1.0f + damageIncreaseAddRate);
			}
		}

		if (_actor is PlayerActor && hitParameter.statusStructForHitObject.monsterActor)
		{
			float damageDecreaseAddRate = _actor.actorStatus.GetValue(hitParameter.statusStructForHitObject.bossMonsterActor ? eActorStatus.BossMonsterDamageDecreaseAddRate : eActorStatus.NormalMonsterDamageDecreaseAddRate);
			if (damageDecreaseAddRate != 0.0f)
				damage *= (1.0f - damageDecreaseAddRate);
		}

		// 버로우로 내려가있는 도중엔 본체에 HitRimBlink 할 필요 없다.
		// DieProcess 들어가기전에 물어보는게 가장 정확하다.
		// DieProcess 진행중엔 Burrow처럼 액터를 바로 끄는 경우가 있어서 ContinuousAffector가 클리어될 수 있기 때문.
		bool showHitBlink = true;
		if (BurrowAffector.CheckBurrow(_affectorProcessor)) showHitBlink = false;

		int intDamage = (int)damage;
		_actor.actorStatus.AddHP(-intDamage);
		ChangeActorStatusAffector.OnDamage(_affectorProcessor);
		CallAffectorValueAffector.OnEvent(_affectorProcessor, CallAffectorValueAffector.eEventType.OnDamage);

#if UNITY_EDITOR
		//Debug.LogFormat("Current = {0} / Max = {1} / Damage = {2} / frameCount = {3}", _actor.actorStatus.GetHP(), _actor.actorStatus.GetValue(eActorStatus.MaxHp), intDamage, Time.frameCount);
#endif

		bool useOnkill = (affectorValueLevelTableData.iValue2 == 1 && !string.IsNullOrEmpty(affectorValueLevelTableData.sValue2) && !_actor.actorStatus.IsDie());
		if (useOnkill && _actor.actorStatus.IsDie())
			_affectorProcessor.ApplyAffectorValue(affectorValueLevelTableData.sValue2, hitParameter, false);

		//Collider col = m_Actor.GetComponent<Collider>();
		//DamageFloaterManager.Instance.ShowDamage(intDamage, m_Actor.transform.position + new Vector3(0.0f, ColliderUtil.GetHeight(col), 0.0f));

		if (!showHitBlink)
			return;

		ShowHitBlink(hitParameter);
	}

	void ShowHitBlink(HitParameter hitParameter)
	{
		if (hitParameter.statusStructForHitObject.showHitBlink)
			HitBlink.ShowHitBlink(_affectorProcessor.cachedTransform);
		if (hitParameter.statusStructForHitObject.showHitRimBlink)
			HitRimBlink.ShowHitRimBlink(_affectorProcessor.cachedTransform, hitParameter.contactNormal);
	}
}
