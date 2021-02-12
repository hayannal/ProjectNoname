using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestInfoItem : MonoBehaviour
{
	public Text titleText;
	public Text contentText;
	public Text goldText;

	int _idx;
	public void RefreshInfo(QuestData.QuestInfo questInfo)
	{
		_idx = questInfo.idx;
		SubQuestTableData subQuestTableData = TableDataManager.instance.FindSubQuestTableData(questInfo.tp);
		titleText.SetLocalizedText(UIString.instance.GetString(subQuestTableData.nameId));
		if (questInfo.cdtn == (int)QuestData.eQuestCondition.None)
			contentText.SetLocalizedText(UIString.instance.GetString(subQuestTableData.descriptionId, questInfo.cnt));
		else
			contentText.SetLocalizedText(string.Format("{0}\n{1}", UIString.instance.GetString(subQuestTableData.descriptionId, questInfo.cnt), GetConditionText(questInfo)));
		goldText.text = questInfo.rwd.ToString("N0");
		goldText.color = DailyFreeItem.GetGoldTextColor();
	}

	string GetConditionText(QuestData.QuestInfo questInfo)
	{
		string paramName = "";
		switch (questInfo.cdtn)
		{
			case (int)QuestData.eQuestCondition.PowerSource:
				paramName = PowerSource.Index2SmallName(questInfo.param);
				break;
			case (int)QuestData.eQuestCondition.Grade:
				paramName = UIString.instance.GetString(string.Format("GameUI_CharGrade{0}", questInfo.param));
				break;
		}
		return UIString.instance.GetString("QuestUI_Condition", paramName);
	}
	
	public void OnClickButton()
	{
		// 정보창에서는 클릭을 할 수 없으니 클릭할 수 있을때는 선택창에서 뿐이다.
		PlayFabApiManager.instance.RequestSelectQuest(_idx, () =>
		{
			QuestSelectCanvas.instance.gameObject.SetActive(false);
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("QuestUI_AcceptQuest"), 2.0f);
		});
	}
}