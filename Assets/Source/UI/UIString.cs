using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
		get
		{
			return _currentRegion;
		}
		set
		{
			if (_currentRegion != value)
			{
				_currentRegion = value;
				_dicString.Clear();
			}
		}
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
				case "ENG": value = inApkStringTableData.eng; find = true; break;
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

	public string[] ParseParameterString(string[] parameterList)
	{
		bool find = false;
		for (int i = 0; i < parameterList.Length; ++i)
		{
			if (parameterList[i] == null)
				continue;
			if (parameterList[i].Length == 0)
				continue;
			if (parameterList[i][0] == '{' && parameterList[i][parameterList[i].Length - 1] == '}')
			{
				find = true;
				break;
			}
		}
		if (!find)
			return parameterList;
		string[] resultList = new string[parameterList.Length];
		for (int i = 0; i < parameterList.Length; ++i)
		{
			if (parameterList[i] == null)
				continue;
			if (parameterList[i].Length == 0)
				continue;
			if (parameterList[i][0] == '{' && parameterList[i][parameterList[i].Length - 1] == '}')
			{
				resultList[i] = parameterList[i].Substring(1, parameterList[i].Length - 2);
				resultList[i] = GetString(resultList[i]);
			}
			else
			{
				resultList[i] = parameterList[i];
			}
		}
		return resultList;
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



	#region Font
	bool _initializedFont = false;
	public void InitializeFont(string overrideInitialRegion = "")
	{
		if (_initializedFont && currentRegion == overrideInitialRegion)
			return;

		// 옵션매니저 같이 외부에서 받은 초기화 정보가 있다면 그걸로 덮어서 초기화한다.
		if (string.IsNullOrEmpty(overrideInitialRegion) == false)
			currentRegion = overrideInitialRegion;

		ReloadRegionFont();

		_initializedFont = true;
	}

	bool _ignoreUnlocalizedFont = false;
	AsyncOperationHandle<Font> _handleLocalizedFont;
	AsyncOperationHandle<Font> _handleUnlocalizedFont;
	public bool useSystemLocalizedFont { get { return _useSystemLocalizedFont; } }
	public bool useSystemUnlocalizedFont { get { return _useSystemUnlocalizedFont; } }
	bool _useSystemLocalizedFont;
	bool _useSystemUnlocalizedFont;
	Font _systemFont = null;
	// 로드중에는 화면 락같은거로 막힐거라 중복 호출되진 않을거다.
	public void ReloadRegionFont()
	{
		if (_handleLocalizedFont.IsValid())
			Addressables.Release<Font>(_handleLocalizedFont);
		if (_handleUnlocalizedFont.IsValid())
			Addressables.Release<Font>(_handleUnlocalizedFont);

		// 현재 언어에 맞는 로컬 언어와 Unlocalized 폰트 하나. 이렇게 두개 들고있어야한다.
		// 폰트를 Resources에 넣으면 번들과 중복로딩이 될거라서 Resouces에 두면 안된다. 그래서 async로딩을 쓸 수 밖에 없으니 로딩할때 유의할것.
		_useSystemLocalizedFont = _useSystemUnlocalizedFont = false;
		bool result = LoadFont(_currentRegion);
		if (result == false)
			_useSystemLocalizedFont = true;
		
		if (_ignoreUnlocalizedFont == false)
		{
			result = LoadFont("Unlocalized");
			if (result == false)
				_useSystemUnlocalizedFont = true;
		}

		if (_useSystemLocalizedFont || _useSystemUnlocalizedFont)
			_systemFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
		else
			_systemFont = null;
	}

	bool LoadFont(string fontTableId)
	{
		FontTableData fontTableData = TableDataManager.instance.FindFontTableData(fontTableId);
		if (fontTableData == null)
			return false;

		bool loadable = false;
		if (fontTableData.haveInApk) loadable = true;
		//if (fontTableData.haveInApk == false && bundleReceived) loadable = true;

		if (loadable == false)
			return false;

		if (fontTableId == "Unlocalized")
		{
			_handleUnlocalizedFont = Addressables.LoadAssetAsync<Font>(fontTableData.fontName);
		}
		else
		{
			_handleLocalizedFont = Addressables.LoadAssetAsync<Font>(fontTableData.fontName);
			_ignoreUnlocalizedFont = fontTableData.ignoreUnlocalizedFont;
		}

		return true;
	}

	public bool IsDoneLoadAsyncFont()
	{
		if (_useSystemLocalizedFont == false)
		{
			if (_handleLocalizedFont.IsValid() == false || _handleLocalizedFont.IsDone == false)
				return false;
		}

		if (_useSystemUnlocalizedFont == false && _ignoreUnlocalizedFont == false)
		{
			if (_handleUnlocalizedFont.IsValid() == false || _handleUnlocalizedFont.IsDone == false)
				return false;
		}

		return true;
	}

	public Font GetLocalizedFont()
	{
		if (_useSystemLocalizedFont)
			return _systemFont;

		return _handleLocalizedFont.Result;
	}

	public Font GetUnlocalizedFont()
	{
		if (_useSystemUnlocalizedFont)
			return _systemFont;

		if (_ignoreUnlocalizedFont)
			return _handleLocalizedFont.Result;

		return _handleUnlocalizedFont.Result;
	}
	#endregion
}
