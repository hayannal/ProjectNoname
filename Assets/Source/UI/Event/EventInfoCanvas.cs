using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventInfoCanvas : MonoBehaviour
{
	public static EventInfoCanvas instance;

	public Text subTitleText;
	public Text messageText;

	System.Action _okAction;

	void Awake()
	{
		instance = this;
	}

	public void ShowCanvas(bool show, string title, string message, string tooltip, System.Action okAction = null)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		subTitleText.SetLocalizedText(title);
		messageText.SetLocalizedText(message);
		_tooltip = tooltip;
		_okAction = okAction;
	}

	string _tooltip;
	public void OnClickMoreButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, _tooltip, 300, subTitleText.transform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickExitButton()
	{
		gameObject.SetActive(false);
		if (_okAction != null)
			_okAction();
	}
}