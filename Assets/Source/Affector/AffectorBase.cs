using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AffectorBase
{
	protected AffectorProcessor _affectorProcessor;
	protected Actor _actor;

	bool _finalized = false;
	public bool finalized { get { return _finalized; } }

	public virtual bool Initialize(Actor actor, AffectorProcessor affectorProcessor)
	{
		_actor = actor;
		_affectorProcessor = affectorProcessor;
		return true;
	}

	public virtual void ExecuteAffector(string affectorValueId, AffectorValueTableData affectorValueTableData, HitParameter hitParameter)
	{
	}

	public virtual void Update()
	{
	}
}
