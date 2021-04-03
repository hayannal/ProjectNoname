using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class MultiTargetAreaGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("MultiTargetAreaGenerator")]
	public float tempValue = 1.0f;

	List<MonsterActor> _listMultiTargetMonsterTemporary;

	// Update is called once per frame
	void Update()
	{
		if (CheckChangeState())
		{
			gameObject.SetActive(false);
			return;
		}

		if (_parentActor.targetingProcessor.GetTarget() == null)
		{
			// 타겟이 없을땐 Fallback 포지션에다가 날아갈테니 그냥 제네레이트 하면 될듯
			Generate(cachedTransform.position, cachedTransform.rotation, true);
		}
		else
		{
			if (_listMultiTargetMonsterTemporary == null)
				_listMultiTargetMonsterTemporary = new List<MonsterActor>();
			_listMultiTargetMonsterTemporary.Clear();

			// 타겟이 있을땐 우선 하나 찍고
			Collider mainTargetCollider = _parentActor.targetingProcessor.GetTarget();
			Generate(mainTargetCollider.transform.position, cachedTransform.rotation, true);

			// 나머지 생성개수에 대해서는 MultiTarget을 검출해서 존재하면 해당 위치에 찍어주는 형태로 한다.
			// TargetingProcessor.FindPresetMultiTargetMonsterList 함수 참고해서 만들었다.

			List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
			for (int i = 0; i < listMonsterActor.Count; ++i)
			{
				Collider monsterCollider = listMonsterActor[i].GetCollider();
				if (mainTargetCollider == monsterCollider)
					continue;

				// team check 굳이 할 필요 있을까 싶어서 패스
				//if (_teamComponent != null)
				//{
				//	if (!Team.CheckTeamFilter(_teamComponent.teamId, monsterCollider, Team.eTeamCheckFilter.Enemy, false))
				//		continue;
				//}

				// object radius
				float colliderRadius = ColliderUtil.GetRadius(monsterCollider);
				if (colliderRadius == -1.0f) continue;

				// 버로우나 고스트는 체크하지 않고 랜덤풀에 넣기로 한다.
				//if (IsOutOfRangePresetMultiTarget(_signal, listMonsterActor[i].affectorProcessor, checkBurrow, checkGhost))
				//	continue;

				// 대신 텔레포트 상태인지는 체크해야한다.
				if (TargetingProcessor.IsOutOfRange(listMonsterActor[i].affectorProcessor))
					continue;

				// distance
				//Vector3 diff = listMonsterActor[i].cachedTransform.position - position;
				//diff.y = 0.0f;
				//if (_playerAI.currentAttackRange > 0.0f)
				//{
				//	float distance = diff.magnitude - colliderRadius;
				//	if (distance > _playerAI.currentAttackRange)
				//		continue;
				//}

				// angle
				//float angle = Vector3.Angle(actor.cachedTransform.forward, diff.normalized);
				//float hypotenuse = Mathf.Sqrt(diff.sqrMagnitude + colliderRadius * colliderRadius);
				//float adjustAngle = Mathf.Rad2Deg * Mathf.Acos(diff.magnitude / hypotenuse);
				//if (cachedActorTableData.multiTargetAngle * 0.5f < angle - adjustAngle)
				//	continue;

				_listMultiTargetMonsterTemporary.Add(listMonsterActor[i]);
			}

			if (_listMultiTargetMonsterTemporary == null || _listMultiTargetMonsterTemporary.Count == 0)
			{
				gameObject.SetActive(false);
				return;
			}

			ObjectUtil.Shuffle<MonsterActor>(_listMultiTargetMonsterTemporary);

			for (int i = 0; i < _listMultiTargetMonsterTemporary.Count; ++i)
			{
				if (i >= createCount - 1)
					break;

				Generate(_listMultiTargetMonsterTemporary[i].cachedTransform.position, cachedTransform.rotation, true);
			}
		}

		gameObject.SetActive(false);
	}
}