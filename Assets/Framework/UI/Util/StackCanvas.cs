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

		CanvasStackInfo stackInfo = new CanvasStackInfo();
		stackInfo.canvasObject = canvasObject;
		stackInfo.forceShow = forceShow;
		stackInfo.optionalPushAction = optionalPushAction;
		stackInfo.optionalPopAction = optionalPopAction;
		_stackCanvas.Push(stackInfo);

		if (prevInfo != null)
		{
			if (prevInfo.optionalPopAction != null)
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
			return true;

		_stackCanvas.Pop();

		CanvasStackInfo currentInfo = null;
		if (_stackCanvas.Count > 0)
			currentInfo = _stackCanvas.Peek();

		if (currentInfo != null)
		{
			if (currentInfo.optionalPushAction != null)
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
			if (index != 0 && e.Current.optionalPushAction != null)
				e.Current.optionalPushAction.Invoke();
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