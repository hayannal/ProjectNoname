using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using MEC;

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

		// 디폴트값으로 체크해둔다.
		if (targetDetectType == HitObject.eTargetDetectType.Area)
			oneHitPerTarget = true;
		else if (targetDetectType == HitObject.eTargetDetectType.SphereCast)
			useHitStay = true;
	}
	#endif
	
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		// 애니메이터에 AnimatorSpeed 시그널이 존재할 경우 시간을 재는게 무의미해진다.
		OnSignal(animator, stateInfo, layerIndex);
	}

	protected override void InitializeHitObject(Transform spawnTransform, MeHitObject meHit, Actor parentActor, Transform parentTransform, int hitSignalIndexInAction)
	{
		// Range에서는 만들어낸 Main HitObject를 기억해놨다가 직접 처리하는데 써야한다.
		_mainHitObject = HitObject.InitializeHit(spawnTransform, meHit, parentActor, parentTransform, hitSignalIndexInAction, 0);

		if (repeatCount > 0)
		{
			if (_listRepeatHitObject == null)
				_listRepeatHitObject = new List<HitObject>();
			_listRepeatHitObject.Clear();

			Timing.RunCoroutine(RepeatProcess(spawnTransform, meHit, parentActor, parentTransform, hitSignalIndexInAction));
		}
	}

	IEnumerator<float> RepeatProcess(Transform spawnTransform, MeHitObject meHit, Actor parentActor, Transform parentTransform, int hitSignalIndexInAction)
	{
		// Repeat 하기전 트랜스폼들을 복제해서 캐싱해야한다. 이래야 본 포지션 및 캐릭터 방향까지 기억할 수 있다.
		Transform duplicatedSpawnTransform = BattleInstanceManager.instance.GetEmptyTransform(spawnTransform.position, spawnTransform.rotation);
		Transform duplicatedParentTransform = BattleInstanceManager.instance.GetEmptyTransform(parentTransform.position, parentTransform.rotation);
		for (int i = 1; i <= meHit.repeatCount; ++i)
		{
			yield return Timing.WaitForSeconds(meHit.repeatInterval);

			// avoid gc
			if (this == null)
				yield break;

			HitObject repeatHitObject = HitObject.InitializeHit(duplicatedSpawnTransform, meHit, parentActor, duplicatedParentTransform, hitSignalIndexInAction, i);
			_listRepeatHitObject.Add(repeatHitObject);
		}
		duplicatedSpawnTransform.gameObject.SetActive(false);
		duplicatedParentTransform.gameObject.SetActive(false);
	}

	HitObject _mainHitObject;
	List<HitObject> _listRepeatHitObject;
	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_mainHitObject != null)
		{
			_mainHitObject.OnFinalizeByLifeTime();
			_mainHitObject = null;
		}

		if (repeatCount > 0)
		{
			Timing.RunCoroutine(RepeatEndProcess());
		}
	}

	IEnumerator<float> RepeatEndProcess()
	{
		for (int i = 1; i <= repeatCount; ++i)
		{
			yield return Timing.WaitForSeconds(repeatInterval);

			// avoid gc
			if (this == null)
				yield break;

			int index = i - 1;
			if (index < _listRepeatHitObject.Count && _listRepeatHitObject[index] != null)
				_listRepeatHitObject[index].OnFinalizeByLifeTime();
		}
		_listRepeatHitObject.Clear();
	}

	override public void OnRangeSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_mainHitObject != null)
			_mainHitObject.UpdateAreaOrSphereCast();

		if (_listRepeatHitObject != null)
		{
			for (int i = 0; i < _listRepeatHitObject.Count; ++i)
			{
				if (_listRepeatHitObject[i] != null)
					_listRepeatHitObject[i].UpdateAreaOrSphereCast();
			}
		}
	}
}