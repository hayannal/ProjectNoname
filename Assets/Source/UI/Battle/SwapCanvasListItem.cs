using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwapCanvasListItem : MonoBehaviour
{
	public RectTransform contentRectTransform;
	public Image characterImage;
	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Image lineColorImage;
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
		//powerLevelText.color = (characterData.powerLevel < suggestedPowerLevel) ? new Color(1.0f, 0.1f, 0.1f) : Color.white;
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		powerSourceText.SetLocalizedText(PowerSource.Index2Name(actorTableData.powerSource));

		switch (actorTableData.grade)
		{
			case 0:
				blurImage.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
				gradient.color1 = Color.white;
				gradient.color2 = Color.black;
				lineColorImage.color = new Color(0.5f, 0.5f, 0.5f);
				break;
			case 1:
				blurImage.color = new Color(0.28f, 0.78f, 1.0f, 0.0f);
				gradient.color1 = new Color(0.0f, 0.7f, 1.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.0f, 0.51f, 1.0f);
				break;
			case 2:
				blurImage.color = new Color(1.0f, 0.78f, 0.31f, 0.0f);
				gradient.color1 = new Color(1.0f, 0.5f, 0.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(1.0f, 0.5f, 0.0f);
				break;
		}

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

	void Update()
	{
		UpdateSelectPosition();
	}

	void UpdateSelectPosition()
	{
		Vector2 selectOffset = new Vector2(-13.0f, 8.0f);
		if (selectObject.activeSelf)
		{
			if (contentRectTransform.anchoredPosition != selectOffset)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, selectOffset, Time.deltaTime * 15.0f);
				Vector2 diff = contentRectTransform.anchoredPosition - selectOffset;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = selectOffset;
			}
		}
		else
		{
			if (contentRectTransform.anchoredPosition != Vector2.zero)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, Vector2.zero, Time.deltaTime * 15.0f);
				Vector2 diff = contentRectTransform.anchoredPosition;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = Vector2.zero;
			}
		}
	}
}
