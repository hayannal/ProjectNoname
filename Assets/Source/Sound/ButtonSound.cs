using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonSound : MonoBehaviour, IPointerClickHandler
{
	public string SFXName = "Click";

	public void OnPointerClick(PointerEventData eventData)
	{
		SoundManager.instance.PlaySFX(SFXName);
	}
}