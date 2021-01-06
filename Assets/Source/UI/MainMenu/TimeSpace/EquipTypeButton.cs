using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipTypeButton : MonoBehaviour
{
	public RectTransform contentRectTransform;
	public Image backgroundImage;
	public int positionIndex;
	public Image equipGradeImage;
	public RectTransform alarmRootTransform;

	void OnEnable()
	{
		RefreshEquipGrade();

		// 9탭이 보이는 상황에서 아이템이 갑자기 사라지는 일은 장착이나 해제니 그때만 예외처리 해주면 된다.
		RefreshAlarmObject();
	}

	public static Color GetGradeBarColor(int grade)
	{
		switch (grade)
		{
			case 0: return new Color(0.792f, 0.792f, 0.792f);
			case 1: return new Color(0.266f, 0.776f, 0.266f);
			case 2: return new Color(0.18f, 0.678f, 0.98f);
			case 3: return new Color(0.7f, 0.2f, 1.0f);
			case 4: return new Color(0.9f, 0.592f, 0.192f);
		}
		return Color.white;
	}

	public void RefreshEquipGrade()
	{
		EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType((TimeSpaceData.eEquipSlotType)positionIndex);
		if (equipData == null)
		{
			equipGradeImage.gameObject.SetActive(false);
			return;
		}

		equipGradeImage.color = GetGradeBarColor(equipData.cachedEquipTableData.grade);
		equipGradeImage.gameObject.SetActive(true);
	}

	public bool selected { get; set; }
	void Update()
	{
		UpdateSelectPosition();
		UpdateSelectImage();
	}

	void UpdateSelectPosition()
	{
		Vector2 selectOffset = new Vector2(-4.0f, 3.0f);
		if (selected)
		{
			if (contentRectTransform.anchoredPosition != selectOffset)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, selectOffset, Time.deltaTime * 8.0f);
				Vector2 diff = contentRectTransform.anchoredPosition - selectOffset;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = selectOffset;
			}
		}
		else
		{
			if (contentRectTransform.anchoredPosition != Vector2.zero)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, Vector2.zero, Time.deltaTime * 8.0f);
				Vector2 diff = contentRectTransform.anchoredPosition;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = Vector2.zero;
			}
		}
	}

	void UpdateSelectImage()
	{
		Color selectColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
		Color deselectColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);
		if (selected)
		{
			if (backgroundImage.color.a != selectColor.a)
			{
				backgroundImage.color = Color.Lerp(backgroundImage.color, selectColor, Time.deltaTime * 5.0f);
				float diff = Mathf.Abs(backgroundImage.color.a - selectColor.a);
				if (diff < 0.01f)
					backgroundImage.color = selectColor;
			}
		}
		else
		{
			if (backgroundImage.color.a != deselectColor.a)
			{
				backgroundImage.color = Color.Lerp(backgroundImage.color, deselectColor, Time.deltaTime * 5.0f);
				float diff = Mathf.Abs(backgroundImage.color.a - deselectColor.a);
				if (diff < 0.01f)
					backgroundImage.color = deselectColor;
			}
		}
	}

	#region AlarmObject
	public void RefreshAlarmObject()
	{
		bool show = false;
		List<EquipData> listEquipData = TimeSpaceData.instance.GetEquipListByType((TimeSpaceData.eEquipSlotType)positionIndex);
		for (int i = 0; i < listEquipData.Count; ++i)
		{
			if (TimeSpaceData.instance.IsEquipped(listEquipData[i]))
				continue;
			show = listEquipData[i].newEquip;
			if (show)
				break;
		}
		if (show)
			AlarmObject.Show(alarmRootTransform);
		else
			AlarmObject.Hide(alarmRootTransform);
	}
	#endregion
}