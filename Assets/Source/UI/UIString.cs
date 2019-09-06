using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIString : MonoBehaviour
{
	public static UIString instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(Resources.Load<GameObject>("UI/UIString")).GetComponent<UIString>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static UIString _instance = null;

	public InApkStringTable inApkStringTable;

	string _currentRegion = "KOR";
	public string currentRegion
	{
		get { return _currentRegion; }
		set { _currentRegion = value; }
	}

	// 스트링 비교는 많이 할수록 오래 걸릴테니 한번 찾을때마다 캐싱해서 넣어둔다.
	// 국가 전환할때만 리셋해주면 된다.
	Dictionary<string, string> _dicString = new Dictionary<string, string>();

	public string GetString(string id)
	{
		if (_dicString.ContainsKey(id))
			return _dicString[id];

		string value = "";
		bool find = false;
		// check inApk string data
		InApkStringTableData inApkStringTableData = FindInApkStringTableData(id);
		if (inApkStringTableData != null)
		{
			switch (_currentRegion)
			{
				case "KOR": value = inApkStringTableData.kor; find = true; break;
				case "ENG": value = inApkStringTableData.eng; find = true;  break;
			}
		}

		// check string data
		// 번들을 받은 상태라면 로딩해도 된다. 지금은 아직 패치매니저를 만들기 전이니 로딩하지 않는다.
		if (string.IsNullOrEmpty(value))
		{

		}

		if (find)
		{
			_dicString.Add(id, value);
			return value;
		}

		return string.Format("UID:{0}", id);
	}

	public string GetString(string key, params object[] arg)
	{
		string value = GetString(key);
		return string.Format(value, arg);
	}
	


	InApkStringTableData FindInApkStringTableData(string id)
	{
		for (int i = 0; i < inApkStringTable.dataArray.Length; ++i)
		{
			if (inApkStringTable.dataArray[i].id == id)
				return inApkStringTable.dataArray[i];
		}
		return null;
	}


	/*
	Font _regionFont = null;
	public void ReloadRegionFont()
	{
		switch (UIString.instance.currentRegion)
		{
		case "KR":
			_regionFont = Resources.Load<Font>("Font/SeoulNamsanEB");
			break;
		case "US":
			_regionFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
			break;
		case "JP":
			_regionFont = Resources.Load<Font>("Font/Meiryo");
			break;
		}
	}

	public void CheckFont(Text textComponent)
	{
		if (_regionFont == null)
			return;
		
		if (textComponent.font.name.Contains("PoetsenOne"))
			return;

		if (textComponent.font != _regionFont)
			textComponent.font = _regionFont;
	}
	*/
}
