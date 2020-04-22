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

	// 스트링 테이블을 패치 가능한 어드레서블 하나로 통합하면서 직접 연결하지 않기로 한다.
	//public InApkStringTable inApkStringTable;

	// 그리고 FontTable은 직접 가져와 쓰는거로 해서
	// TableDataManaer의 패치가 필요할때에도 제대로 된 스트링과 폰트로 보여주면서 확인창을 띄울 수 있게 처리한다.
	// 폰트테이블은 어차피 번역나라가 추가될때 재빌드 해야하므로 Resources에 포함되어도 상관없다.
	public FontTable fontTable;
	public LanguageTable languageTable;

	public LanguageTableData FindLanguageTableData(string languageId)
	{
		for (int i = 0; i < languageTable.dataArray.Length; ++i)
		{
			if (languageTable.dataArray[i].id == languageId)
				return languageTable.dataArray[i];
		}
		return null;
	}

	public LanguageTableData FindLanguageTableDataBySystemLanguage(int systemLanguage)
	{
		for (int i = 0; i < languageTable.dataArray.Length; ++i)
		{
			if (languageTable.dataArray[i].unityLanguageCode == systemLanguage)
				return languageTable.dataArray[i];
		}
		return null;
	}

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

	#region Initialize
	bool _initialized = false;
	public void Initialize(string overrideInitialRegion = "")
	{
		if (_initialized && currentRegion == overrideInitialRegion)
			return;

		ReloadStringData();
		InitializeFont(overrideInitialRegion);

		_initialized = true;
	}
	#endregion

	#region StringData
	AsyncOperationHandle<StringTable> _handleStringTable;
	void ReloadStringData()
	{
		if (_handleStringTable.IsValid())
			Addressables.Release<StringTable>(_handleStringTable);

		_handleStringTable = Addressables.LoadAssetAsync<StringTable>("StringTable");
	}

	public bool IsDoneLoadAsyncStringData()
	{
		if (_handleStringTable.IsValid() == false || _handleStringTable.IsDone == false)
			return false;

		return true;
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

		// 로딩속도와 메모리, 작업 환경등을 고려한 결과
		// InApk를 구분하지 않고 나라도 구분하지 않고 패치는 통으로 받을 수 있도록 한 파일안에 모든 스트링 데이터를 넣는게
		// 가장 효율적이란 결론을 내렸다. 그래서 MainSceneBuilder에서 
		// check inApk string data
		StringTableData stringTableData = FindStringTableData(id);
		if (stringTableData != null)
		{
			switch (_currentRegion)
			{
				case "KOR": value = stringTableData.kor; find = true; break;
				case "ENG": value = stringTableData.eng; find = true; break;
				case "JPN": value = stringTableData.jpn; find = true; break;
				case "CHN": value = stringTableData.chn; find = true; break;
				case "CHW": value = stringTableData.chw; find = true; break;
				case "FRN": value = stringTableData.frn; find = true; break;
				case "GMN": value = stringTableData.gmn; find = true; break;
				case "IND": value = stringTableData.ind; find = true; break;
				case "ITA": value = stringTableData.ita; find = true; break;
				case "RUS": value = stringTableData.rus; find = true; break;
				case "SPN": value = stringTableData.spn; find = true; break;
				case "THA": value = stringTableData.tha; find = true; break;
				case "VIE": value = stringTableData.vie; find = true; break;
				case "PRT": value = stringTableData.prt; find = true; break;
				case "ARB": value = stringTableData.arb; find = true; break;
				case "BLR": value = stringTableData.blr; find = true; break;
				case "BGR": value = stringTableData.bgr; find = true; break;
				case "CZE": value = stringTableData.cze; find = true; break;
				case "DUT": value = stringTableData.dut; find = true; break;
				case "FIN": value = stringTableData.fin; find = true; break;
				case "GRE": value = stringTableData.gre; find = true; break;
				case "HBR": value = stringTableData.hbr; find = true; break;
				case "HGR": value = stringTableData.hgr; find = true; break;
				case "MLY": value = stringTableData.mly; find = true; break;
				case "POL": value = stringTableData.pol; find = true; break;
				case "RMN": value = stringTableData.rmn; find = true; break;
				case "SVK": value = stringTableData.svk; find = true; break;
				case "SWD": value = stringTableData.swd; find = true; break;
				case "TUR": value = stringTableData.tur; find = true; break;
				case "UKR": value = stringTableData.ukr; find = true; break;
				default:
#if UNITY_EDITOR
					Debug.LogErrorFormat("Invalid localize region! : {0}", _currentRegion);
#endif
					break;
			}
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

	StringTableData FindStringTableData(string id)
	{
		// MainSceneBuilder에서 로딩 완료를 확인하고 캔버스들을 초기화 했을테니 이미 로딩이 되었다고 판단하고 검사하지 않는다.
		//if (IsDoneLoadAsyncStringData())
		//	return null;
		StringTable stringTable = _handleStringTable.Result;
		for (int i = 0; i < stringTable.dataArray.Length; ++i)
		{
			if (stringTable.dataArray[i].id == id)
				return stringTable.dataArray[i];
		}
		return null;
	}
	#endregion

	#region Font
	void InitializeFont(string overrideInitialRegion = "")
	{
		// 옵션매니저 같이 외부에서 받은 초기화 정보가 있다면 그걸로 덮어서 초기화한다.
		if (string.IsNullOrEmpty(overrideInitialRegion) == false)
			currentRegion = overrideInitialRegion;

		ReloadRegionFont();
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
		FontTableData fontTableData = FindFontTableData(fontTableId);
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

	FontTableData FindFontTableData(string condition)
	{
		for (int i = 0; i < fontTable.dataArray.Length; ++i)
		{
			if (fontTable.dataArray[i].id == condition)
				return fontTable.dataArray[i];
		}
		return null;
	}
	#endregion
}
