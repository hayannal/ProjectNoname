using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TeleportedAffector : AffectorBase
{
	float _endTime;
	Vector3 _prevPosition;

	const float TeleportedHeight = 500.0f;
	const float EffectOffetY = 0.8f;

	bool _applied = false;
	AffectorValueLevelTableData _affectorValueLevelTableData;
	GameObject _positionEffectObject;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
		{
			finalized = true;
			return;
		}

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_affectorValueLevelTableData = affectorValueLevelTableData;

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue2))
		{
			GameObject positionEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue2);
			if (positionEffectPrefab != null)
				_positionEffectObject = BattleInstanceManager.instance.GetCachedObject(positionEffectPrefab, _actor.cachedTransform.position, Quaternion.identity);
		}
		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue3))
		{
			GameObject onStartEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
			if (onStartEffectPrefab != null)
				BattleInstanceManager.instance.GetCachedObject(onStartEffectPrefab, _actor.cachedTransform.position + new Vector3(0.0f, EffectOffetY, 0.0f), _actor.cachedTransform.rotation);
		}

		_actor.EnableAI(false);
		_actor.actionController.idleAnimator.enabled = false;
		_actor.baseCharacterController.movement.useGravity = false;
		_prevPosition = _actor.cachedTransform.position;
		_actor.cachedTransform.position = new Vector3(_actor.cachedTransform.position.x, TeleportedHeight, _actor.cachedTransform.position.z);
		BattleInstanceManager.instance.AddTeleportedAffector(this);
		_applied = true;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public override void FinalizeAffector()
	{
		if (_applied == false)
			return;

		BattleInstanceManager.instance.RemoveTeleportedAffector(this);
		_actor.cachedTransform.position = _prevPosition;
		_actor.baseCharacterController.movement.useGravity = true;
		_actor.actionController.idleAnimator.enabled = true;
		_actor.actionController.PlayActionByActionName("Idle");
		_actor.EnableAI(true);

		if (_positionEffectObject != null)
		{
			_positionEffectObject.SetActive(false);
			_positionEffectObject = null;
		}
		if (!string.IsNullOrEmpty(_affectorValueLevelTableData.sValue4))
		{
			GameObject onEndEffectPrefab = FindPreloadObject(_affectorValueLevelTableData.sValue4);
			if (onEndEffectPrefab != null)
				BattleInstanceManager.instance.GetCachedObject(onEndEffectPrefab, _actor.cachedTransform.position + new Vector3(0.0f, EffectOffetY, 0.0f), _actor.cachedTransform.rotation);
		}
		_applied = false;
	}

	// 어펙터의 소멸자는 씬이 이동되어도 호출되지 않는다.(액터가 사라져도 호출되지 않는다.)
	// 그래서 OnDestroy처럼 쓸 수 없고
	// 스스로 아무 리스트에도 포함되지 않아야 지워지는데, 이러려면
	// static 리스트에도 들어있지 않은채로 finalized가 호출되서 continuousAffectorList에서 삭제되어야 한다.
	//
	// 판정하기 불편하므로 소멸자를 써서 컨트롤하는건 하지 않기로 한다.
	//~TeleportedAffector()
	//{
	//	Debug.Log("destroyed");
	//}

	// static을 쓰면서 씬 전환시 클리어 하는 방법이 하나 있긴한데
	// 바로 씬 이동 직전 코드에 static 으로 된 Clear함수를 만들어서 호출하는거다.
	// 근데 이 방법은 씬 이동코드에다가 자꾸 이상한 컨텐츠 클래스의 Clear를 호출해야한다는 점에서 지저분하다.
	//public static void Clear()

	// actor가 null인지를 검사해서 static 리스트에서 빼는 방법도 있으나
	// 이건 진짜 별로인 코드다.
	// 씬이동시 자동으로 초기화 되는 매니저안에 두는게 가장 깔끔하다.

	/*
	#region static
	static List<TeleportedAffector> s_listTeleportedAffector;
	public static void Push(TeleportedAffector teleportedAffector)
	{
		if (s_listTeleportedAffector == null)
			s_listTeleportedAffector = new List<TeleportedAffector>();

		s_listTeleportedAffector.Add(teleportedAffector);
	}

	public static void Pop(TeleportedAffector teleportedAffector)
	{
		if (s_listTeleportedAffector == null)
			return;

		s_listTeleportedAffector.Remove(teleportedAffector);
	}

	public static int GetActiveCount()
	{
		if (s_listTeleportedAffector == null)
			return 0;
		return s_listTeleportedAffector.Count;
	}

	public static void RestoreFirstObject()
	{
		if (s_listTeleportedAffector == null)
			return;

		if (s_listTeleportedAffector.Count > 0)
			s_listTeleportedAffector[0].finalized = true;
	}
	#endregion
	*/
}