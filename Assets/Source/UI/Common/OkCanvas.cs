using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OkCanvas : MonoBehaviour
{
	public static OkCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(CommonCanvasGroup.instance.okCanvasPrefab).GetComponent<OkCanvas>();
			}
			return _instance;
		}
	}
	static OkCanvas _instance = null;

	public Text titleText;
	public Text messageText;

	System.Action _okAction;

	Canvas _canvas;
	int _defaultSortOrder;
	void Awake()
	{
		_canvas = GetComponent<Canvas>();
		_defaultSortOrder = _canvas.sortingOrder;
	}

	void OnDisable()
	{
		if (_canvas.sortingOrder != _defaultSortOrder)
			_canvas.sortingOrder = _defaultSortOrder;
	}

	public void ShowCanvas(bool show, string title, string message, System.Action okAction = null, int overrideSortOrder = -1)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		titleText.SetLocalizedText(title);
		messageText.SetLocalizedText(message);
		_okAction = okAction;

		if (overrideSortOrder != -1)
			_canvas.sortingOrder = overrideSortOrder;
	}

	public void OnClickOkButton()
	{
		gameObject.SetActive(false);
		if (_okAction != null)
			_okAction();
	}
}