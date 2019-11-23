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

	void OnEnable()
	{
		animator.Play("Modal Dialog In");
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
}