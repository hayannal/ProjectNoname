using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSound : MonoBehaviour
{
	public string SFXName = "Click";

	Button _button;
	Toggle _toggle;

	void Start()
	{
		_button = GetComponent<Button>();
		if (_button != null)
		{
			_button.onClick.AddListener(OnClick);
		}
		_toggle = GetComponent<Toggle>();
		if (_toggle != null)
		{
			_toggle.onValueChanged.AddListener(OnToggle);
		}
	}

	void OnClick()
	{
		PlaySound();
	}

	void OnToggle(bool toggle)
	{
		if (toggle)
			PlaySound();
	}

	void PlaySound()
	{
		if (_button != null && _button.interactable == false)
			return;
		if (_toggle != null && _toggle.interactable == false)
			return;

		SoundManager.instance.PlaySFX(SFXName);
	}
}