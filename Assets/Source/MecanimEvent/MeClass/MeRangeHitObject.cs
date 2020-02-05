using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using MEC;
using MecanimStateDefine;
using ActorStatusDefine;

public class MeRangeHitObject : MeHitObject
{
	override public bool RangeSignal { get { return true; } }
	
	#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		EditorGUILayout.LabelField("[Only available for Area or SphereCast]", EditorStyles.boldLabel);
		base.OnGUI_PropertyWindow();
		if (targetDetectType != HitObject.eTargetDetectType.Area && targetDetectType != HitObject.eTargetDetectType.SphereCast)
			targetDetectType = HitObject.eTargetDetectType.Area;
		lifeTime = 0.0f;
	}
	#endif
	
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		// 애니메이터에 AnimatorSpeed 시그널이 존재할 경우 시간을 재는게 무의미해진다.
		OnSignal(animator, stateInfo, layerIndex);
	}

	Transform _cachedSpawnTransform;
	Actor _cachedParentActor;
	Transform _cachedParentTransform;
	int _cachedHitSignalIndexInAction;
	protected override void InitializeHitObject(Transform spawnTransform, MeHitObject meHit, Actor parentActor, Transform parentTransform, StatusBase statusBase, float parentHitObjectCreateTime, int hitSignalIndexInAction)
	{
		// Range에서는 만들어낸 Main HitObject를 기억해놨다가 직접 처리하는데 써야한다.
		_mainHitObject = HitObject.InitializeHit(spawnTransform, meHit, parentActor, parentTransform, statusBase, parentHitObjectCreateTime, hitSignalIndexInAction, 0, 0);
		if (_mainHitObject != null)
		{
			_cachedSpawnTransform = spawnTransform;
			_cachedParentActor = parentActor;
			_cachedParentTransform = parentTransform;
			_cachedHitSignalIndexInAction = hitSignalIndexInAction;
		}

		bool normalAttack = parentActor.actionController.mecanimState.IsState((int)eMecanimState.Attack);
		int repeatAddCountByLevelPack = normalAttack ? RepeatHitObjectAffector.GetAddCount(parentActor.affectorProcessor) : 0;
		_totalRepeatCount = repeatCount + repeatAddCountByLevelPack;
		if (_totalRepeatCount > 0)
		{
			if (_listRepeatHitObject == null)
				_listRepeatHitObject = new List<HitObject>();
			_listRepeatHitObject.Clear();

			_resultRepeatInterval = meHit.repeatInterval;
			if (_resultRepeatInterval == 0.0f && repeatAddCountByLevelPack > 0) _resultRepeatInterval = RepeatHitObjectAffector.GetInterval(parentActor.affectorProcessor);
			Timing.RunCoroutine(RepeatProcess(spawnTransform, meHit, parentActor, parentTransform, statusBase, parentHitObjectCreateTime, hitSignalIndexInAction));
		}
	}

	IEnumerator<float> RepeatProcess(Transform spawnTransform, MeHitObject meHit, Actor parentActor, Transform parentTransform, StatusBase statusBase, float parentHitObjectCreateTime, int hitSignalIndexInAction)
	{
		// Repeat 하기전 트랜스폼들을 복제해서 캐싱해야한다. 이래야 본 포지션 및 캐릭터 방향까지 기억할 수 있다.
		Transform duplicatedSpawnTransform = BattleInstanceManager.instance.GetEmptyTransform(spawnTransform.position, spawnTransform.rotation);
		Transform duplicatedParentTransform = BattleInstanceManager.instance.GetEmptyTransform(parentTransform.position, parentTransform.rotation);
		for (int i = 1; i <= _totalRepeatCount; ++i)
		{
			yield return Timing.WaitForSeconds(_resultRepeatInterval);

			// avoid gc
			if (this == null)
				yield break;

			HitObject repeatHitObject = HitObject.InitializeHit(duplicatedSpawnTransform, meHit, parentActor, duplicatedParentTransform, statusBase, parentHitObjectCreateTime, hitSignalIndexInAction, i, _totalRepeatCount - meHit.repeatCount);
			_listRepeatHitObject.Add(repeatHitObject);
		}
		duplicatedSpawnTransform.gameObject.SetActive(false);
		duplicatedParentTransform.gameObject.SetActive(false);
	}

	HitObject _mainHitObject;
	List<HitObject> _listRepeatHitObject;
	int _totalRepeatCount;
	float _resultRepeatInterval;
	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_mainHitObject != null)
		{
			_mainHitObject.OnFinalizeByRangeSignal();
			_mainHitObject = null;
		}

		if (_totalRepeatCount > 0)
		{
			Timing.RunCoroutine(RepeatEndProcess());
		}
	}

	IEnumerator<float> RepeatEndProcess()
	{
		for (int i = 1; i <= _totalRepeatCount; ++i)
		{
			yield return Timing.WaitForSeconds(_resultRepeatInterval);

			// avoid gc
			if (this == null)
				yield break;

			int index = i - 1;
			if (index < _listRepeatHitObject.Count && _listRepeatHitObject[index] != null)
				_listRepeatHitObject[index].OnFinalizeByRangeSignal();
		}
		_listRepeatHitObject.Clear();
	}

	override public void OnRangeSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_mainHitObject != null)
		{
			// RangeHit는 애니에 맞춰서 움직이는 근접 공격에 쓰려고 한거라 위치와 각도를 매프레임 갱신해줘야한다.
			// 원래라면 Repeat쪽도 해야하는데 Repeat쪽은 거의 쓰지 않을 가능성이 높고
			// 처리한다해도 애니를 다시 재생시키면서 포지션을 시뮬레이션 해야하는데 로직이 복잡해서 우선 패스하기로 한다.
			Vector3 areaPosition = HitObject.GetSpawnPosition(_cachedSpawnTransform, this, _cachedParentTransform, _cachedParentActor, _cachedHitSignalIndexInAction);
			Vector3 areaDirection = _cachedSpawnTransform.forward;
			_mainHitObject.cachedTransform.position = areaPosition;
			_mainHitObject.cachedTransform.forward = areaDirection;
			_mainHitObject.UpdateAreaOrSphereCast();
		}

		if (_totalRepeatCount > 0)
		{
			Timing.RunCoroutine(RepeatRangeSignalProcess());
		}
	}

	IEnumerator<float> RepeatRangeSignalProcess()
	{
		for (int i = 1; i <= _totalRepeatCount; ++i)
		{
			yield return Timing.WaitForSeconds(_resultRepeatInterval);

			// avoid gc
			if (this == null)
				yield break;

			int index = i - 1;
			if (index < _listRepeatHitObject.Count && _listRepeatHitObject[index] != null)
				_listRepeatHitObject[index].UpdateAreaOrSphereCast();
		}
	}
}