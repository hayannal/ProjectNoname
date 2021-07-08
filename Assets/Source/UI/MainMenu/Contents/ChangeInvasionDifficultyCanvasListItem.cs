using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeInvasionDifficultyCanvasListItem : MonoBehaviour
{
	public Button difficultyButton;
	public Text buttonText;

	int _difficulty;
	public void Initialize(int difficulty, int selectedActorPowerLevel, bool selected)
	{
		_difficulty = difficulty;

		string difficultyText = GetDifficultyText(difficulty);
		int limitPowerLevel = GetLimitPowerLevel(difficulty);
		string powerLevelText = "";
		if (selectedActorPowerLevel < limitPowerLevel)
			powerLevelText = string.Format("POWER <color=#FF7700><size=16>{0}</size></color>", limitPowerLevel);
		else
			powerLevelText = string.Format("POWER <size=16>{0}</size>", limitPowerLevel);
		buttonText.text = string.Format("{0} - {1}", difficultyText, powerLevelText);
		difficultyButton.interactable = !selected;
	}

	public static string GetDifficultyText(int difficulty)
	{
		switch(difficulty)
		{
			case 1: return "EASY";
			case 2: return "NORMAL";
			case 3: return "HARD";
			case 4: return "EXPERT";
			case 5: return "HELL";
		}
		return "EASY";
	}

	public static int GetLimitPowerLevel(int difficulty)
	{
		switch (difficulty)
		{
			case 1: return 1;
			case 2: return 3;
			case 3: return 5;
			case 4: return 7;
			case 5: return 9;
		}
		return 1;
	}

	public void OnClickButton()
	{
		ChangeInvasionDifficultyCanvas.instance.gameObject.SetActive(false);
		InvasionEnterCanvas.instance.OnChangeDifficulty(_difficulty);
	}
}