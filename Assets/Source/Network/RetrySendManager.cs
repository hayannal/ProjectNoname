using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RetrySendManager : MonoBehaviour {

	public static RetrySendManager instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("RetrySendManager")).AddComponent<RetrySendManager>();
			return _instance;
		}
	}
	static RetrySendManager _instance = null;

	// 거의 대부분 다
	// 하나의 독립적인 패킷 주고받을때 실패했을 경우 재전송하는거지
	// 로그인시 여러 묶음으로 된 정보전송시 실패했을때 재전송하는게 아니다.
	// 그러니 cachedAction은 하나만 유지하도록 한다.
	Action _cachedAction;

	// 패킷 전송시 화면 차단도 대행
	bool _showWaitingNetworkCanvas;

	// Retry가 하는 일은 다음과 같다
	// 1. 우선 패킷을 보낸다. 빨리 끝날줄 알았는데 지체되면 네트워크 딜레이 팝업창을 조금 늦게 띄운다.
	// 2. 성공하면 원래대로 성공콜백이 호출될거다.
	// 3. 실패하면 원대대로 실패콜백이 호출될거다.
	// 4. 그런데 실패시엔 하나 더 있다. 메세지박스를 띄우고 재시도 하겠느냐고 묻는다.
	// 5. 재시작 or 다시 시도 누르면
	// 5-1. 재시작은 그냥 메인씬 복귀
	// 5-2. 다시 시도 누르면 아까 보냈던 패킷을 다시 보내고 결과에 따라 처리도 원래대로 해야한다.
	public void RequestAction(Action requestAction, bool showWaitingNetworkCanvas)
	{
		_cachedAction = requestAction;
		_showWaitingNetworkCanvas = showWaitingNetworkCanvas;

		if (_showWaitingNetworkCanvas)
			WaitingNetworkCanvas.Show(true);

		if (_cachedAction != null)
			_cachedAction.Invoke();
	}

	public void OnSuccess()
	{
		if (_showWaitingNetworkCanvas)
			WaitingNetworkCanvas.Show(false);

		_cachedAction = null;
		_showWaitingNetworkCanvas = false;
	}

	public void OnFailure()
	{
		if (_showWaitingNetworkCanvas)
			WaitingNetworkCanvas.Show(false);

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_Reconnect"), () =>
		{
			if (_showWaitingNetworkCanvas)
				WaitingNetworkCanvas.Show(true);

			if (_cachedAction != null)
				_cachedAction.Invoke();
		}, () =>
		{
			// To Main Scene?
			SceneManager.LoadScene(0);
		});
	}

	#region Multi Request
	List<Action> _listRequestAction;
	public void RequestActionList(List<Action> listRequestAction, bool showWaitingNetworkCanvas)
	{
		_listRequestAction = listRequestAction;
		_showWaitingNetworkCanvas = showWaitingNetworkCanvas;

		if (_showWaitingNetworkCanvas)
			WaitingNetworkCanvas.Show(true);

		_listFailureIndex.Clear();
		for (int i = 0; i < _listRequestAction.Count; ++i)
			_listRequestAction[i].Invoke();
	}

	public void OnSuccessForList(int index)
	{
		if (index < _listRequestAction.Count)
			_listRequestAction[index] = null;

		bool allSuccess = true;
		for (int i = 0; i < _listRequestAction.Count; ++i)
		{
			if (_listRequestAction[i] != null)
			{
				allSuccess = false;
				break;
			}
		}
		if (allSuccess == false)
			return;

		if (_showWaitingNetworkCanvas)
			WaitingNetworkCanvas.Show(false);

		_showWaitingNetworkCanvas = false;
		_listFailureIndex.Clear();
		_listRequestAction = null;
	}

	List<int> _listFailureIndex = new List<int>();
	public void OnFailureForList(int index)
	{
		if (_showWaitingNetworkCanvas)
			WaitingNetworkCanvas.Show(false);

		// 여기가 핵심인데 이미 어느 누군가 실패한 상태에서 리스트에 들어있는 또다른 항목이 실패했을때
		// 이미 YesNoCanvas는 띄워져있을테니 그냥 호출하면 덮어버리게 된다.
		// 그러니 카운트로 분기타서 창을 띄울지 실패리스트에 넣을지 처리한다.
		if (_listFailureIndex.Count == 0)
		{
			_listFailureIndex.Add(index);
			YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_Reconnect"), OnClickRetryForList, () =>
			{
				// To Main Scene?
				SceneManager.LoadScene(0);
			});
		}
		else
		{
			if (_listFailureIndex.Contains(index) == false)
				_listFailureIndex.Add(index);
		}
	}

	void OnClickRetryForList()
	{
		if (_showWaitingNetworkCanvas)
			WaitingNetworkCanvas.Show(true);

		for (int i = 0; i < _listFailureIndex.Count; ++i)
		{
			int index = _listFailureIndex[i];
			if (index < _listRequestAction.Count && _listRequestAction[index] != null)
				_listRequestAction[index].Invoke();
		}
		_listFailureIndex.Clear();
	}
	#endregion
}