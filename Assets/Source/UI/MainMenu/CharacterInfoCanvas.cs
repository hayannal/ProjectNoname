using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterInfoCanvas : MonoBehaviour
{
	public static CharacterInfoCanvas instance;

	public GameObject characterInfoInnerCanvasPrefab;
	public CurrencySmallInfo currencySmallInfo;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		Instantiate<GameObject>(characterInfoInnerCanvasPrefab);
	}

	void OnEnable()
	{
		if (CharacterInfoInnerCanvas.instance != null)
			CharacterInfoInnerCanvas.instance.gameObject.SetActive(true);

		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		if (CharacterInfoInnerCanvas.instance != null)
			CharacterInfoInnerCanvas.instance.gameObject.SetActive(false);

		StackCanvas.Pop(gameObject);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		// 현재 상태에 따라
		StackCanvas.Home();
	}

	#region Info
	public string currentActorId { get; private set; }
	public void RefreshInfo(string actorId)
	{
		currentActorId = actorId;
	}
	#endregion


	public void OnDragRect(BaseEventData baseEventData)
	{
		CharacterListCanvas.instance.OnDragRect(baseEventData);
	}
}