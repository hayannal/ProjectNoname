using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StringUtil : MonoBehaviour {

	static public void SplitIntList(string szData, ref List<int> listData)
	{
		if (listData == null) listData = new List<int>();
		listData.Clear();

		if (string.IsNullOrEmpty(szData))
			return;

		string[] split = szData.Split(',');
		for (int i = 0; i < split.Length; ++i)
		{
			int parse = 0;
			if (int.TryParse(split[i], out parse))
				listData.Add(parse);
		}
	}

	static public void SplitStringList(string origString)
	{
	}
}
