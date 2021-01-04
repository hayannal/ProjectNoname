using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MEC;
using MecanimStateDefine;

// 시작부터 버로우 하는 RtsTurret몬스터가 사용하는 패시브 어펙터
public class BurrowOnStartAffector : AffectorBase
{
	bool _standby = false;
	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// Die했을때 꺼놨던걸 복구해야한다.
		_actor.actionController.animator.enabled = true;

		_affectorValueLevelTableData = affectorValueLevelTableData;

		// 진짜 본체는 Burrow 포지션에 두고 애니만 올려둔다.
		_actor.actionController.cachedAnimatorTransform.localPosition = new Vector3(0.0f, -BurrowAffector.s_BurrowPositionY, 0.0f);
		_actor.cachedTransform.localPosition = new Vector3(_actor.cachedTransform.localPosition.x, BurrowAffector.s_BurrowPositionY, _actor.cachedTransform.localPosition.z);
		_standby = true;
	}

	public override void SendInfo(string arg)
	{
		if (_actor.actorStatus.IsDie())
			return;

		if (arg == "burrowOff")
		{
			if (_standby)
			{
				// 이때가 다 올라온 시점일거다.
				_actor.actionController.cachedAnimatorTransform.localPosition = Vector3.zero;
				_actor.cachedTransform.position = new Vector3(_actor.cachedTransform.position.x, 0.0f, _actor.cachedTransform.position.z);
				_standby = false;
			}
		}
		else if (arg == "burrowOn")
		{
			// 이때가 다 내려간 시점일거다.
			// 사실 정확히 처리하려면 DisableActorCollider 발동되는 시점에 내려야하는데
			// 어차피 안내려도 컬리더가 꺼있어서 타격이 들어가지 않아서 상관없긴 하다. 우선 이렇게 해본다.
			// 이래야 standby 구분도 명확하게 내려와있을땐 standby true 나머지 상황에선 false로 된다.
			_actor.actionController.cachedAnimatorTransform.localPosition = new Vector3(0.0f, -BurrowAffector.s_BurrowPositionY, 0.0f);
			_actor.cachedTransform.position = new Vector3(_actor.cachedTransform.position.x, BurrowAffector.s_BurrowPositionY, _actor.cachedTransform.position.z);
			_standby = true;
		}
	}

	bool CheckBurrow()
	{
		// 내려가있거나 collider 꺼있는 상태라면 burrow상태로 처리. 이때를 제외하곤 텔레포트 홀드 다 걸린다.
		return (_standby || _actor.GetCollider().enabled == false);
	}

	void CheckDie()
	{
		// Die애니가 아예 없는 관계로 애니메이터만 끄기로 한다. 흠 이걸 해도 될지 모르겠다.
		_actor.actionController.animator.enabled = false;
	}

	public static bool CheckBurrow(AffectorProcessor affectorProcessor)
	{
		BurrowOnStartAffector burrowOnStartAffector = (BurrowOnStartAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.BurrowOnStart);
		if (burrowOnStartAffector == null)
			return false;

		return burrowOnStartAffector.CheckBurrow();
	}

	public static void CheckDie(AffectorProcessor affectorProcessor)
	{
		BurrowOnStartAffector burrowOnStartAffector = (BurrowOnStartAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.BurrowOnStart);
		if (burrowOnStartAffector == null)
			return;

		burrowOnStartAffector.CheckDie();
	}
}