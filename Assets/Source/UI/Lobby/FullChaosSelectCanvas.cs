using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullChaosSelectCanvas : MonoBehaviour
{
	public static FullChaosSelectCanvas instance;

	public Transform subTitleTextTransform;

	void Awake()
	{
		instance = this;
	}

	public void OnClickMoreButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("GameUI_ChaosPopMore"), 300, subTitleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickChallengeButton()
	{
	}

	public void OnClickRevertButton()
	{
	}
}