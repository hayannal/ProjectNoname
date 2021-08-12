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

	// 캐릭터메뉴와 달리 9개중에 image이벤트 빼고 다 보상이 있기때문에 배열로 처리해본다.
	public RectTransform[] tabAlarmRootTransformList;
	public RectTransform[] tabShadowAlarmRootTransformList;

	void Awake()
	{
		instance = this;
	}

	bool _started = false;
	void Start()
	{
		// 최초 시작에는 받을 수 있는 이벤트중 가장 왼쪽에 있는걸 보여주면 된다.
		// 받을 수 있는게 없다면 활성화된 이벤트중에 가장 왼쪽에 있는걸 보여주면 된다.
		int openIndex = -1;
		for (int i = 0; i < (int)CumulativeEventData.eEventType.Amount; ++i)
		{
			if (CumulativeEventData.instance.IsReceivableEvent((CumulativeEventData.eEventType)i))
			{
				openIndex = i;
				break;
			}
		}
		if (openIndex == -1)
		{
			for (int i = 0; i < (int)CumulativeEventData.eEventType.Amount; ++i)
			{
				if (CumulativeEventData.instance.IsActiveEvent((CumulativeEventData.eEventType)i))
				{
					openIndex = i;
					break;
				}
			}
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
			case 2: space = 70; break;
			case 3: space = 60; break;
			case 4: space = 50; break;
			case 5: space = 40; break;
			case 6: space = 25; break;
			case 7: space = 15; break;
			case 8: space = 5; break;
			case 9: space = 0; break;
			case 10: space = 0; break;
			case 11: space = 0; break;
			case 12: space = 0; break;
		}
		gridLayoutGroup.spacing = new Vector2(space, 0);

		// 탭 구성이 바뀌었을수도 있으니 Alarm도 다시 체크해본다.
		RefreshAlarmObjectList();
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

		StackCanvas.Pop(gameObject);
	}

	public void RefreshAlarmObjectList()
	{
		for (int i = 0; i < tabAlarmRootTransformList.Length; ++i)
		{
			if (menuButtonList[i].gameObject.activeSelf == false)
				continue;

			bool show = CumulativeEventData.instance.IsReceivableEvent((CumulativeEventData.eEventType)i);
			RefreshAlarmObject(show, i);
		}
	}

	public void RefreshAlarmObject(CumulativeEventData.eEventType eventType, bool refreshEventBoard = true)
	{
		bool show = CumulativeEventData.instance.IsReceivableEvent(eventType);
		RefreshAlarmObject(show, (int)eventType);
		if (refreshEventBoard)
			EventBoard.instance.RefreshAlarmObject(eventType, show);
	}

	void RefreshAlarmObject(bool show, int buttonIndex)
	{
		if (tabAlarmRootTransformList[buttonIndex] == null || tabShadowAlarmRootTransformList[buttonIndex] == null)
			return;

		if (show)
		{
			AlarmObject.Show(tabAlarmRootTransformList[buttonIndex]);
			AlarmObject.Show(tabShadowAlarmRootTransformList[buttonIndex], true, false, false, true);
		}
		else
		{
			AlarmObject.Hide(tabAlarmRootTransformList[buttonIndex]);
			AlarmObject.Hide(tabShadowAlarmRootTransformList[buttonIndex]);
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
		LobbyCanvas.Home();
	}


	

	#region Menu Button
	public void OnClickMenuButton1() { OnValueChangedToggle(0); }
	public void OnClickMenuButton2() { OnValueChangedToggle(1); }
	public void OnClickMenuButton3() { OnValueChangedToggle(2); }
	public void OnClickMenuButton4() { OnValueChangedToggle(3); }
	public void OnClickMenuButton5() { OnValueChangedToggle(4); }
	public void OnClickMenuButton6() { OnValueChangedToggle(5); }
	public void OnClickMenuButton7() { OnValueChangedToggle(6); }
	public void OnClickMenuButton8() { OnValueChangedToggle(7); }
	public void OnClickMenuButton9() { OnValueChangedToggle(8); }
	public void OnClickMenuButton10() { OnValueChangedToggle(9); }
	public void OnClickMenuButton11() { OnValueChangedToggle(10); }
	public void OnClickMenuButton12() { OnValueChangedToggle(11); }

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
	}
	#endregion
}