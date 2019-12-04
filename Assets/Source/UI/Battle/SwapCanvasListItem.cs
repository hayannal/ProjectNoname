using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwapCanvasListItem : MonoBehaviour
{
	public Image characterImage;
	public Text powerLevelText;
	public Text nameText;
	public Text powerSourceText;
	public Text recommandedText;
	public GameObject selectObject;

	public string actorId { get; set; }
	public void Initialize(CharacterData characterData, int suggestedPowerLevel, string[] suggestedActorIdList)
	{
		actorId = characterData.actorId;

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(characterData.actorId);
		AddressableAssetLoadManager.GetAddressableSprite(actorTableData.portraitAddress, "Icon", (sprite) =>
		{
			characterImage.sprite = null;
			characterImage.sprite = sprite;
		});

		powerLevelText.text = UIString.instance.GetString("GameUI_Power", characterData.powerLevel);
		powerLevelText.color = (characterData.powerLevel < suggestedPowerLevel) ? new Color(0.78f, 0.0f, 0.0f) : Color.white;
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		powerSourceText.SetLocalizedText(PowerSource.Index2Name(actorTableData.powerSource));

		recommandedText.gameObject.SetActive(false);
		if (suggestedActorIdList != null)
		{
			bool find = false;
			for (int i = 0; i < suggestedActorIdList.Length; ++i)
			{
				if (suggestedActorIdList[i] == characterData.actorId)
				{
					find = true;
					break;
				}
			}
			if (find)
			{
				recommandedText.SetLocalizedText(UIString.instance.GetString("GameUI_Suggested"));
				recommandedText.gameObject.SetActive(true);
			}
		}
		
		selectObject.SetActive(false);
	}

	public void OnClickButton()
	{
		SwapCanvas.instance.OnClickListItem(actorId);
	}

	public void ShowSelectObject(bool show)
	{
		selectObject.SetActive(show);
	}
}
