using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeDifficultyCanvasListItem : MonoBehaviour
{
	public Button difficultyButton;
	public Text difficultyText;
	public GameObject clearObject;

	int _difficulty;
	public void Initialize(int difficulty, int visualDifficulty, bool showClear, bool selected)
	{
		_difficulty = difficulty;
		difficultyText.text = string.Format("<size=14>DIFFICULTY</size> {0}", visualDifficulty);
		difficultyButton.interactable = !selected;
		clearObject.SetActive(showClear);
	}

	public void OnClickButton()
	{
		ChangeDifficultyCanvas.instance.gameObject.SetActive(false);
		BossBattleEnterCanvas.instance.OnChangeDifficulty(_difficulty);
	}
}