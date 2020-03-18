using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackCanvas : MonoBehaviour
{
	static StackCanvas instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("StackCanvas")).AddComponent<StackCanvas>();
			return _instance;
		}
	}
	static StackCanvas _instance = null;

	// 스택 최상단 바로 아래 있어서 복구하는 상황엔 true를 리턴한다.
	public static bool Push(GameObject canvasObject, bool forceShow = false, Action optionalPushAction = null, Action optionalPopAction = null)
	{
		return instance.InternalPush(canvasObject, forceShow, optionalPushAction, optionalPopAction);
	}

	// 뭔가 다른 창이 스택되서 가려져야하는 경우엔 true를 리턴한다.
	public static bool Pop(GameObject canvasObject)
	{
		if (_instance == null)
			return false;
		return _instance.InternalPop(canvasObject);
	}

	public static void Back()
	{
		if (_instance == null)
			return;
		_instance.InternalBack();
	}

	public static void Home()
	{
		if (_instance == null)
			return;
		_instance.InternalHome();
	}

	public static bool IsInStack(GameObject canvasObject, bool exceptFirst = true)
	{
		if (_instance == null)
			return false;
		return _instance.InternalIsInStack(canvasObject, exceptFirst);
	}

	// 스택구조는 크게 둘로 나뉜다.
	// 현재창이 보이면서 그 위에 새로운 창이 쌓이는 구조. 혹은 현재창은 숨겨놓고 새창이 뜨는 구조.
	// 전자는 사실 이 구조를 안써도 되는데 써야되는 순간이 있다. 바로 DotMainMenu처럼 맨 위에 뜰때만 환경을 어둡게 처리하는 경우다.
	class CanvasStackInfo
	{
		public GameObject canvasObject;
		public bool forceShow;	// 원래 대부분의 창들은 다른 임의의 창이 자기위로 스택될때 자동으로 하이드 된다. 그러나 일부 메뉴들은 그대로 남는다. 대표적으로 DotMainMenu
		public Action optionalPushAction;
		public Action optionalPopAction;
	}
	Stack<CanvasStackInfo> _stackCanvas = new Stack<CanvasStackInfo>();

	public bool InternalPush(GameObject canvasObject, bool forceShow, Action optionalPushAction = null, Action optionalPopAction = null)
	{
		if (_processHome)
			return false;

		CanvasStackInfo prevInfo = null;
		if (_stackCanvas.Count > 0)
			prevInfo = _stackCanvas.Peek();

		// 최상단에 있던 캔버스가 팝될때 그 아래껄 켤텐데 이땐 이미 최상단에 해당 오브젝트가 있을거다. 이럴땐 그냥 리턴하면 된다.
		if (prevInfo != null && prevInfo.canvasObject == canvasObject && prevInfo.forceShow == false)
			return true;

		// 새로운 캔버스를 추가로 스택하는거라면
		CanvasStackInfo stackInfo = new CanvasStackInfo();
		stackInfo.canvasObject = canvasObject;
		stackInfo.forceShow = forceShow;
		stackInfo.optionalPushAction = optionalPushAction;
		stackInfo.optionalPopAction = optionalPopAction;
		_stackCanvas.Push(stackInfo);

		// Push보다 이걸 먼저 수행해버리면 아래 Pop의 리턴값을 구별해낼 수 없게되버려서 꼭 Push보다 아래 호출되어야한다.
		// 왜냐면 이걸 먼저 수행할 경우
		// 새 창이 스택되면서 꺼지는 경우와 그냥 끄려고 하는 경우 둘다 first item이기 때문에 peek로 얻어보면 구분이 안된다.
		// 그래서 이렇게 Push후 disable처리하는거다.
		if (prevInfo != null)
		{
			if (prevInfo.forceShow && optionalPopAction != null)
				prevInfo.optionalPopAction.Invoke();
			if (prevInfo.forceShow == false && prevInfo.canvasObject.activeSelf)
				prevInfo.canvasObject.SetActive(false);
		}
		return false;
	}

	public bool InternalPop(GameObject canvasObject)
	{
		if (_processHome)
			return false;

		if (_stackCanvas.Count == 0)
			return false;

		CanvasStackInfo info = _stackCanvas.Peek();
		if (info == null)
			return false;

		if (info.canvasObject != canvasObject)
		{
			if (info.forceShow == false)
			{
				// 바로 이전창인지 검사해서 stacked으로 인해 disable되는지 판단한다.
				int index = 0;
				bool secondItem = false;
				Stack<CanvasStackInfo>.Enumerator e = _stackCanvas.GetEnumerator();
				while (e.MoveNext())
				{
					if (e.Current == null)
						continue;
					if (index == 1)
					{
						if (e.Current.canvasObject == canvasObject)
						{
							secondItem = true;
							break;
						}
					}
					++index;
				}
				if (secondItem)
					return true;
			}
			return false;
		}

		// 진짜 최상단 캔버스가 pop되는거라면
		_stackCanvas.Pop();

		CanvasStackInfo currentInfo = null;
		if (_stackCanvas.Count > 0)
			currentInfo = _stackCanvas.Peek();

		// 그 아래 창을 얻어와서 켜주는 처리를 한다.
		if (currentInfo != null)
		{
			if (currentInfo.forceShow && currentInfo.optionalPushAction != null)
				currentInfo.optionalPushAction.Invoke();
			if (currentInfo.forceShow == false && currentInfo.canvasObject.activeSelf == false)
				currentInfo.canvasObject.SetActive(true);
		}
		return false;
	}

	public void InternalBack()
	{
		// 잘하면 필요없을지도
	}

	bool _processHome;
	public void InternalHome()
	{
		_processHome = true;
		int index = 0;
		Stack<CanvasStackInfo>.Enumerator e = _stackCanvas.GetEnumerator();
		while (e.MoveNext())
		{
			if (e.Current == null)
				continue;
			if (index != 0)
			{
				if (e.Current.forceShow && e.Current.optionalPushAction != null)
					e.Current.optionalPushAction.Invoke();
				if (e.Current.forceShow == false && e.Current.optionalPopAction != null)
					e.Current.optionalPopAction.Invoke();
			}
			e.Current.canvasObject.SetActive(false);
			++index;
		}
		_stackCanvas.Clear();
		_processHome = false;
	}

	public bool InternalIsInStack(GameObject canvasObject, bool exceptFirst)
	{
		bool find = false;
		int index = 0;
		Stack<CanvasStackInfo>.Enumerator e = _stackCanvas.GetEnumerator();
		while (e.MoveNext())
		{
			if (e.Current == null)
				continue;
			if (index == 0 && exceptFirst)
			{
				++index;
				continue;
			}
			if (e.Current.canvasObject == canvasObject)
			{
				find = true;
				break;
			}
			++index;
		}
		return find;
	}
}