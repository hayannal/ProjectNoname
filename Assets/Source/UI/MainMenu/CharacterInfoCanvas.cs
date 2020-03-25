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

	void Awake()
	{
		instance = this;
	}

	int _openMenuIndex = -1;
	void Start()
	{
		// 항상 최초 시작에는 현재 메뉴를 어디까지 보여줄지 체크해본다.
		// 이후 초월할때마다 한번씩 갱신하면 될거다.
		bool result = ContentsManager.IsOpen(ContentsManager.eOpenContensByCharacter.Transcendence, false);
		_openMenuIndex = 2;

		// 결과가 나오면 우선 그에 맞춰서 숨길거 숨긴다.
		for (int i = 0; i < menuButtonList.Length; ++i)
			menuButtonList[i].gameObject.SetActive(i <= _openMenuIndex);

		// 항상 게임을 처음 켤땐 0번탭을 보게 해준다.
		OnValueChangedToggle(0);
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
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		// 현재 상태에 따라
		StackCanvas.Home();
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
		TooltipCanvas.Show(false, TooltipCanvas.eDirection.CharacterInfo, "", 0.0f, null, Vector2.zero);

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
}