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
		contentText.SetLocalizedText(UIString.instance.GetString(subQuestTableData.descriptionId, questInfo.cnt));
		goldText.text = questInfo.rwd.ToString("N0");
		goldText.color = DailyFreeItem.GetGoldTextColor();
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