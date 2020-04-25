using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventInputLockCanvas : MonoBehaviour
{
	public static EventInputLockCanvas instance;

	void Awake()
	{
		instance = this;
	}

	public void OnClickBackgroundButton()
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
			TitleCanvas.instance.FadeTitle();

		EventManager.instance.OnClickScreen();
	}
}