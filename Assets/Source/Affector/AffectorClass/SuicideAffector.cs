using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SuicideAffector : AffectorBase
{
	float _endTime;

	Renderer _renderer;
	Renderer[] _rendererList;
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
		_affectorValueLevelTableData = affectorValueLevelTableData;

		_actor.actorStatus.SetHpRatio(0.0f);
		_actor.OnDie();
		_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue1), 0.05f);

		// 렌더러 On/Off
		if (affectorValueLevelTableData.iValue1 == 1)
		{
			_renderer = _actor.GetComponentInChildren<Renderer>();
			_renderer.enabled = false;
		}
		else if (affectorValueLevelTableData.iValue1 == 2)
		{
			if (_rendererList == null)
				_rendererList = _actor.GetComponentsInChildren<Renderer>();

			for (int i = 0; i < _rendererList.Length; ++i)
				_rendererList[i].enabled = false;
		}

		// lifeTime
		//_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	// 인자로는 받지만 어차피 Die처리가 끝날때까지 없앨 이유가 없으므로 처리하진 않는다.
	// 오히려 DieProcess쪽으로 넘겨서 기본 DieTime(1.2초)를 늦추는데 사용된다.
	//public override void UpdateAffector()
	//{
	//	if (CheckEndTime(_endTime) == false)
	//		return;
	//}

	public static bool CheckSuicide(AffectorProcessor affectorProcessor, ref float suicideLifeTime, ref bool needRestoreSuicide)
	{
		SuicideAffector suicideAffector = (SuicideAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Suicide);
		if (suicideAffector == null)
			return false;

		suicideLifeTime = suicideAffector._affectorValueLevelTableData.fValue1;
		needRestoreSuicide = (suicideAffector._affectorValueLevelTableData.iValue1 > 0);
		return true;
	}

	public static void RestoreRenderer(AffectorProcessor affectorProcessor)
	{
		SuicideAffector suicideAffector = (SuicideAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Suicide);
		if (suicideAffector == null)
			return;

		// 꺼둔 렌더러를 복구해야 다음에 켜질때 켜져있는채로 나오게 된다.
		if (suicideAffector._renderer != null)
			suicideAffector._renderer.enabled = true;
		else if (suicideAffector._rendererList != null)
		{
			for (int i = 0; i < suicideAffector._rendererList.Length; ++i)
				suicideAffector._rendererList[i].enabled = true;
		}
	}
}