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

		// 회피 체크
		if (affectorValueLevelTableData.iValue3 == 0)
		{
			float evadeRate = _actor.actorStatus.GetValue(eActorStatus.Evade);
			if (evadeRate > 0.0f && Random.value <= evadeRate)
			{
				// 안밀리게. 이 코드가 가장 간결하다.
				if (_actor.GetRigidbody() != null)
					_actor.GetRigidbody().velocity = Vector3.zero;

				// 회피 데미지 플로터 적용.
				FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Evade, _actor);
				return;
			}
		}

		//float damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack] * 1000.0f / (_actor.actorStatus.GetValue(eActorStatus.Defense) + 1000.0f);
		float damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack] - _actor.actorStatus.GetValue(eActorStatus.Defense);
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

		int intDamage = (int)damage;
		_actor.actorStatus.AddHP(-intDamage);
		CallAffectorValueAffector.OnEvent(_affectorProcessor, CallAffectorValueAffector.eEventType.OnDamage);

#if UNITY_EDITOR
		//Debug.LogFormat("Current = {0} / Max = {1} / Damage = {2} / frameCount = {3}", _actor.actorStatus.GetHP(), _actor.actorStatus.GetValue(eActorStatus.MaxHP), intDamage, Time.frameCount);
#endif

		bool useOnkill = (affectorValueLevelTableData.iValue2 == 1 && !string.IsNullOrEmpty(affectorValueLevelTableData.sValue2) && !_actor.actorStatus.IsDie());
		if (useOnkill && _actor.actorStatus.IsDie())
			_affectorProcessor.ApplyAffectorValue(affectorValueLevelTableData.sValue2, hitParameter, false);

		//Collider col = m_Actor.GetComponent<Collider>();
		//DamageFloaterManager.Instance.ShowDamage(intDamage, m_Actor.transform.position + new Vector3(0.0f, ColliderUtil.GetHeight(col), 0.0f));

		// 버로우로 내려가있는 도중엔 본체에 HitRimBlink 할 필요 없다.
		if (BurrowAffector.CheckBurrow(_affectorProcessor))
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
