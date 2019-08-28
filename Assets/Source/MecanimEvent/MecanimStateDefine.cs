using UnityEngine;
using System.Collections;

namespace MecanimStateDefine {

	public enum eMecanimState
	{
		Idle = 1,
		Move = 2,
		Attack = 3,
		Attacked = 4,
		Ultimate = 5,
	}
}

public class MecanimStateUtil_NotUsed
{
	/*
	public static string GetNotAllowingMoveStateList()
	{
		return "3,4,5,6,7";
	}
	
	public static bool IsMovable(MecanimState mecanimState)
	{
		if (mecanimState.IsState((int)MecanimStateDefine.eState.Attack) ||
			mecanimState.IsState((int)MecanimStateDefine.eState.Hitted) ||
			mecanimState.IsState((int)MecanimStateDefine.eState.AutoSkill) ||
			mecanimState.IsState((int)MecanimStateDefine.eState.ActiveSkill) ||
			mecanimState.IsState((int)MecanimStateDefine.eState.BigHitted))
			return false;
		return true;
	}
	*/
}