using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeButton : MonoBehaviour
{
	Button _button;

	void OnEnable()
	{
		HomeButton.Push(this);
	}

	void OnDisable()
	{
		HomeButton.Pop(this);
	}

	// Use this for initialization
	void Start()
	{
		_button = GetComponent<Button>();
		if (_button != null)
		{
			_button.onClick.AddListener(OnClick);
		}
	}

	void OnClick()
	{
		for (int i = s_homeButtonList.Count - 1; i >= 0; --i)
		{
			if (s_homeButtonList[i] == null)
			{
				s_homeButtonList.RemoveAt(i);
				continue;
			}

			// 여기서 둘중 하나로 구현할 수 있는데..
			// 하나는 백버튼처럼 자기 창을 닫게 해두고 Invoke하는 것과
			// 다른 하나는 자신이 닫을 창을 인스펙터에 등록해두고 여기서 직접 SetActive(false) 하는건데..
			// 당연히 후자가 더 이상해보여서 전자형태로 만들어둔다.
			// 전자 형태로 가면서 자기 스스로는 이미 클릭했으니 처리안하고 삭제하고 나머지들을 처리한다.
			if (s_homeButtonList[i] == this)
			{
				s_homeButtonList.RemoveAt(i);
				continue;
			}
			if (_button.interactable)
				_button.onClick.Invoke();
		}
#if UNITY_EDITOR
		// 모든 홈키가 실행되었다면 카운트가 0이어야한다.
		if (s_homeButtonList.Count != 0)
		{
			Debug.LogError("HomeButton Invalid Data : List Count is zero.");
		}
#endif
	}


#region Manager
	static List<HomeButton> s_homeButtonList;
	public static void Push(HomeButton homeButton)
	{
		if (s_homeButtonList == null)
			s_homeButtonList = new List<HomeButton>();

		s_homeButtonList.Add(homeButton);
	}

	public static void Pop(HomeButton homeButton)
	{
		if (s_homeButtonList == null)
			return;

		s_homeButtonList.Remove(homeButton);
	}
#endregion
}