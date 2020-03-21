using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullChaosSelectCanvas : MonoBehaviour
{
	public static FullChaosSelectCanvas instance;

	void Awake()
	{
		instance = this;
	}

	public void OnClickChallengeButton()
	{
	}

	public void OnClickRevertButton()
	{
	}
}