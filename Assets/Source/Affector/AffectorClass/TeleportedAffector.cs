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
	public bool bossMonster { get; private set; }
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

		bossMonster = false;
		MonsterActor monsterActor = null;
		if (_actor.IsMonsterActor())
			monsterActor = _actor as MonsterActor;
		if (monsterActor != null && monsterActor.bossMonster)
			bossMonster = true;

		_actor.EnableAI(false);
		_actor.actionController.idleAnimator.enabled = false;
		HitObject.EnableRigidbodyAndCollider(false, _actor.GetRigidbody(), _actor.GetCollider());
		_actor.baseCharacterController.movement.useGravity = false;
		_prevPosition = _actor.cachedTransform.position;
		_actor.cachedTransform.position = new Vector3(_actor.cachedTransform.position.x, TeleportedHeight, _actor.cachedTransform.position.z);
		BattleInstanceManager.instance.AddTeleportedAffector(this);
		_applied = true;

		// 쿨타임 준비. 왜 이런게 필요한지는 아래 읽어볼 것.
		PrepareCooltime();

		// 텔레포트와 Rush는 동시에 있기 어렵다. 무조건 떼어낸다.
		// 시간에 의해 없어지지 않는 Rush타입을 가지고 있는 상태에서 텔레포트 하고와고 되돌아오면
		// 이미 AI스탭은 다른거로 진행중인데 RushAffector가 적용될 수 있다. 그래서 지워주는거다.
		RushAffector rushAffector = (RushAffector)_actor.affectorProcessor.GetFirstContinuousAffector(eAffectorType.Rush);
		if (rushAffector != null) rushAffector.finalized = true;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	static string s_generatedId = "_generatedId_Teleported";
	public override void FinalizeAffector()
	{
		if (_applied == false)
			return;

		BattleInstanceManager.instance.RemoveTeleportedAffector(this);
		_actor.cachedTransform.position = _prevPosition;
		_actor.baseCharacterController.movement.useGravity = true;
		HitObject.EnableRigidbodyAndCollider(true, _actor.GetRigidbody(), _actor.GetCollider());
		_actor.actionController.idleAnimator.enabled = true;
		_actor.actionController.PlayActionByActionName("Idle");
		_actor.EnableAI(true);
		if (_actor.IsMonsterActor())
		{
			MonsterActor monsterActor = _actor as MonsterActor;
			if (monsterActor != null)
				monsterActor.monsterAI.OnFinalizeTeleportedAffector();
		}

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

		ApplyCooltime();
	}

	void PrepareCooltime()
	{
		// 우리 쿨타임에 약간 문제점이라고 할게 하나 있는데
		// 한번도 DefaultContainer이 적용되지 않는 affectorProcessor에 TeleportedAffector의 FinalizeAffector가 호출되면
		// affectorProcessor.UpdateAffectorProcessor 함수를 돌던 도중에 새 DefaultContainer가 추가되는거라
		// Enumerator가 무효하게 되버린다. 당연히 null처리에서 익셉션뜨고 이후 어펙터들의 Update가 호출되지 않게 되버린다.
		// 그래서 이걸 막기위해 가장 안전한 시점인
		// 텔레포트 어펙터가 추가될때 1초짜리 쿨타임으로 미리 넣어두기로 한다.
		//
		// 이전에는 쿨타임 추가되는 타이밍이 OnEvent쪽이라서 상관없었는데 이번에 처음으로 Update도중에 추가되는거라 이런 예외처리가 필요하게 되었다.
		AffectorValueLevelTableData affectorValueLevelTableData = new AffectorValueLevelTableData();
		affectorValueLevelTableData.affectorValueId = s_generatedId;
		affectorValueLevelTableData.fValue1 = 1.0f;
		affectorValueLevelTableData.sValue1 = eAffectorType.Teleported.ToString();
		_affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.DefaultContainer, affectorValueLevelTableData, _actor, false);
	}

	void ApplyCooltime()
	{
		// 쿨타임 등록
		AffectorValueLevelTableData affectorValueLevelTableData = new AffectorValueLevelTableData();
		// OverrideAffector가 제대로 호출되기 위해서 임시 아이디를 지정해줘야한다. 거의 오버라이드 될 일이 없겠지만 안전하게 해둔다.
		affectorValueLevelTableData.affectorValueId = s_generatedId;
		affectorValueLevelTableData.fValue1 = _affectorValueLevelTableData.fValue2;
		affectorValueLevelTableData.sValue1 = eAffectorType.Teleported.ToString();
		_affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.DefaultContainer, affectorValueLevelTableData, _actor, false);
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