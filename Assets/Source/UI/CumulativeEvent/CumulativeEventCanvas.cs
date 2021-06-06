using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CumulativeEventCanvas : MonoBehaviour
{
	public static CumulativeEventCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public GameObject[] innerMenuPrefabList;
	public Transform innerMenuRootTransform;
	public MenuButton[] menuButtonList;
	public GridLayoutGroup gridLayoutGroup;

	/*
	// 5개의 메뉴중에 3개만 알람을 쓰니 따로 관리하도록 한다. 그런데 그림자가 있어야 예뻐져서 각각 그림자도 추가하기로 한다.
	public RectTransform growthAlarmRootTransform;
	public RectTransform growthShadowAlarmRootTransform;
	public RectTransform transcendAlarmRootTransform;
	public RectTransform transcendShadowAlarmRootTransform;
	public RectTransform potentialAlarmRootTransform;
	public RectTransform potentialShadowAlarmRootTransform;
	*/

	void Awake()
	{
		instance = this;
	}

	bool _started = false;
	void Start()
	{
		// 최초 시작에는 활성화된 이벤트중에 가장 왼쪽에 있는걸 보여주면 된다.
		int openIndex = -1;
		for (int i = (int)CumulativeEventData.eEventType.Amount - 1; i >= 0; --i)
		{
			if (CumulativeEventData.instance.IsActiveEvent((CumulativeEventData.eEventType)i))
				openIndex = i;
		}
		RefreshOpenTabSlot();
		OnValueChangedToggle(openIndex);

		_started = true;
	}

	public void RefreshOpenTabSlot()
	{
		int activeCount = CumulativeEventData.instance.GetActiveEventCount();
		if (activeCount <= 1)
		{
			for (int i = 0; i < menuButtonList.Length; ++i)
				menuButtonList[i].gameObject.SetActive(false);
			return;
		}

		for (int i = 0; i < menuButtonList.Length; ++i)
		{
			bool active = CumulativeEventData.instance.IsActiveEvent((CumulativeEventData.eEventType)i);
			menuButtonList[i].gameObject.SetActive(active);
		}

		int space = 0;
		switch (activeCount)
		{
			case 2: space = 20 * 4; break;
			case 3: space = 20 * 3; break;
			case 4: space = 20 * 2; break;
			case 5: space = 20; break;
			case 6:
			case 7:
			case 8: space = 0; break;
		}
		gridLayoutGroup.spacing = new Vector2(space, 0);
	}

	void OnEnable()
	{
		// 탭을 유저가 바꾸지 않았는데 마음대로 바꾸는건 별로인거 같아서 평소에는 아무것도 안한다.
		// 그렇지만 다시 열려고 할때 현재 탭이 보이지 않아야하는 상황이라면
		// 적절한 탭으로 전환하기로 한다.
		if (_started)
		{
			if (_lastIndex != -1 && CumulativeEventData.instance.IsActiveEvent((CumulativeEventData.eEventType)_lastIndex) == false)
			{
				int openIndex = -1;
				for (int i = (int)CumulativeEventData.eEventType.Amount - 1; i >= 0; --i)
				{
					if (CumulativeEventData.instance.IsActiveEvent((CumulativeEventData.eEventType)i))
						openIndex = i;
				}
				OnValueChangedToggle(openIndex);
			}
		}

		RefreshAlarmObjectList();

		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		/*
		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(false);
		}
		*/

		StackCanvas.Pop(gameObject);
	}

	public void RefreshAlarmObjectList()
	{
		/*
		CharacterData characterData = PlayerData.instance.GetCharacterData(CharacterListCanvas.instance.selectedActorId);
		if (characterData != null && characterData.IsPlusAlarmState())
		{
			AlarmObject.Show(growthAlarmRootTransform, false, false, true);
			AlarmObject.Show(growthShadowAlarmRootTransform, false, false, true, true);
		}
		else
		{
			AlarmObject.Hide(growthAlarmRootTransform);
			AlarmObject.Hide(growthShadowAlarmRootTransform);
		}

		if (characterData != null && characterData.IsTranscendAlarmState())
		{
			AlarmObject.Show(transcendAlarmRootTransform);
			AlarmObject.Show(transcendShadowAlarmRootTransform, true, false, false, true);
		}
		else
		{
			AlarmObject.Hide(transcendAlarmRootTransform);
			AlarmObject.Hide(transcendShadowAlarmRootTransform);
		}

		if (characterData != null && characterData.IsPotentialAlarmState())
		{
			AlarmObject.Show(potentialAlarmRootTransform);
			AlarmObject.Show(potentialShadowAlarmRootTransform, true, false, false, true);
		}
		else
		{
			AlarmObject.Hide(potentialAlarmRootTransform);
			AlarmObject.Hide(potentialShadowAlarmRootTransform);
		}
		*/
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		// 현재 상태에 따라
		LobbyCanvas.Home();
	}


	

	#region Menu Button
	public void OnClickMenuButton1() { OnValueChangedToggle(0); }
	public void OnClickMenuButton2() { OnValueChangedToggle(1); }
	public void OnClickMenuButton3() { OnValueChangedToggle(2); }
	public void OnClickMenuButton4() { OnValueChangedToggle(3); }
	public void OnClickMenuButton5() { OnValueChangedToggle(4); }

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
			GameObject newObject = Instantiate<GameObject>(innerMenuPrefabList[index], innerMenuRootTransform);
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

		switch (index)
		{
			case 0:
				//EquipEnhanceCanvas.instance.RefreshInfo(_equipData);
				break;
			case 1:
				//EquipOptionCanvas.instance.RefreshInfo(_equipData);
				break;
		}
	}
	#endregion
}