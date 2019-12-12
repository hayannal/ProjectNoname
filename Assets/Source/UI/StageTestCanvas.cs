using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StageTestCanvas : MonoBehaviour
{
	public static StageTestCanvas instance;

	public Text currentMapText;
	public Text currentStatText;

	void Awake()
	{
		instance = this;

		StageTableData stageTableData = TableDataManager.instance.FindStageTableData(1, 1, PlayerData.instance.chaosMode);
		if (stageTableData != null)
			StageManager.instance.currentStageTableData = stageTableData;
	}

	public void OnClickButton()
	{
		//SceneManager.LoadScene(0);
	}

	public void RefreshCurrentMapText(int chapter, int stage, string map)
	{
		currentMapText.text = string.Format("chapter = {0} / stage = {1} / map = {2}", chapter, stage, map);
	}

	public void RefreshCurrentStatText(float hp, float sp)
	{
		currentStatText.text = string.Format("hp = {0:0.##} / sp = {1:0.##}", hp, sp);
	}
}
