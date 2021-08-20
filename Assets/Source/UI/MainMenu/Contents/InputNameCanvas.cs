using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputNameCanvas : MonoBehaviour
{
	public static InputNameCanvas instance;

	public InputField nameInputField;
	public Text nameText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		RefreshText();
	}

	void RefreshText()
	{
		// 폰트 적용을 위해 호출해야한다.
		nameText.font = UIString.instance.GetLocalizedFont();
		nameInputField.text = "";
	}



	public void OnClickConfirmButton()
	{
		if (nameInputField.text == "")
			return;

		if (nameInputField.text.Length < 3)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("RankUI_NameTooShort"), 2.0f);
			return;
		}
		if (nameInputField.text.Length > 25)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("RankUI_NameTooSLong"), 2.0f);
			return;
		}
		string noSpaceText = nameInputField.text.Replace(" ", "");
		if (string.IsNullOrEmpty(noSpaceText))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("RankUI_NameOnlySpace"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestRegisterName(nameInputField.text, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("RankUI_NameComplete"), 2.0f);
			gameObject.SetActive(false);
			RankingCanvas.instance.RefreshGrid();
			RankingCanvas.instance.RefreshInfo();
		}, (error) =>
		{
			if (error.Error == PlayFab.PlayFabErrorCode.InvalidParams)
			{

			}
			else if (error.Error == PlayFab.PlayFabErrorCode.NameNotAvailable)
			{
				// duplicate
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("RankUI_ChangeName"), 2.0f);
			}
		});
	}
}