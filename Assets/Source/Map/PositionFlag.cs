using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionFlag : MonoBehaviour
{
	public float range;
	public string flagValue;

	void OnEnable()
	{
		OnInitialized(this);
	}

	void OnDisable()
	{
		OnFinalized(this);
	}


	public static bool IsInRange(AffectorProcessor affectorProcessor, string flagValue)
	{
		// 기본적으로 포지션 버프는 플레이어에게만 적용되게 되어있다.
		// 혹시 나중에 몬스터용이 필요해진다면 플래그 추가해서 처리하기로 한다.
		if (affectorProcessor.actor != null && affectorProcessor.actor.IsMonsterActor())
			return false;

		if (s_listInitializedPositionFlag == null)
			return false;

		for (int i = 0; i < s_listInitializedPositionFlag.Count; ++i)
		{
			if (s_listInitializedPositionFlag[i].flagValue != flagValue)
				continue;

			Vector3 diff = affectorProcessor.cachedTransform.position - s_listInitializedPositionFlag[i].cachedTransform.position;
			float sqrMagnitude = diff.x * diff.x + diff.z * diff.z;
			if (sqrMagnitude > s_listInitializedPositionFlag[i].range * s_listInitializedPositionFlag[i].range)
				continue;

			return true;
		}
		return false;
	}




	static List<PositionFlag> s_listInitializedPositionFlag;
	static void OnInitialized(PositionFlag positionFlag)
	{
		if (s_listInitializedPositionFlag == null)
			s_listInitializedPositionFlag = new List<PositionFlag>();

		if (s_listInitializedPositionFlag.Contains(positionFlag) == false)
			s_listInitializedPositionFlag.Add(positionFlag);
	}

	static void OnFinalized(PositionFlag positionFlag)
	{
		if (s_listInitializedPositionFlag == null)
			return;

		s_listInitializedPositionFlag.Remove(positionFlag);
	}

	public static void OnPreInstantiateMap()
	{
		if (s_listInitializedPositionFlag == null)
			return;

		for (int i = 0; i < s_listInitializedPositionFlag.Count; ++i)
			s_listInitializedPositionFlag[i].gameObject.SetActive(false);
		s_listInitializedPositionFlag.Clear();
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

#if UNITY_EDITOR
	void OnDrawGizmosSelected()
	{
		UnityEditor.Handles.DrawWireDisc(cachedTransform.position, Vector3.up, range);
	}
#endif
}