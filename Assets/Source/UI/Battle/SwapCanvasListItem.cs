using System;
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
	public GameObject powerLevelObject;
	public Text powerLevelText;
	public Text nameText;
	public Text powerSourceText;
	public Text recommandedText;
	public GameObject selectObject;
	public GameObject blackObject;
	public RectTransform alarmRootTransform;

	public string actorId { get; set; }
	public void Initialize(string actorId, int powerLevel, int suggestedPowerLevel, string[] suggestedActorIdList, Action<string> clickCallback)
	{
		this.actorId = actorId;

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		AddressableAssetLoadManager.GetAddressableSprite(actorTableData.portraitAddress, "Icon", (sprite) =>
		{
			characterImage.sprite = null;
			characterImage.sprite = sprite;
		});

		powerLevelObject.SetActive(powerLevel > 0);
		powerLevelText.text = UIString.instance.GetString("GameUI_Power", powerLevel);
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
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false && GatePillar.CheckSuggestedActor(suggestedActorIdList, actorId))
		{
			recommandedText.SetLocalizedText(UIString.instance.GetString("GameUI_Suggested"));
			recommandedText.gameObject.SetActive(true);
		}
		blackObject.SetActive(lobby && powerLevel == 0);
		
		selectObject.SetActive(false);
		_clickAction = clickCallback;
	}

	Action<string> _clickAction;
	public void OnClickButton()
	{
		if (_clickAction != null)
			_clickAction.Invoke(actorId);
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


	#region Alarm
	// 다른 Alarm 가진 오브젝트들과 달리 캐릭터창은 다른 창들과 GridItem을 공유하면서도 해당 캔버스에서만 보여야하기 때문에 LitItem 단에서 처리하지 않는다.
	// 그래서 밖에서 컨트롤 할 수 있게 public 함수로만 만들어두고 사용한다.
	public void ShowAlarm(bool show, bool usePlusAlarm = false)
	{
		if (show)
		{
			// plusAlarm에서는 애니를 사용하지 않는다.
			if (usePlusAlarm)
				AlarmObject.Show(alarmRootTransform, false, true, true);
			else
				AlarmObject.Show(alarmRootTransform, true, true, false);
		}
		else
		{
			AlarmObject.Hide(alarmRootTransform);
		}
	}
	#endregion




	RectTransform _rectTransform;
	public RectTransform cachedRectTransform
	{
		get
		{
			if (_rectTransform == null)
				_rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}
}
