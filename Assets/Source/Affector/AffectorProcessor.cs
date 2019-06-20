using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AffectorProcessor : MonoBehaviour {

	Dictionary<eAffectorType, AffectorBase> _dicAffector;
	List<AffectorBase> _listContinuousAffector;

	Actor _actor;

	void Awake()
	{
		_actor = GetComponent<Actor>();
	}

	public void ExcuteAffectorValue(string affectorValueId, HitParameter hitParameter, bool syncAffector)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null) return;

		ExcuteAffector(affectorValueId, data, hitParameter);

		/*
		#region Network Sync
		if (syncAffector)
			SyncAffectorInfo(affectorValueID, hitParameter);
		#endregion
		*/
	}

	public void ExcuteAffector(string affectorValueID, AffectorValueTableData data, HitParameter hitParameter)
	{
		eAffectorType affectorType = (eAffectorType)data.affectorId;
		if (AffectorCustomCreator.IsContinuousAffector(affectorType))
		{
			// to update
			if (_listContinuousAffector == null) _listContinuousAffector = new List<AffectorBase>();
			AffectorBase affectorBase = AffectorCustomCreator.CreateAffector(affectorType);
			if (affectorBase != null)
			{
				if (affectorBase.Initialize(_actor, this) == false)
					return;
				_listContinuousAffector.Add(affectorBase);
				affectorBase.ExcuteAffector(affectorValueID, data, hitParameter);
			}
		}
		else
		{
			AffectorBase affectorBase = null;
			if (_dicAffector == null) _dicAffector = new Dictionary<eAffectorType, AffectorBase>();
			if (_dicAffector.ContainsKey(affectorType)) affectorBase = _dicAffector[affectorType];
			else
			{
				affectorBase = AffectorCustomCreator.CreateAffector(affectorType);
				if (affectorBase != null)
				{
					if (affectorBase.Initialize(_actor, this) == false)
						return;
					_dicAffector.Add(affectorType, affectorBase);
				}
			}
			if (affectorBase != null) affectorBase.ExcuteAffector(affectorValueID, data, hitParameter);
		}
	}

	void Update()
	{
		if (_listContinuousAffector == null)
			return;

		for (int i = 0; i < _listContinuousAffector.Count; ++i)
			_listContinuousAffector[i].Update();

		for (int i = _listContinuousAffector.Count-1; i >= 0; --i)
		{
			if (_listContinuousAffector[i].finalized)
				_listContinuousAffector.RemoveAt(i);
		}
	}

	/*
	#region Network Sync
	void SyncAffectorInfo(string affectorValueID, HitParameter hitParameter)
	{
		if (isServer)
		{
			RpcExcuteAffector(affectorValueID, hitParameter);
		}
	}

	[ClientRpc]
	void RpcExcuteAffector(string affectorValueID, HitParameter hitParameter)
	{
		if (isServer)
			return;

		ExcuteAffectorValue(affectorValueID, hitParameter, false);
	}
	#endregion
	*/
}
