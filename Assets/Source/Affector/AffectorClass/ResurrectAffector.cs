using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResurrectAffector : AffectorBase
{
	float _endTime;
	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_affectorValueLevelTableData = affectorValueLevelTableData;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// 들어올 일이 없을거 같다.
		//Debug.Break();
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public override void SendInfo(string arg)
	{
		if (arg == "resurrectFinish")
		{
			if (_processing)
			{
				_actor.actorStatus.SetHpRatio(1.0f);
				_actor.actionController.idleAnimator.enabled = true;
				_actor.actionController.PlayActionByActionName("Idle");
				_actor.EnableAI(true);
				finalized = true;
			}
		}
	}

	bool _processing = false;
	bool CheckResurrect()
	{
		// 죽어있는 적을 또 죽일일은 없을거 같지만 혹시 중복 호출되면 리턴
		if (_processing)
			return true;

		if (_actor.actorStatus.IsDie() == false)
			return false;

		_actor.EnableAI(false);
		_actor.actionController.idleAnimator.enabled = false;
		_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue1), 0.05f);
		_processing = true;
		return true;
	}

	public static bool CheckResurrect(AffectorProcessor affectorProcessor)
	{
		ResurrectAffector resurrectAffector = (ResurrectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Resurrect);
		if (resurrectAffector == null)
			return false;
		return resurrectAffector.CheckResurrect();
	}

	public static bool IsProcessingResurrect(AffectorProcessor affectorProcessor)
	{
		ResurrectAffector resurrectAffector = (ResurrectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Resurrect);
		if (resurrectAffector == null)
			return false;
		return resurrectAffector._processing;
	}
}