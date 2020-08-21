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
	//DynamicBone[] _dynamicBoneList;
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

		// 아무래도 잘못 본건지 다이나믹본은 별 문제가 안보인다. 추가하려다가 패스.
		//if (_dynamicBoneList == null)
		//	_dynamicBoneList = GetComponentsInChildren<DynamicBone>();
		//
		//if (_dynamicBoneList != null)
		//{
		//	for (int i = 0; i < _dynamicBoneList.Length; ++i)
		//	{
		//		for (int j = 0; j < updateCount * 2; ++j)
		//		{
		//			_dynamicBoneList[i].PreUpdate();
		//			_dynamicBoneList[i].UpdateDynamicBones(Time.deltaTime);
		//		}
		//	}
		//}
	}
}
