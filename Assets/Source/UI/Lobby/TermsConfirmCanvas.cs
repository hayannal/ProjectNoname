using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TermsConfirmCanvas : MonoBehaviour
{
	public static TermsConfirmCanvas instance;

	public Text termsText;
	public Text policyText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		termsText.SetLocalizedText(UIString.instance.GetString("GameUI_TermsOfService"));
		termsText.fontStyle = FontStyle.Italic;
		policyText.SetLocalizedText(UIString.instance.GetString("GameUI_PrivacyPolicy"));
		policyText.fontStyle = FontStyle.Italic;
	}

	System.Action _okAction;
	public void ShowCanvas(System.Action okAction)
	{
		_okAction = okAction;
	}

	public void OnClickTermsTextButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("TermsCanvas", () =>
		{
			TermsCanvas.instance.RefreshInfo(true, false);
		});
	}

	public void OnClickPolicyTextButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("TermsCanvas", () =>
		{
			TermsCanvas.instance.RefreshInfo(false, false);
		});
	}

	public void OnClickOkButton()
	{
		// 패킷 처리 후 문제가 없다면
		PlayFabApiManager.instance.RequestConfirmTerms(() =>
		{
			gameObject.SetActive(false);
			if (_okAction != null)
				_okAction();
		});
	}

	public void OnClickBackgroundButton()
	{
		// nothing
	}
}