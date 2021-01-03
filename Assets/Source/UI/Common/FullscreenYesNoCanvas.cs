using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FullscreenYesNoCanvas : MonoBehaviour
{
	public static FullscreenYesNoCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(CommonCanvasGroup.instance.fullscreenYesNoCanvasPrefab).GetComponent<FullscreenYesNoCanvas>();
			}
			return _instance;
		}
	}
	static FullscreenYesNoCanvas _instance = null;

	public Animator animator;
	public CanvasGroup rootCanvasGroup;
	public Text titleText;
	public Text messageText;

	System.Action _yesAction;
	System.Action _noAction;

	public static bool IsShow()
	{
		// instance접근 없이 보여지고 있는지 판단하기 위해 나중에 추가된 함수
		if (_instance == null)
			return false;
		if (_instance.gameObject == null)
			return false;
		return _instance.gameObject.activeSelf;
	}

	void OnEnable()
	{
		animator.Play("Modal Dialog In");
		rootCanvasGroup.alpha = 0.0f;
		_needRootCanvasGroupZeroAlpha = true;
	}

	void OnDisable()
	{
		_needCheckRootCanvasGroupAlpha = false;
	}

	public void ShowCanvas(bool show, string title, string message, System.Action yesAction, System.Action noAction = null)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		titleText.SetLocalizedText(title);
		messageText.SetLocalizedText(message);
		_yesAction = yesAction;
		_noAction = noAction;
	}

	public void OnClickYesButton()
	{
		//gameObject.SetActive(false);
		if (_yesAction != null)
			_yesAction();
	}

	public void OnClickNoButton()
	{
		//gameObject.SetActive(false);
		if (_noAction != null)
			_noAction();

		_needCheckRootCanvasGroupAlpha = true;
	}

	bool _needCheckRootCanvasGroupAlpha = false;
	private void Update()
	{
		if (_needCheckRootCanvasGroupAlpha == false)
			return;

		if (rootCanvasGroup.alpha == 0.0f)
		{
			_needCheckRootCanvasGroupAlpha = false;
			gameObject.SetActive(false);
		}
	}

	// 게이트 필라 칠때 나오게 되어있는데
	// 이게 OnCollisionEnter도 있고 MecanimEventBase.OnStateUpdate도 있고 캐싱이냐 최초 생성이냐에 따라 다른 결과가 나온다.
	// 모든 상황에서 animator 자동으로 실행될때 alpha값을 0에서부터 시작하려면
	// 렌더링 하기 직전 LateUpdate에서 alpha를 재설정 하는게 제일 안전해서 이렇게 예외처리 하기로 한다.
	bool _needRootCanvasGroupZeroAlpha = false;
	private void LateUpdate()
	{
		if (_needRootCanvasGroupZeroAlpha)
		{
			rootCanvasGroup.alpha = 0.0f;
			_needRootCanvasGroupZeroAlpha = false;
		}
	}
}