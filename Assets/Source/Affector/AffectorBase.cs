using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AffectorBase
{
	protected eAffectorType _affectorType;
	protected AffectorProcessor _affectorProcessor;
	protected Actor _actor;

	bool _finalized = false;
	public bool finalized
	{
		get
		{
			return _finalized;
		}
		set
		{
			if (_finalized == false)
			{
				// 내부에서 호출된게 아니라면 강제로 지우려는 시도일 것이다. 이펙트 삭제같은거 바로 해야하니 FinalizeAffector를 호출해준다.
				FinalizeAffector();
			}
			_finalized = value;
		}
	}

	// 매니지드로 체크된 애들은 컨티뉴어스 리스트 돌때 다른 곳에서 관리되는 애들이니 AffectorValueId 가 같은지 체크하지 않는다.
	public bool managed { get; set; }

	public eAffectorType affectorType { get { return _affectorType; } }
	public string affectorValueId { get; set; }

	public virtual void InitializeAffector(eAffectorType affectorType, Actor actor, AffectorProcessor affectorProcessor)
	{
		_affectorType = affectorType;
		_actor = actor;
		_affectorProcessor = affectorProcessor;
	}

	public virtual void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
	}

	public virtual void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
	}

	public virtual void UpdateAffector()
	{
	}

	public virtual void FinalizeAffector()
	{
	}
}
