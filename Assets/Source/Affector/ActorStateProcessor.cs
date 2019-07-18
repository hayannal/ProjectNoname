using System.Collections;
using System.Collections.Generic;
using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////////////////
// 게임이라면 흔하게 쓰는 상태이상 및 버프 디버프 등을 묶음으로 관리하기 위해 만든 프로세서
// 내부에선 결국 어펙터 프로세서처럼 어펙터를 돌린다는 점에서 어펙터 프로세서 옆에 두게 되었다.
// 어펙터의 가장 큰 단점이 여러 컨티뉴어스 어펙터를 묶는 개념이 없어서 개별로 어펙터 처리를 해야한다는 것인데,
// 그걸 극복하기 위해 추가된 프로세서다.
public class ActorStateProcessor : MonoBehaviour
{
	Dictionary<string, List<AffectorBase>> _dicActorStateInfo;

	void OnEnable()
	{
		if (_dicActorStateInfo == null)
			return;

		Dictionary<string, List<AffectorBase>>.Enumerator e = _dicActorStateInfo.GetEnumerator();
		while (e.MoveNext())
			e.Current.Value.Clear();
	}

	public void AddActorState(string actorStateId, string[] affectorValueIdList, HitParameter hitParameter)
	{
		if (_dicActorStateInfo == null)
			_dicActorStateInfo = new Dictionary<string, List<AffectorBase>>();

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
							listAffector[i] = CreateNewContinuousAffector(affectorValueIdList[i], hitParameter);
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
				AffectorBase newAffector = CreateNewContinuousAffector(affectorValueIdList[i], hitParameter);
				if (newAffector == null)
					continue;

				if (AffectorCustomCreator.IsContinuousAffector(newAffector.affectorType))
					listAffector.Add(newAffector);
				else
					Debug.LogErrorFormat("Non-continuous affector in a Actor State! / actorStateId = {0} / AffectorValueId = {1}", actorStateId, affectorValueIdList[i]);
			}
		}
	}

	AffectorBase CreateNewContinuousAffector(string affectorValueId, HitParameter hitParameter)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null)
			return null;
		AffectorValueLevelTableData levelData = GetAffectorValueLevelTableData(affectorValueId, hitParameter);
		if (levelData == null)
			return null;
		
		eAffectorType affectorType = (eAffectorType)data.affectorId;
		AffectorBase affectorBase = AffectorCustomCreator.CreateAffector(affectorType);
		if (affectorBase != null)
		{
			affectorBase.InitializeAffector(affectorType, affectorProcessor.actor, affectorProcessor);
			affectorBase.affectorValueId = levelData.affectorValueId;
			affectorBase.ExecuteAffector(levelData, hitParameter);
		}
		return affectorBase;
	}

	AffectorValueLevelTableData GetAffectorValueLevelTableData(string affectorValueId, HitParameter hitParameter)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null)
			return null;

		int affectorValueLevel = affectorProcessor.GetAffectorValueLevel(data, hitParameter.statusStructForHitObject.skillLevel);
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

	void Update()
	{
		if (_dicActorStateInfo == null)
			return;

		Dictionary<string, List<AffectorBase>>.Enumerator e = _dicActorStateInfo.GetEnumerator();
		while (e.MoveNext())
		{
			for (int i = 0; i < e.Current.Value.Count; ++i)
			{
				if (e.Current.Value[i].finalized)
					continue;

				e.Current.Value[i].UpdateAffector();
			}
		}

		// AffectorProcessor와 달리 finalized 된다고 바로 삭제하지 않는다.
		// 자신에게 속한 모든 어펙터가 다 finalized 되어야 같이 삭제한다.
		e = _dicActorStateInfo.GetEnumerator();
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




	AffectorProcessor _affectorProcessor;
	public AffectorProcessor affectorProcessor
	{
		get
		{
			if (_affectorProcessor == null)
				_affectorProcessor = GetComponent<AffectorProcessor>();
			return _affectorProcessor;
		}
	}
}
