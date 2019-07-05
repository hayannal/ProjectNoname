using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageTestCanvas : MonoBehaviour
{
	public static StageTestCanvas instance;

	public Text currentMapText;

	void Awake()
	{
		instance = this;
	}

	public void RefreshCurrentMapText(int chapter, int stage, string map)
	{
		currentMapText.text = string.Format("chapter = {0} / stage = {1} / map = {2}", chapter, stage, map);
	}

	public void OnClickNextStage()
	{
		StageManager.instance.NextStage();
	}
}
