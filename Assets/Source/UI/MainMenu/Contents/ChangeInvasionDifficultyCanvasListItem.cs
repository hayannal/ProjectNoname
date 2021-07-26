using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeInvasionDifficultyCanvasListItem : MonoBehaviour
{
	public Button difficultyButton;
	public Text buttonText;

	int _difficulty;
	bool _selectable = false;
	public void Initialize(int difficulty, int selectedActorPowerLevel, bool selected)
	{
		_difficulty = difficulty;

		string difficultyText = GetDifficultyText(difficulty);
		int limitPowerLevel = GetLimitPowerLevel(difficulty);
		string powerLevelText = "";
		if (selectedActorPowerLevel < limitPowerLevel)
		{
			powerLevelText = string.Format("POWER <color=#FF7700><size=16>{0}</size></color>", limitPowerLevel);
			_selectable = false;
		}
		else
		{
			powerLevelText = string.Format("POWER <size=16>{0}</size>", limitPowerLevel);
			_selectable = true;
		}
		buttonText.text = string.Format("{0} - {1}", difficultyText, powerLevelText);
		difficultyButton.interactable = !selected;
	}

	public static string GetDifficultyText(int difficulty)
	{
		switch(difficulty)
		{
			case 1: return "EASY 1";
			case 2: return "EASY 2";
			case 3: return "EASY 3";
			case 4: return "EASY 4";
			case 5: return "HARD 1";
			case 6: return "HARD 2";
			case 7: return "HARD 3";
			case 8: return "HARD 4";
			case 9: return "HARD 5";
			case 10: return "HELL 1";
			case 11: return "HELL 2";
			case 12: return "HELL 3";
			case 13: return "HELL 4";
			case 14: return "HELL 5";
			case 15: return "HELL 6";
			case 16: return "HELL 7";
		}
		return "EASY 1";
	}

	public static int GetLimitPowerLevel(int difficulty)
	{
		//switch (difficulty)
		//{
		//	case 1: return 1;
		//	case 2: return 3;
		//	case 3: return 5;
		//	case 4: return 7;
		//	case 5: return 9;
		//}
		//return 1;

		// 이제는 1:1 매칭이라 diffculty로 저장해둔게 곧 파워레벨이다.
		return difficulty;
	}

	public void OnClickButton()
	{
		if (_selectable == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("InvasionUI_CannotSelectPower"), 2.0f);
			return;
		}

		ChangeInvasionDifficultyCanvas.instance.gameObject.SetActive(false);
		InvasionEnterCanvas.instance.OnChangeDifficulty(_difficulty);
	}
}