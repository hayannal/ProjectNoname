using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

public class MailCanvasListItem : MonoBehaviour
{
	public LayoutElement layoutElement;
	public RectTransform contentRectTransform;
	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Text nameText;
	public GameObject selectObject;

	public Transform descRootTransform;
	public CanvasGroup descObjectCanvasGroup;
	public Text remainTimeText;
	public Text descText;
	public Text noAttachDescText;
	public GameObject rewardRootObject;
	public GameObject goldIconObject;
	public GameObject diaIconObject;
	public GameObject energyIconObject;
	public GameObject equipBoxObject;
	public Text countText;
	public Text rewardNameText;
	public GameObject addObject;
	public Text addText;
	public RectTransform alarmRootTransform;

	float _defaultLayoutPreferredHeightMin;
	float _defaultLayoutPreferredHeightMax;
	void Awake()
	{
		_defaultLayoutPreferredHeightMin = layoutElement.minHeight;
		_defaultLayoutPreferredHeightMax = layoutElement.preferredHeight;
	}

	public string id { get; set; }
	public int receiveDay { get; set; }
	int _addDia, _addGold, _addEnergy;
	string _type;
	string _value;
	public void Initialize(MailData.MailCreateInfo createInfo, MailData.MyMailData myMailData, int receiveDay, DateTime validTime)
	{
		this.id = myMailData.id;
		this.receiveDay = receiveDay;
		_addDia = _addGold = _addEnergy = 0;

		nameText.SetLocalizedText(UIString.instance.GetString(createInfo.nm));

		_type = createInfo.tp;
		_value = createInfo.vl;
		if (string.IsNullOrEmpty(createInfo.tp))
		{
			// 보상이 없는 메일이다. 보통 공지같은데 쓰는 시스템 메일이다.
			rewardRootObject.SetActive(false);
			descText.gameObject.SetActive(false);
			noAttachDescText.SetLocalizedText(UIString.instance.GetString(createInfo.de));
			noAttachDescText.gameObject.SetActive(true);

			blurImage.color = new Color(1.0f, 0.392f, 0.392f, 0.274f);
			gradient.color1 = Color.white;
			gradient.color2 = new Color(1.0f, 0.0f, 0.392f, 0.427f);

			remainTimeText.text = "";
			_needUpdate = false;
		}
		else
		{
			rewardRootObject.SetActive(true);
			noAttachDescText.gameObject.SetActive(false);
			descText.SetLocalizedText(UIString.instance.GetString(createInfo.de));
			descText.gameObject.SetActive(true);

			blurImage.color = new Color(0.896f, 0.896f, 0.452f, 0.274f);
			gradient.color1 = Color.white;
			gradient.color2 = new Color(0.896f, 0.827f, 0.0f, 0.475f);

			_validTime = validTime;
			_needUpdate = true;

			if (createInfo.tp == "cu")
			{
				if (createInfo.vl == CurrencyData.GoldCode())
				{
					_addGold = createInfo.cn;
					goldIconObject.SetActive(true);
					diaIconObject.SetActive(false);
					energyIconObject.SetActive(false);
					countText.color = DailyFreeItem.GetGoldTextColor();
				}
				else if (createInfo.vl == CurrencyData.DiamondCode())
				{
					_addDia = createInfo.cn;
					goldIconObject.SetActive(false);
					diaIconObject.SetActive(true);
					energyIconObject.SetActive(false);
					countText.color = DailyFreeItem.GetDiaTextColor();
				}
				else
				{
					_addEnergy = createInfo.cn;
					goldIconObject.SetActive(false);
					diaIconObject.SetActive(false);
					energyIconObject.SetActive(true);
					countText.color = Color.white;
				}
				countText.text = createInfo.cn.ToString("N0");
				countText.gameObject.SetActive(true);
				equipBoxObject.SetActive(false);
				rewardNameText.gameObject.SetActive(false);
				addObject.SetActive(false);
			}
			else if (createInfo.tp == "be")
			{
				goldIconObject.SetActive(false);
				diaIconObject.SetActive(false);
				energyIconObject.SetActive(false);
				equipBoxObject.SetActive(true);

				countText.gameObject.SetActive(false);
				rewardNameText.gameObject.SetActive(true);
				addObject.SetActive(true);
				addText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", createInfo.vl)));
			}
			AlarmObject.Show(alarmRootTransform);
		}

		layoutElement.preferredHeight = _defaultLayoutPreferredHeightMin;
		descRootTransform.localScale = new Vector3(1.0f, 0.0f, 1.0f);
		descObjectCanvasGroup.alpha = 0.0f;
		selectObject.SetActive(false);
	}

	public void OnClickButton()
	{
		MailCanvas.instance.OnClickListItem(id, receiveDay);
		SoundManager.instance.PlaySFX(selectObject.activeSelf ? "GridOff" : "GridOn");
	}
	
	public void ShowSelectObject(bool show)
	{
		if (show)
			selectObject.SetActive(!selectObject.activeSelf);
		else
			selectObject.SetActive(show);
	}

	void Update()
	{
		UpdateSelectPosition();
		UpdateDescTransform();
		UpdateRemainTime();		
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

	void UpdateDescTransform()
	{
		if (selectObject.activeSelf)
		{
			if (layoutElement.preferredHeight != _defaultLayoutPreferredHeightMax)
			{
				layoutElement.preferredHeight = Mathf.Lerp(layoutElement.preferredHeight, _defaultLayoutPreferredHeightMax, Time.deltaTime * 15.0f);
				float diff = layoutElement.preferredHeight - _defaultLayoutPreferredHeightMax;
				if (Mathf.Abs(diff) < 0.1f)
					layoutElement.preferredHeight = _defaultLayoutPreferredHeightMax;

				float ratio = (layoutElement.preferredHeight - _defaultLayoutPreferredHeightMin) / (_defaultLayoutPreferredHeightMax - _defaultLayoutPreferredHeightMin);
				descRootTransform.localScale = new Vector3(1.0f, ratio, 1.0f);
				ratio -= 0.9f;
				if (ratio < 0.0f) ratio = 0.0f;
				ratio *= (1.0f / 0.1f);
				descObjectCanvasGroup.alpha = ratio;
			}
		}
		else
		{
			if (layoutElement.preferredHeight != _defaultLayoutPreferredHeightMin)
			{
				layoutElement.preferredHeight = Mathf.Lerp(layoutElement.preferredHeight, _defaultLayoutPreferredHeightMin, Time.deltaTime * 15.0f);
				float diff = layoutElement.preferredHeight - _defaultLayoutPreferredHeightMin;
				if (Mathf.Abs(diff) < 0.1f)
					layoutElement.preferredHeight = _defaultLayoutPreferredHeightMin;

				float ratio = (layoutElement.preferredHeight - _defaultLayoutPreferredHeightMin) / (_defaultLayoutPreferredHeightMax - _defaultLayoutPreferredHeightMin);
				descRootTransform.localScale = new Vector3(1.0f, ratio, 1.0f);
				ratio -= 0.9f;
				if (ratio < 0.0f) ratio = 0.0f;
				ratio *= (1.0f / 0.1f);
				descObjectCanvasGroup.alpha = ratio;
			}
		}
	}

	DateTime _validTime;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _validTime)
		{
			if (selectObject.activeSelf)
			{
				TimeSpan remainTime = _validTime - ServerTime.UtcNow;
				if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
				{
					remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours + remainTime.Days * 24, remainTime.Minutes, remainTime.Seconds);
					remainTimeText.color = (remainTime.Days > 0) ? new Color(0.5f, 0.5f, 0.5f) : new Color(1.0f, 0.54f, 0.54f);
					_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
				}
			}
		}
		else
		{
			_needUpdate = false;
			remainTimeText.text = "00:00:00";
			gameObject.SetActive(false);
		}
	}

	public void OnClickRewardButton()
	{
		if (_type == "cu")
		{
			PlayFabApiManager.instance.RequestReceiveMailPresent(id, receiveDay, _type, _addDia, _addGold, _addEnergy, "", (serverFailure, itemGrantString) =>
			{
				DotMainMenuCanvas.instance.RefreshMailAlarmObject();
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("MailUI_AfterClaim"), 2.0f);
				MailCanvas.instance.RefreshGrid();
			});
		}
		else if (_type == "be")
		{
			if (TimeSpaceData.instance.IsInventoryVisualMax())
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
				return;
			}

			// 장비박스 했을때처럼 드랍프로세서로부터 하나 뽑아와야한다.
			string equipId = PrepareDropProcessor();
			if (equipId == "")
				return;
			if (CheatingListener.detectedCheatTable)
				return;

			PlayFabApiManager.instance.RequestReceiveMailPresent(id, receiveDay, _type, 0, 0, 0, equipId, OnRecvEquipBox);
		}
	}

	DropProcessor _cachedDropProcessor;
	string PrepareDropProcessor()
	{
		// 오리진 박스와 마찬가지로 먼저 드랍프로세서부터 만들어야한다.
		string dropId = "";
		switch (_value)
		{
			case "1": dropId = "Dnvuswkdqlu"; break;
			case "2": dropId = "Dnvuswkdqlv"; break;
			case "3": dropId = "Dnvuswkdqlw"; break;
			default:
				return "";
		}
		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, dropId, "", true, true);
		if (CheatingListener.detectedCheatTable)
			return "";
		List<ObscuredString> listDropItemId = DropManager.instance.GetLobbyDropItemInfo();
		if (listDropItemId.Count == 1)
			return listDropItemId[0];
		return "";
	}

	// EquipBoxConfirmCanvas의 OnRecvEquipBox에서 가져와서 수정해서 쓴다.
	void OnRecvEquipBox(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		// 캐릭터와 달리 장비는 드랍프로세서에서 정보를 뽑아쓰는게 아니라서 미리 클리어해도 상관없다.
		DropManager.instance.ClearLobbyDropInfo();
		DotMainMenuCanvas.instance.RefreshMailAlarmObject();
		if (itemGrantString == "")
			return;

		EquipData grantEquipData = TimeSpaceData.instance.OnRecvGrantEquip(itemGrantString, 1);
		if (grantEquipData == null)
			return;
		AddressableAssetLoadManager.GetAddressableSprite(grantEquipData.cachedEquipTableData.shotAddress, "Icon", null);

		List<ItemInstance> listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(itemGrantString);
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Equip1, _cachedDropProcessor, 0, () =>
			{
				// 결과창은 각 패킷이 자신의 Response에 맞춰서 보여줘야한다.
				// 여기서는 장비 그리드를 띄운다.
				// 결과창을 닫을때 RandomBoxScreenCanvas도 같이 닫아주면 알아서 시작점인 CashShopCanvas로 돌아오게 될거다.
				UIInstanceManager.instance.ShowCanvasAsync("EquipBoxResultCanvas", () =>
				{
					EquipBoxResultCanvas.instance.RefreshInfo(listGrantItem);
				});
			});
		});
	}




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