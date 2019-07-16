using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AffectorProcessor : MonoBehaviour {

	Dictionary<int, AffectorBase> _dicAffector;
	List<AffectorBase> _listContinuousAffector;

	public Actor actor { get; set; }

	void Awake()
	{
		actor = GetComponent<Actor>();
	}

	void OnEnable()
	{
		if (_listContinuousAffector == null)
			return;

		for (int i = _listContinuousAffector.Count - 1; i >= 0; --i)
			_listContinuousAffector.RemoveAt(i);
	}

	public void ExcuteAffectorValue(string affectorValueId, int affectorValueLevel, HitParameter hitParameter)
	{
		AffectorValueTableData data = TableDataManager.instance.FindAffectorValueTableData(affectorValueId);
		if (data == null) return;

		ExcuteAffectorValue(data, affectorValueLevel, hitParameter, false);
	}

	public void ExcuteAffectorValue(AffectorValueTableData data, int affectorValueLevel, HitParameter hitParameter, bool syncAffector)
	{
		AffectorValueLevelTableData levelData = TableDataManager.instance.FindAffectorValueLevelTableData(data.id, affectorValueLevel);
		if (levelData == null)
		{
			Debug.LogErrorFormat("Not found AffectorValueLevelTableData. AffectorValueId = {0} / level = {1}", data.id, affectorValueLevel);
			return;
		}

		ExcuteAffector((eAffectorType)data.affectorId, levelData, hitParameter);

		//#region Network Sync
		//if (syncAffector)
		//	SyncAffectorInfo(data.id, leve, hitParameter);
		//#endregion
	}

	void ExcuteAffector(eAffectorType affectorType, AffectorValueLevelTableData levelData, HitParameter hitParameter)
	{
		if (AffectorCustomCreator.IsContinuousAffector(affectorType))
		{
			// to update
			if (_listContinuousAffector == null) _listContinuousAffector = new List<AffectorBase>();
			AffectorBase affectorBase = AffectorCustomCreator.CreateAffector(affectorType);
			if (affectorBase != null)
			{
				if (affectorBase.Initialize(actor, this) == false)
					return;
				_listContinuousAffector.Add(affectorBase);
				affectorBase.ExecuteAffector(levelData, hitParameter);
			}
		}
		else
		{
			AffectorBase affectorBase = null;
			if (_dicAffector == null) _dicAffector = new Dictionary<int, AffectorBase>();
			if (_dicAffector.ContainsKey((int)affectorType)) affectorBase = _dicAffector[(int)affectorType];
			else
			{
				affectorBase = AffectorCustomCreator.CreateAffector(affectorType);
				if (affectorBase != null)
				{
					if (affectorBase.Initialize(actor, this) == false)
						return;
					_dicAffector.Add((int)affectorType, affectorBase);
				}
			}
			if (affectorBase != null) affectorBase.ExecuteAffector(levelData, hitParameter);
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
