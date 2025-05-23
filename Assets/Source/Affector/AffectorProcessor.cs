﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class AffectorProcessor : MonoBehaviour {

	Dictionary<int, AffectorBase> _dicAffector;
	Dictionary<int, List<AffectorBase>> _dicContinuousAffector;

	public Actor actor { get; private set; }
	public bool dontClearOnDisable { get; set; }

	void Awake()
	{
		actor = GetComponent<Actor>();
	}

	void OnDisable()
	{
		if (dontClearOnDisable)
		{
			// 몬스터도 갑자기 Disable된다면 DisableAffector 호출이 필요하긴 한데
			// 로직상 갑자기 Disable될 일이 없다. 우선 불필요한 로직이니 빼둔다.
			if (_dicContinuousAffector != null)
			{
				Dictionary<int, List<AffectorBase>>.Enumerator e = _dicContinuousAffector.GetEnumerator();
				while (e.MoveNext())
				{
					for (int i = 0; i < e.Current.Value.Count; ++i)
					{
						if (e.Current.Value[i].finalized)
							continue;
						e.Current.Value[i].DisableAffector();
					}
				}
			}
			return;
		}

		if (_dicContinuousAffector != null)
		{
			Dictionary<int, List<AffectorBase>>.Enumerator e = _dicContinuousAffector.GetEnumerator();
			while (e.MoveNext())
				e.Current.Value.Clear();
		}

		if (_dicActorStateInfo != null)
		{
			Dictionary<string, List<AffectorBase>>.Enumerator e = _dicActorStateInfo.GetEnumerator();
			while (e.MoveNext())
				e.Current.Value.Clear();
		}

		ClearHitStayInterval();
	}

	public int GetAffectorValueLevel(AffectorValueTableData data, int skillLevel)
	{
		int affectorValueLevel = 1;
		if (skillLevel != 0)
		{
			if (string.IsNullOrEmpty(data.skillLevel2AffectorLevel))
				affectorValueLevel = skillLevel;
			else
			{
				Dictionary<int, int> dicConvertData = BattleInstanceManager.instance.GetCachedSkillLevel2AffectorLevelData(data.skillLevel2AffectorLevel);
				if (dicConvertData != null)
				{
					if (dicConvertData.ContainsKey(skillLevel))
						affectorValueLevel = dicConvertData[skillLevel];
					else
					{
						Debug.LogErrorFormat("No SkillLevel in SkillLevel2AffectorLevel. AffectorValueId = {0} / SkillLevel = {1}", data.id, skillLevel);
						affectorValueLevel = skillLevel;
					}
				}
			}
		}
		return affectorValueLevel;
	}

	public AffectorBase ApplyAffectorValue(string affectorValueId, HitParameter hitParameter, bool managed = false, bool syncAffector = true)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null)
			return null;
		int affectorValueLevel = GetAffectorValueLevel(data, hitParameter.statusStructForHitObject.skillLevel);
		return ExcuteAffectorValue(data, affectorValueLevel, hitParameter, managed, syncAffector);
	}

	AffectorBase ExcuteAffectorValue(AffectorValueTableData data, int affectorValueLevel, HitParameter hitParameter, bool managed, bool syncAffector)
	{
		AffectorValueLevelTableData levelData = TableDataManager.instance.FindAffectorValueLevelTableData(data.id, affectorValueLevel);
		if (levelData == null)
		{
			Debug.LogErrorFormat("Not found AffectorValueLevelTableData. AffectorValueId = {0} / level = {1}", data.id, affectorValueLevel);
			return null;
		}

		return ExcuteAffector((eAffectorType)data.affectorId, levelData, hitParameter, managed);

		//#region Network Sync
		//if (syncAffector)
		//	SyncAffectorInfo(data.id, level, hitParameter);
		//#endregion
	}

	AffectorBase ExcuteAffector(eAffectorType affectorType, AffectorValueLevelTableData levelData, HitParameter hitParameter, bool managed)
	{
		for (int i = 0; i < levelData.conditionValueId.Length; ++i)
		{
			if (string.IsNullOrEmpty(levelData.conditionValueId[i]))
				continue;
			if (Condition.CheckCondition(levelData.conditionValueId[i], hitParameter, this, actor) == false)
				return null;
		}

		AffectorBase affectorBase = null;
		if (AffectorCustomCreator.IsContinuousAffector(affectorType))
		{
			if (_dicContinuousAffector == null) _dicContinuousAffector = new Dictionary<int, List<AffectorBase>>();

			List<AffectorBase> listContinuousAffector = null;
			if (_dicContinuousAffector.ContainsKey((int)affectorType))
				listContinuousAffector = _dicContinuousAffector[(int)affectorType];
			else
			{
				listContinuousAffector = new List<AffectorBase>();
				_dicContinuousAffector.Add((int)affectorType, listContinuousAffector);
			}

			AffectorBase existSameContinuousAffector = null;
			if (managed == false)
			{
				for (int i = 0; i < listContinuousAffector.Count; ++i)
				{
					if (listContinuousAffector[i].finalized || listContinuousAffector[i].managed)
						continue;

					if (listContinuousAffector[i].affectorValueId == levelData.affectorValueId)
					{
						existSameContinuousAffector = listContinuousAffector[i];
						affectorBase = existSameContinuousAffector;
						break;
					}
				}
			}

			if (existSameContinuousAffector != null)
			{
				existSameContinuousAffector.OverrideAffector(levelData, hitParameter);

				////////////////////////////////////////////////////////////////////////////
				// 이런식으로 레벨 비교해서 하려고 했는데 mmo구조로 갔을때든
				// 모든 상황을 고려해보면 레벨 비교에서도 로직상 풀 수 없는 문제들이 나온다.
				// 그럴바엔 레벨 검사 없이, 재생성이나 삭제 없이
				// 오버라이드로 처리하기로 한다.
				//if (existSameContinuousAffector.level < levelData.level)
				//{
				//	// do nothing
				//	affectorBase = existSameContinuousAffector;
				//}
				//else if (existSameContinuousAffector.level == levelData.level)
				//{
				//	existSameContinuousAffector.OverrideAffector(levelData, hitParameter);
				//}
				//else
				//{
				//	// finalize exist affector
				//	existSameContinuousAffector.finalized = true;
				//	// for create new affector
				//	existSameContinuousAffector = null;
				//}
			}
			else
			{
				// to update
				affectorBase = AffectorCustomCreator.CreateAffector(affectorType);
				if (affectorBase != null)
				{
					affectorBase.InitializeAffector(affectorType, actor, this);
					affectorBase.managed = managed;
					affectorBase.affectorValueId = levelData.affectorValueId;
					listContinuousAffector.Add(affectorBase);
					affectorBase.ExecuteAffector(levelData, hitParameter);
				}
			}
		}
		else
		{
			if (_dicAffector == null) _dicAffector = new Dictionary<int, AffectorBase>();
			if (_dicAffector.ContainsKey((int)affectorType)) affectorBase = _dicAffector[(int)affectorType];
			else
			{
				affectorBase = AffectorCustomCreator.CreateAffector(affectorType);
				if (affectorBase != null)
				{
					affectorBase.InitializeAffector(affectorType, actor, this);
					_dicAffector.Add((int)affectorType, affectorBase);
				}
			}
			if (affectorBase != null)
			{
				affectorBase.affectorValueId = levelData.affectorValueId;
				affectorBase.ExecuteAffector(levelData, hitParameter);
			}
		}
		return affectorBase;
	}

	#region Without Table
	public AffectorBase ExecuteAffectorValueWithoutTable(eAffectorType affectorType, AffectorValueLevelTableData levelData, Actor actor, bool managed)
	{
		HitParameter hitParameter = new HitParameter();
		if (actor != null)
		{
			hitParameter.statusBase = new StatusBase();
			actor.actorStatus.CopyStatusBase(ref hitParameter.statusBase);
			SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, actor);
		}
		return ExcuteAffector(affectorType, levelData, hitParameter, managed);
	}
	#endregion

	public List<AffectorBase> GetContinuousAffectorList(eAffectorType affectorType)
	{
		if (_dicContinuousAffector == null)
			return null;
		if (_dicContinuousAffector.ContainsKey((int)affectorType) == false)
			return null;

		return _dicContinuousAffector[(int)affectorType];
	}

	public bool IsContinuousAffectorValueId(string affectorValueId)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null)
			return false;

		List<AffectorBase> listContinuousAffector = GetContinuousAffectorList((eAffectorType)data.affectorId);
		if (listContinuousAffector == null)
			return false;

		for (int i = 0; i < listContinuousAffector.Count; ++i)
		{
			if (listContinuousAffector[i].finalized)
				continue;
			if (listContinuousAffector[i].affectorValueId == affectorValueId)
				return true;
		}
		return false;
	}

	public bool IsContinuousAffectorType(eAffectorType affectorType)
	{
		List<AffectorBase> listContinuousAffector = GetContinuousAffectorList(affectorType);
		if (listContinuousAffector == null)
			return false;

		for (int i = 0; i < listContinuousAffector.Count; ++i)
		{
			if (listContinuousAffector[i].finalized == false)
				return true;
		}
		return false;
	}

	// 실제 처리에서는 발견되는 가장 최초 ContinuousAffector를 가져와서 처리할때가 많다. 그래서 공용함수로 만들어둔다.
	public AffectorBase GetFirstContinuousAffector(eAffectorType affectorType)
	{
		List<AffectorBase> listContinuousAffector = GetContinuousAffectorList(affectorType);
		if (listContinuousAffector == null)
			return null;

		for (int i = 0; i < listContinuousAffector.Count; ++i)
		{
			if (listContinuousAffector[i].finalized == false)
				return listContinuousAffector[i];
		}
		return null;
	}

	void Update()
	{
		UpdateAffectorProcessor();
		UpdateActorState();
	}

	void FixedUpdate()
	{
		FixedUpdateAffectorProcessor();
	}

	void UpdateAffectorProcessor()
	{
		if (_dicContinuousAffector == null)
			return;

		Dictionary<int, List<AffectorBase>>.Enumerator e = _dicContinuousAffector.GetEnumerator();
		while (e.MoveNext())
		{
			for (int i = 0; i < e.Current.Value.Count; ++i)
			{
				if (e.Current.Value[i].finalized)
					continue;
				e.Current.Value[i].UpdateAffector();
			}
		}

		e = _dicContinuousAffector.GetEnumerator();
		while (e.MoveNext())
		{
			for (int i = e.Current.Value.Count - 1; i >= 0; --i)
			{
				if (e.Current.Value[i].finalized)
					e.Current.Value.RemoveAt(i);
			}
		}
	}

	void FixedUpdateAffectorProcessor()
	{
		if (_dicContinuousAffector == null)
			return;

		Dictionary<int, List<AffectorBase>>.Enumerator e = _dicContinuousAffector.GetEnumerator();
		while (e.MoveNext())
		{
			for (int i = 0; i < e.Current.Value.Count; ++i)
			{
				if (e.Current.Value[i].finalized)
					continue;
				e.Current.Value[i].FixedUpdateAffector();
			}
		}
	}



	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}


	#region ActorState
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// 게임이라면 흔하게 쓰는 상태이상 및 버프 디버프 등을 묶음으로 관리하기 위해 만든 기능
	// 내부에선 결국 어펙터 프로세서처럼 어펙터를 돌린다는 점에서 어펙터 프로세서 옆에 두게 되었다.
	// 어펙터의 가장 큰 단점이 여러 컨티뉴어스 어펙터를 묶는 개념이 없어서 개별로 어펙터 처리를 해야한다는 것인데,
	// 그걸 극복하기 위해 추가된거다.
	// 프로세서를 분리하면 코드는 깔끔해지는데 버프 같은거 통합해서 계산해야할때 컨테이너 양쪽에서 받아와야하므로 불편해져서
	// 기존의 리스트에 두되 managed true로 해서 넣어두고 관리하기로 한다.

	Dictionary<string, List<AffectorBase>> _dicActorStateInfo;

	public void AddActorState(string actorStateId, HitParameter hitParameter)
	{
		ActorStateTableData data = TableDataManager.instance.FindActorStateTableData(actorStateId);
		if (data == null)
			return;

		if (_dicActorStateInfo == null)
			_dicActorStateInfo = new Dictionary<string, List<AffectorBase>>();

		string[] affectorValueIdList = data.continuousAffectorValueId;

		bool createAffector = false;
		List<AffectorBase> listAffector = null;
		if (_dicActorStateInfo.ContainsKey(actorStateId) == false)
		{
			createAffector = true;
			listAffector = new List<AffectorBase>();
			_dicActorStateInfo.Add(actorStateId, listAffector);
		}
		else
		{
			listAffector = _dicActorStateInfo[actorStateId];
			if (listAffector.Count == 0)
				createAffector = true;
			else if (listAffector.Count == affectorValueIdList.Length)
			{
				// override if one is active
				bool useOverride = false;
				for (int i = 0; i < listAffector.Count; ++i)
				{
					if (listAffector[i] == null)
						continue;
					if (listAffector[i].finalized == false)
						useOverride = true;
				}
				if (useOverride)
				{
					for (int i = 0; i < affectorValueIdList.Length; ++i)
					{
						if (listAffector[i] == null)
						{
							listAffector[i] = ApplyAffectorValue(affectorValueIdList[i], hitParameter, true);
							if (listAffector[i] == null)
								continue;

							if (AffectorCustomCreator.IsContinuousAffector(listAffector[i].affectorType) == false)
								Debug.LogErrorFormat("Non-continuous affector in a Actor State! / actorStateId = {0} / AffectorValueId = {1}", actorStateId, affectorValueIdList[i]);
						}
						else
						{
							AffectorValueLevelTableData levelData = GetAffectorValueLevelTableData(affectorValueIdList[i], hitParameter);
							listAffector[i].OverrideAffector(levelData, hitParameter);
						}
					}
				}
				else
				{
					listAffector.Clear();
					createAffector = true;
				}
			}
			else
			{
				Debug.LogErrorFormat("ActorState Affector Count is different from existing. ActorStateId = {0}", actorStateId);
				listAffector.Clear();
				createAffector = true;
			}
		}

		if (createAffector)
		{
			for (int i = 0; i < affectorValueIdList.Length; ++i)
			{
				AffectorBase newAffector = ApplyAffectorValue(affectorValueIdList[i], hitParameter, true);
				if (newAffector == null)
					continue;

				if (AffectorCustomCreator.IsContinuousAffector(newAffector.affectorType))
					listAffector.Add(newAffector);
				else
					Debug.LogErrorFormat("Non-continuous affector in a Actor State! / actorStateId = {0} / AffectorValueId = {1}", actorStateId, affectorValueIdList[i]);
			}
		}
	}

	AffectorValueLevelTableData GetAffectorValueLevelTableData(string affectorValueId, HitParameter hitParameter)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null)
			return null;

		int affectorValueLevel = GetAffectorValueLevel(data, hitParameter.statusStructForHitObject.skillLevel);
		AffectorValueLevelTableData levelData = TableDataManager.instance.FindAffectorValueLevelTableData(data.id, affectorValueLevel);
		if (levelData == null)
		{
			Debug.LogErrorFormat("Not found AffectorValueLevelTableData. AffectorValueId = {0} / level = {1}", data.id, affectorValueLevel);
			return null;
		}

		return levelData;
	}

	public bool IsActorState(string actorStateId)
	{
		if (_dicActorStateInfo == null)
			return false;

		if (_dicActorStateInfo.ContainsKey(actorStateId) == false)
			return false;

		List<AffectorBase> listExistAffector = _dicActorStateInfo[actorStateId];
		for (int i = 0; i < listExistAffector.Count; ++i)
		{
			if (listExistAffector[i] == null)
				continue;
			if (listExistAffector[i].finalized == false)
				return true;
		}
		return false;
	}

	void UpdateActorState()
	{
		if (_dicActorStateInfo == null)
			return;

		// _dicContinuousAffector 업데이트쪽에서 실제 UpdateAffector를 호출해주고 있으므로 여기서는 할 필요 없다.
		//Dictionary<string, List<AffectorBase>>.Enumerator e = _dicActorStateInfo.GetEnumerator();
		//while (e.MoveNext())
		//{
		//	for (int i = 0; i < e.Current.Value.Count; ++i)
		//	{
		//		if (e.Current.Value[i].finalized)
		//			continue;
		//		e.Current.Value[i].UpdateAffector();
		//	}
		//}

		// AffectorProcessor와 달리 finalized 된다고 바로 삭제하지 않는다.
		// 자신에게 속한 모든 어펙터가 다 finalized 되어야 같이 삭제한다.
		Dictionary<string, List<AffectorBase>>.Enumerator e = _dicActorStateInfo.GetEnumerator();
		while (e.MoveNext())
		{
			bool allFinalized = true;
			for (int i = 0; i < e.Current.Value.Count; ++i)
			{
				if (e.Current.Value[i] == null)
					continue;
				if (e.Current.Value[i].finalized == false)
					allFinalized = false;
			}

			if (allFinalized)
				e.Current.Value.Clear();
		}
	}

	#endregion




	#region Check Group HitStay Interval for Ignore Duplicate
	// 여러 장판이 깔려도 틱당 한번씩만 처리하려면 피격자 입장에서 시간을 체크해야한다.
	// 만약 발사 액터를 구분하지 않는다면 액터아이디와 hitStayId를 조합해서 키를 만들면 될거다. 지금은 안쓸거 같아서 보류
	Dictionary<string, float> _dicHitStayTime = null;
	public bool CheckHitStayInterval(int hitStayIdForIgnoreDuplicate, float hitStayInterval, int creatorActorInstanceId)
	{
		if (_dicHitStayTime == null)
			_dicHitStayTime = new Dictionary<string, float>();

		string key = string.Format("{0}_{1}", creatorActorInstanceId, hitStayIdForIgnoreDuplicate);
		if (_dicHitStayTime.ContainsKey(key) == false)
		{
			_dicHitStayTime.Add(key, Time.time);
			return true;
		}
		float lastTime = _dicHitStayTime[key];
		if (Time.time > lastTime + hitStayInterval)
		{
			_dicHitStayTime[key] = Time.time;
			return true;
		}
		return false;
	}

	public void ClearHitStayInterval()
	{
		if (_dicHitStayTime == null)
			return;
		_dicHitStayTime.Clear();
	}
	#endregion



	#region CannotAction RefCount
	// MeAnimatorSpeed 시그널 발동중에 CannotAction이 함께 호출될 경우 제대로 복구가 안되는 현상이 생겨서 이렇게 예외처리 해보기로 한다.
	bool _animatorSpeedModified;
	public void OnModifyAnimatorSpeed(bool on)
	{
		_animatorSpeedModified = on;
	}

	// 여러개의 CannotAction 이 중복될때는 해당 액터별로 RefCount로 관리해야해서 이렇게 추가해둔다.
	int _cannotActionRefCount = 0;
	float _cannotActionPrevSpeed = 0.0f;
	public void SavePrevSpeed(float speed)
	{
		if (_cannotActionRefCount == 0)
			_cannotActionPrevSpeed = speed;

		++_cannotActionRefCount;
	}

	public void RestorePrevSpeed(Animator animator, bool restoreForDie)
	{
		--_cannotActionRefCount;
		if (_cannotActionRefCount == 0)
		{
			// 여기서 복구를 해야하는데
			if (_animatorSpeedModified || restoreForDie)
			{
				// MeAnimatorSpeed를 사용중이었다면 뭔가 꼬일 염려가 있어서 디폴트값으로 돌리기로 하고(0이나 1보다 훨씬 큰 값으로 변하기도 한다.)
				animator.speed = 1.0f;
			}
			else
			{
				// 사용중이지 않다면 원래대로 복구한다.
				animator.speed = _cannotActionPrevSpeed;
			}
			_cannotActionPrevSpeed = 0.0f;
		}
	}
	#endregion








	// for Sync
	public AffectorBase ExcuteAffectorValue(string affectorValueId, int affectorValueLevel, HitParameter hitParameter, bool managed)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null) return null;

		return ExcuteAffectorValue(data, affectorValueLevel, hitParameter, managed, false);
	}

	/*
	#region Network Sync
	void SyncAffectorInfo(string affectorValueId, int level, HitParameter hitParameter)
	{
		if (isServer)
		{
			RpcExcuteAffector(affectorValueId, level, hitParameter);
		}
	}

	[ClientRpc]
	void RpcExcuteAffector(string affectorValueId, int level, HitParameter hitParameter)
	{
		if (isServer)
			return;

		ExcuteAffectorValue(affectorValueId, level, hitParameter, false);
	}
	#endregion
	*/
}
