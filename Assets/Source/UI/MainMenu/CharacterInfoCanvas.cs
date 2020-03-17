using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class CharacterInfoCanvas : MonoBehaviour
{
	public static CharacterInfoCanvas instance;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		StackCanvas.Pop(gameObject);
	}

	#region Info
	public void RefreshInfo(string actorId)
	{
		// tooltip
		// CharStory + "\n\n" + CharDesc

		// 뽑기창에서는 이와 다르게
		// Char CharDesc는 기본으로 나오고 돋보기로만 Story를 본다.
	}
	#endregion


	public void OnDragRect(BaseEventData baseEventData)
	{
		CharacterListCanvas.instance.OnDragRect(baseEventData);
	}
}
