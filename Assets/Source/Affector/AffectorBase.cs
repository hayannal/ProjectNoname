using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AffectorBase
{
	protected eAffectorType _affectorType;
	protected AffectorProcessor _affectorProcessor;
	protected Actor _actor;

	bool _finalized = false;
	public bool finalized { get { return _finalized; } set { _finalized = value; } }
	public eAffectorType affectorType { get { return _affectorType; } }

	public virtual bool Initialize(eAffectorType affectorType, Actor actor, AffectorProcessor affectorProcessor)
	{
		_affectorType = affectorType;
		_actor = actor;
		_affectorProcessor = affectorProcessor;
		return true;
	}

	public virtual void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
	}

	public virtual void Update()
	{
	}

	// Etc
	public virtual float GetRemainTime() { return -1.0f; }
}
