using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AffectorProcessor : MonoBehaviour {

	Dictionary<int, AffectorBase> _dicAffector;
	Dictionary<int, List<AffectorBase>> _dicContinuousAffector;

	public Actor actor { get; private set; }

	void Awake()
	{
		actor = GetComponent<Actor>();
	}

	void OnEnable()
	{
		if (_dicContinuousAffector == null)
			return;

		Dictionary<int, List<AffectorBase>>.Enumerator e = _dicContinuousAffector.GetEnumerator();
		while (e.MoveNext())
			e.Current.Value.Clear();
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

	public AffectorBase ApplyAffectorValue(string affectorValueId, HitParameter hitParameter, bool syncAffector = true)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null)
			return null;
		int affectorValueLevel = GetAffectorValueLevel(data, hitParameter.statusStructForHitObject.skillLevel);
		return ExcuteAffectorValue(data, affectorValueLevel, hitParameter, syncAffector);
	}

	AffectorBase ExcuteAffectorValue(AffectorValueTableData data, int affectorValueLevel, HitParameter hitParameter, bool syncAffector)
	{
		AffectorValueLevelTableData levelData = TableDataManager.instance.FindAffectorValueLevelTableData(data.id, affectorValueLevel);
		if (levelData == null)
		{
			Debug.LogErrorFormat("Not found AffectorValueLevelTableData. AffectorValueId = {0} / level = {1}", data.id, affectorValueLevel);
			return null;
		}

		return ExcuteAffector((eAffectorType)data.affectorId, levelData, hitParameter);

		//#region Network Sync
		//if (syncAffector)
		//	SyncAffectorInfo(data.id, level, hitParameter);
		//#endregion
	}

	AffectorBase ExcuteAffector(eAffectorType affectorType, AffectorValueLevelTableData levelData, HitParameter hitParameter)
	{
		for (int i = 0; i < levelData.conditionValueId.Length; ++i)
		{
			if (string.IsNullOrEmpty(levelData.conditionValueId[i]))
				continue;
			if (Condition.CheckCondition(levelData.conditionValueId[i], hitParameter, this, actorStateProcessor, actor) == false)
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
			for (int i = 0; i < listContinuousAffector.Count; ++i)
			{
				if (listContinuousAffector[i].finalized)
					continue;

				if (listContinuousAffector[i].affectorValueId == levelData.affectorValueId)
				{
					existSameContinuousAffector = listContinuousAffector[i];
					affectorBase = existSameContinuousAffector;
					break;
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
					affectorBase.affectorValueId = levelData.affectorValueId;
					affectorBase.ExecuteAffector(levelData, hitParameter);
					listContinuousAffector.Add(affectorBase);
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

	public bool IsContinuousAffectorValueId(string affectorValueId)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null)
			return false;
		if (_dicContinuousAffector == null)
			return false;
		if (_dicContinuousAffector.ContainsKey(data.affectorId) == false)
			return false;

		List<AffectorBase> listContinuousAffector = _dicContinuousAffector[data.affectorId];
		for (int i = 0; i < listContinuousAffector.Count; ++i)
		{
			if (listContinuousAffector[i].finalized)
				continue;
			if (listContinuousAffector[i].affectorValueId == affectorValueId)
				return true;
		}
		return false;
	}

	void Update()
	{
		if (_dicContinuousAffector == null)
			return;

		Dictionary<int, List<AffectorBase>>.Enumerator e = _dicContinuousAffector.GetEnumerator();
		while (e.MoveNext())
		{
			for (int i = 0; i < e.Current.Value.Count; ++i)
				e.Current.Value[i].UpdateAffector();
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

	void OnCollisionEnter(Collision collision)
	{
		//Debug.Log("hitted object collision enter");

		bool collided = false;
		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;

			collided = true;

			HitObject hitObject = BattleInstanceManager.instance.GetHitObjectFromCollider(col);
			if (hitObject == null)
				continue;

			hitObject.OnCollisionEnterAffectorProcessor(this, contact);
		}
	}


	ActorStateProcessor _actorStateProcessor;
	public ActorStateProcessor actorStateProcessor
	{
		get
		{
			if (_actorStateProcessor == null)
				_actorStateProcessor = GetComponent<ActorStateProcessor>();
			return _actorStateProcessor;
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


	// for Sync
	public AffectorBase ExcuteAffectorValue(string affectorValueId, int affectorValueLevel, HitParameter hitParameter)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null) return null;

		return ExcuteAffectorValue(data, affectorValueLevel, hitParameter, false);
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
