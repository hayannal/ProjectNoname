using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class Trap : MonoBehaviour
{
	public float multiAtk;
	public float hitStayInterval;
	public int hitStayIdForIgnoreDuplicate = 99;
	//public string[] affectorValueIdList;
	const int _tempActorInstanceId = 99;

	void Start()
	{
		Team.SetTeamLayer(gameObject, Team.eTeamLayer.TEAM1_HITOBJECT_LAYER);
	}

	StatusBase _statusBase = null;
	Dictionary<AffectorProcessor, float> _dicHitStayTime = null;
	void OnTriggerStay(Collider other)
	{
		if (other.isTrigger)
			return;

		Collider col = other;
		if (col == null)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
		if (affectorProcessor == null)
			return;

		if (affectorProcessor.actor.IsPlayerActor() == false)
			return;
		PlayerActor playerActor = affectorProcessor.actor as PlayerActor;
		if (playerActor == null)
			return;
		if (playerActor.flying)
			return;

		// check Levitation Character

		if (_dicHitStayTime == null)
			_dicHitStayTime = new Dictionary<AffectorProcessor, float>();

		if (affectorProcessor.CheckHitStayInterval(hitStayIdForIgnoreDuplicate, hitStayInterval, _tempActorInstanceId))
		{
			if (_statusBase == null)
				_statusBase = new StatusBase();
			_statusBase.valueList[(int)eActorStatus.Attack] = StageManager.instance.currentStageTableData.standardAtk;

			Vector3 contactPoint = affectorProcessor.cachedTransform.position;
			Vector3 contactNormal = affectorProcessor.cachedTransform.forward;

			HitParameter hitParameter = new HitParameter();
			hitParameter.hitNormal = transform.forward;
			hitParameter.contactNormal = contactNormal;
			hitParameter.contactPoint = contactPoint;
			hitParameter.statusBase = _statusBase;
			hitParameter.statusStructForHitObject.teamId = (int)Team.eTeamID.DefaultMonster;

			eAffectorType affectorType = eAffectorType.CollisionDamage;
			AffectorValueLevelTableData collisionDamageAffectorValue = new AffectorValueLevelTableData();
			collisionDamageAffectorValue.fValue1 = multiAtk;
			affectorProcessor.ExecuteAffectorValueWithoutTable(affectorType, collisionDamageAffectorValue, hitParameter, false);

			//if (meHit.showHitEffect)
			//	HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, hitParameter.statusStructForHitObject.weaponIDAtCreation);
		}
	}
}
