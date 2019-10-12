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

		// 무적 검사를 가장 먼저.
		//if (InvincibleAffector.CheckInvincible(_affectorProcessor))
		//	return;

		// 횟수 보호막 검사가 그 다음.
		if (CountBarrierAffector.CheckBarrier(_affectorProcessor))
			return;

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

		bool useOnkill = (affectorValueLevelTableData.iValue2 == 1 && !string.IsNullOrEmpty(affectorValueLevelTableData.sValue2) && !_actor.actorStatus.IsDie());
		if (useOnkill && _actor.actorStatus.IsDie())
			_affectorProcessor.ApplyAffectorValue(affectorValueLevelTableData.sValue2, hitParameter, false);

		//Collider col = m_Actor.GetComponent<Collider>();
		//DamageFloaterManager.Instance.ShowDamage(intDamage, m_Actor.transform.position + new Vector3(0.0f, ColliderUtil.GetHeight(col), 0.0f));

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
