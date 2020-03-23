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
		// 서버통신 하고 닫아야할때가 많으니 여기선 바로 닫지 않도록 하려고 했는데 안닫으니 반응이 느려보인다. 닫고 처리한다.
		gameObject.SetActive(false);
		if (_okAction != null)
			_okAction();
	}
}