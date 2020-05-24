using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterInfoCanvas : MonoBehaviour
{
	public static CharacterInfoCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject[] innerMenuPrefabList;
	public MenuButton[] menuButtonList;
	public GameObject menuRootObject;

	// 4개의 메뉴중에 2개만 알람을 쓰니 따로 관리하도록 한다. 그런데 그림자가 있어야 예뻐져서 각각 그림자도 추가하기로 한다.
	public RectTransform growthAlarmRootTransform;
	public RectTransform growthShadowAlarmRootTransform;
	public RectTransform potentialAlarmRootTransform;
	public RectTransform potentialShadowAlarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		// 항상 최초 시작에는 현재 메뉴를 어디까지 보여줄지 체크해본다.
		// 이후 초월할때마다 한번씩 갱신하면 될거다.
		int highestLimitBreakLevel = 0;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
			highestLimitBreakLevel = Mathf.Max(highestLimitBreakLevel, PlayerData.instance.listCharacterData[i].limitBreakLevel);
		RefreshOpenMenuSlot(highestLimitBreakLevel);

		// 항상 게임을 처음 켤땐 0번탭을 보게 해준다.
		OnValueChangedToggle(0);
	}

	int _highestLimitBreakLevel = -1;
	public void RefreshOpenMenuSlot(int newLimitBreakLevel)
	{
		if (newLimitBreakLevel <= _highestLimitBreakLevel)
			return;

		_highestLimitBreakLevel = newLimitBreakLevel;
		int openMenuIndex = 0;
		switch (_highestLimitBreakLevel)
		{
			case 0: openMenuIndex = -1; break;
			case 1: openMenuIndex = 1; break;
			case 2: openMenuIndex = 2; break;
			case 3: openMenuIndex = 3; break;
		}
		// 결과가 나오면 우선 그에 맞춰서 숨길거 숨긴다.
		for (int i = 0; i < menuButtonList.Length; ++i)
			menuButtonList[i].gameObject.SetActive(i <= openMenuIndex);
	}

	void OnEnable()
	{
		RefreshInfo();

		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(false);
		}

		StackCanvas.Pop(gameObject);
	}

	void RefreshInfo()
	{
		bool contains = PlayerData.instance.ContainsActor(CharacterListCanvas.instance.selectedActorId);

		// 원래라면 현재 가지고 있는 캐릭터들의 초월 상태를 보고 거기에 맞춰서 메뉴를 오픈해야하지만
		// 인벤에 없는 캐릭을 보는거라면 무조건 성장탭으로 바꾸고 탭을 숨겨야한다. 당연히 원래 캐릭으로 돌아갈때는 하단 탭들을 다시 보여줘야한다.
		// 그래서 아예 버튼 루트를 캐릭터 존재 여부에 따라서 컨트롤 하기로 한다.
		menuRootObject.SetActive(contains);
		if (contains == false)
			OnValueChangedToggle(0);

		// 별도의 카메라 스페이스 캔버스로 되어있기 때문에 들어올때마다 마지막 탭으로 켜줘야한다.
		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(_lastIndex == i);
		}

		RefreshAlarmObjectList();
	}

	public void RefreshAlarmObjectList()
	{
		CharacterData characterData = PlayerData.instance.GetCharacterData(CharacterListCanvas.instance.selectedActorId);
		if (characterData == null)
		{
			AlarmObject.Hide(growthAlarmRootTransform);
			AlarmObject.Hide(growthShadowAlarmRootTransform);
			AlarmObject.Hide(potentialAlarmRootTransform);
			AlarmObject.Hide(potentialShadowAlarmRootTransform);
			return;
		}

		if (characterData.IsPlusAlarmState())
		{
			AlarmObject.Show(growthAlarmRootTransform, false, false, true);
			AlarmObject.Show(growthShadowAlarmRootTransform, false, false, true, true);
		}
		if (characterData.IsAlarmState())
		{
			AlarmObject.Show(potentialAlarmRootTransform);
			AlarmObject.Show(potentialAlarmRootTransform, true, false, false, true);
		}
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();

		if (_okAction != null)
		{
			_okAction();
			_okAction = null;
		}
	}

	public void OnClickHomeButton()
	{
		// 현재 상태에 따라
		LobbyCanvas.Home();

		_okAction = null;
	}



	#region Menu Button
	public void OnClickMenuButton1() { OnValueChangedToggle(0); }
	public void OnClickMenuButton2() { OnValueChangedToggle(1); }
	public void OnClickMenuButton3() { OnValueChangedToggle(2); }
	public void OnClickMenuButton4() { OnValueChangedToggle(3); }

	List<Transform> _listMenuTransform = new List<Transform>();
	int _lastIndex = -1;
	void OnValueChangedToggle(int index)
	{
		if (index == _lastIndex)
			return;

		if (_listMenuTransform.Count == 0)
		{
			for (int i = 0; i < menuButtonList.Length; ++i)
				_listMenuTransform.Add(null);
		}

		if (_listMenuTransform[index] == null && innerMenuPrefabList[index] != null)
		{
			GameObject newObject = Instantiate<GameObject>(innerMenuPrefabList[index]);
			_listMenuTransform[index] = newObject.transform;
		}

		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			menuButtonList[i].isOn = (index == i);
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(index == i);
		}

		_lastIndex = index;
	}
	#endregion






	public void OnDragRect(BaseEventData baseEventData)
	{
		CharacterListCanvas.instance.OnDragRect(baseEventData);
	}

	public void OnClickDetailButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("CharacterInfoDetailCanvas", null);
	}


	#region DailyShopCharacterConfirmCanvas
	System.Action _okAction;
	public void ReserveBackButton(System.Action okAction)
	{
		_okAction = okAction;
	}
	#endregion
}