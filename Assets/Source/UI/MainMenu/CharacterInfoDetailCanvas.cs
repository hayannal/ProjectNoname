using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterInfoDetailCanvas : MonoBehaviour
{
	public static CharacterInfoDetailCanvas instance;

	public Transform infoCameraTransform;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
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


	public void OnDragRect(BaseEventData baseEventData)
	{
		CharacterListCanvas.instance.OnDragRect(baseEventData);
	}
}