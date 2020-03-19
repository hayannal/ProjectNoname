using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ExperienceCanvas : MonoBehaviour
{
	public static ExperienceCanvas instance;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		ChangeExperienceMode();
		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		StackCanvas.Pop(gameObject);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	void ChangeExperienceMode()
	{
		CharacterListCanvas.instance.ChangeExperience();
	}
}