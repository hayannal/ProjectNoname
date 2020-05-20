using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Hexart;

public class AutoSelectCanvas : MonoBehaviour
{
	public static AutoSelectCanvas instance;

	public Image[] gradeImageList;
	public Text[] gradeTextList;
	public GameObject[] gradeOnOffObjectList;
	public GameObject[] selectObjectList;

	public SwitchAnim enhancedSwitch;
	public Text enhancedOnOffText;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		enhancedSwitch.isOn = false;
		for (int i = 0; i < gradeImageList.Length; ++i)
			gradeImageList[i].color = EquipListStatusInfo.GetGradeTitleBarColor(i);
		for (int i = 0; i < gradeTextList.Length; ++i)
			gradeTextList[i].SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", i)));
	}

	public void OnClickGrade0() { OnClickGrade(0); }
	public void OnClickGrade1() { OnClickGrade(1); }
	public void OnClickGrade2() { OnClickGrade(2); }
	public void OnClickGrade3() { OnClickGrade(3); }
	public void OnClickGrade4() { OnClickGrade(4); }

	bool _initialized = false;
	public void InitializeGrade(int grade)
	{
		if (_initialized)
			return;

		// 처음 켜질때 1회만 초기화로 쓴다.
		// 자신보다 하나 낮은 등급만 켜두는데 예외로 일반 등급일땐 자신 이하로 처리한다.
		int includeGradeMax = 0;
		switch (grade)
		{
			case 0: includeGradeMax = 0; break;
			default: includeGradeMax = grade - 1; break;
		}
		for (int i = 0; i <= includeGradeMax; ++i)
			OnClickGrade(i);

		_initialized = true;
	}

	List<int> _listGrade = new List<int>();
	void OnClickGrade(int grade)
	{
		bool contains = _listGrade.Contains(grade);
		if (contains)
		{
			_listGrade.Remove(grade);
			gradeOnOffObjectList[grade].SetActive(true);
			selectObjectList[grade].SetActive(false);
		}
		else
		{
			_listGrade.Add(grade);
			gradeOnOffObjectList[grade].SetActive(false);
			selectObjectList[grade].SetActive(true);
		}
	}

	public void OnSwitchOnEnhanced()
	{
		enhancedOnOffText.text = "ON";
		enhancedOnOffText.color = Color.white;
	}

	public void OnSwitchOffEnhanced()
	{
		enhancedOnOffText.text = "OFF";
		enhancedOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
	}

	public void OnClickApplyButton()
	{
		if (_listGrade.Count == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_SelectGrade"), 2.0f);
			return;
		}

		if (EquipSellCanvas.instance != null && EquipSellCanvas.instance.gameObject.activeSelf)
			EquipSellCanvas.instance.OnAutoSelect(_listGrade, enhancedSwitch.isOn);
		else
			EquipInfoGrowthCanvas.instance.OnAutoSelect(_listGrade, enhancedSwitch.isOn);
		gameObject.SetActive(false);
	}

	public void OnClickCancelButton()
	{
		gameObject.SetActive(false);
	}
}