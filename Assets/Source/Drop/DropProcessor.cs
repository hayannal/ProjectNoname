using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class DropProcessor : MonoBehaviour
{
	public enum eDropType
	{
		None,
		Exp,
		Gold,
		LevelPack,
		Heart,
		Gacha,
		Ultimate,
	}

	#region Static Fuction
	static bool FloatRange(eDropType dropType)
	{
		switch (dropType)
		{
			case eDropType.Ultimate:
				return true;
		}
		return false;
	}

	public static void Drop(Transform rootTransform, string dropId, string addDropId)
	{
		Debug.Log("dropId : " + dropId + " / addDropId : " + addDropId);

		DropProcessor dropProcess = BattleInstanceManager.instance.GetCachedDropProcessor(rootTransform.position);

		if (!string.IsNullOrEmpty(dropId))
		{
			DropTableData dropTableData = TableDataManager.instance.FindDropTableData(dropId);
			if (dropTableData != null)
				Drop(dropProcess, dropTableData);
		}

		if (!string.IsNullOrEmpty(addDropId))
		{
			DropTableData dropTableData = TableDataManager.instance.FindDropTableData(addDropId);
			if (dropTableData != null)
				Drop(dropProcess, dropTableData);
		}

		dropProcess.StartDrop();
	}

	static void Drop(DropProcessor dropProcessor, DropTableData dropTableData)
	{
		for (int i = 0; i < dropTableData.dropEnum.Length; ++i)
		{
			if (Random.value > dropTableData.probability[i])
				continue;

			eDropType dropType = (eDropType)dropTableData.dropEnum[i];
			float floatValue = 0.0f;
			int intValue = 0;
			if (FloatRange(dropType))
				floatValue = Random.Range(dropTableData.minValue[i], dropTableData.maxValue[i]);
			else
			{
				int minValue = Mathf.RoundToInt(dropTableData.minValue[i]);
				int maxValue = Mathf.RoundToInt(dropTableData.maxValue[i]);
				intValue = Random.Range(minValue, maxValue + 1);
			}

			switch (dropType)
			{
				case eDropType.Exp:
					dropProcessor.Add(dropType, floatValue, intValue);
					break;
				case eDropType.Gold:
					DropGold(intValue);
					break;
				case eDropType.LevelPack:
					dropProcessor.Add(dropType, floatValue, intValue);
					break;
				case eDropType.Heart:
					dropProcessor.Add(dropType, floatValue, intValue);
					break;
				case eDropType.Gacha:
					dropProcessor.Add(dropType, floatValue, intValue);
					break;
				case eDropType.Ultimate:
					DropSp(floatValue);
					break;
			}
		}
	}

	public static void DropSp(float dropSpValue)
	{
		if (dropSpValue == 0.0f)
			return;

		Debug.Log("dropSp : " + dropSpValue);

		BattleInstanceManager.instance.playerActor.actorStatus.AddSP(dropSpValue);
	}

	public static void DropGold(int dropGoldValue)
	{
		if (dropGoldValue == 0)
			return;

		Debug.Log("dropGold : " + dropGoldValue);

		//StageManager.instance.StackDropGold(dropGoldValue);
	}
	#endregion

	class DropObjectInfo
	{
		public eDropType dropType;
		public float floatValue;
		public int intValue;
	}
	List<DropObjectInfo> _listDropObjectInfo = new List<DropObjectInfo>();

    public void Add(eDropType dropType, float floatValue, int intValue)
	{
		DropObjectInfo newInfo = null;
		switch (dropType)
		{
			case eDropType.Exp:
				int randomCount = Random.Range(4, 7);
				int quotient = intValue / randomCount;
				int remainder = intValue % randomCount;
				for (int i = 0; i < randomCount; ++i)
				{
					int currentCount = quotient + ((i < remainder) ? 1 : 0);
					newInfo = new DropObjectInfo();
					newInfo.dropType = dropType;
					newInfo.intValue = currentCount;
					_listDropObjectInfo.Add(newInfo);
				}
				break;
			case eDropType.Gold:
				break;
			case eDropType.LevelPack:
			case eDropType.Heart:
			case eDropType.Gacha:
				newInfo = new DropObjectInfo();
				newInfo.dropType = dropType;
				newInfo.intValue = intValue;
				_listDropObjectInfo.Add(newInfo);
				break;
			case eDropType.Ultimate:
				break;
		}
	}

	public void StartDrop()
	{
		Timing.RunCoroutine(DropProcess());
	}

	IEnumerator<float> DropProcess()
	{
		for (int i = 0; i < _listDropObjectInfo.Count; ++i)
		{
			GameObject dropObjectPrefab = GetDropObjectPrefab(_listDropObjectInfo[i].dropType);
			if (dropObjectPrefab == null)
				continue;

			DropObject cachedItem = BattleInstanceManager.instance.GetCachedDropObject(dropObjectPrefab, GetRandomDropPosition(), Quaternion.identity);
			cachedItem.Initialize(_listDropObjectInfo[i].dropType, _listDropObjectInfo[i].floatValue, _listDropObjectInfo[i].intValue);
			yield return Timing.WaitForSeconds(0.2f);
		}

		_listDropObjectInfo.Clear();
		gameObject.SetActive(false);
	}

	// temp code
	GameObject GetDropObjectPrefab(eDropType dropType)
	{
		string name = string.Format("Drop{0}", dropType.ToString());
		for (int i = 0; i < StageManager.instance.dropObjectPrefabList.Length; ++i)
		{
			if (StageManager.instance.dropObjectPrefabList[i].name == name)
				return StageManager.instance.dropObjectPrefabList[i];
		}
		return null;
	}

	Vector3 GetRandomDropPosition()
	{
		Vector2 randomOffset = Random.insideUnitCircle * Random.value * 2.0f;
		return cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);
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
}
