using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TailAnimatorUpdater : MonoBehaviour
{
	#region staticFunction
	public static void UpdateAnimator(Transform rootTransform, int updateCount)
	{
		TailAnimatorUpdater tailAnimatorUpdater = rootTransform.GetComponent<TailAnimatorUpdater>();
		if (tailAnimatorUpdater == null) tailAnimatorUpdater = rootTransform.gameObject.AddComponent<TailAnimatorUpdater>();
		tailAnimatorUpdater.UpdateAnimator(updateCount);
	}
	#endregion


	FIMSpace.FTail.FTail_Animator[] _tailAnimatorList;
	public void UpdateAnimator(int updateCount)
	{
		if (_tailAnimatorList == null)
			_tailAnimatorList = GetComponentsInChildren<FIMSpace.FTail.FTail_Animator>();

		if (_tailAnimatorList != null)
		{
			for (int i = 0; i < _tailAnimatorList.Length; ++i)
			{
				for (int j = 0; j < updateCount; ++j)
					_tailAnimatorList[i].CalculateOffsets();
			}
		}
	}
}
