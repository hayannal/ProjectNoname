using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CumulativeEventEquipInfoCanvas : MonoBehaviour
{
	public static CumulativeEventEquipInfoCanvas instance;

	public Text gradeText;

	public Image blurImage;
	public Image backgroundImge;
	public Sprite[] backgroundSpriteList;
	public Image equipIconImage;
	public Text nameText;

	public Text attackText;
	public RectTransform detailButtonRectTransform;

	public EquipListStatusInfo materialSmallStatusInfo;

	void Awake()
	{
		instance = this;
	}

	void OnDisable()
	{
		materialSmallStatusInfo.gameObject.SetActive(false);
	}

	CumulativeEventData.EventRewardInfo _slotInfo;
	public void ShowCanvas(bool show, CumulativeEventData.EventRewardInfo eventRewardInfo)
	{
		_slotInfo = eventRewardInfo;

		// DailyShop때처럼 마스킹으로 가려볼까 하다가 안에 들어있는게 많아서 떼어내서 쓰기로 한다.
		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(eventRewardInfo.value);
		if (equipTableData == null)
			return;

		RefreshEquipIconImage(eventRewardInfo.value);

		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", equipTableData.grade)));
		gradeText.color = EquipListStatusInfo.GetGradeDropObjectNameColor(equipTableData.grade);
		attackText.text = UIString.instance.GetString("GameUI_NumberRange", ActorStatus.GetDisplayAttack(equipTableData.min).ToString("N0"), ActorStatus.GetDisplayAttack(equipTableData.max).ToString("N0"));

		// 장착중인 정보
		EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType((TimeSpaceData.eEquipSlotType)equipTableData.equipType);
		if (equipData != null)
		{
			materialSmallStatusInfo.RefreshInfo(equipData, false);
			materialSmallStatusInfo.gameObject.SetActive(false);
			materialSmallStatusInfo.gameObject.SetActive(true);
		}
		else
			materialSmallStatusInfo.gameObject.SetActive(false);

		// 구매하면 3d 오브젝트가 떠야하니 미리 로딩을 걸어둔다.
		AddressableAssetLoadManager.GetAddressableGameObject(equipTableData.prefabAddress, "Equip", null);
	}

	void RefreshEquipIconImage(string value)
	{
		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(value);
		if (equipTableData == null)
			return;

		RefreshBackground(equipTableData.grade == 4);

		AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", (sprite) =>
		{
			equipIconImage.sprite = null;
			equipIconImage.sprite = sprite;
		});

		nameText.SetLocalizedText(UIString.instance.GetString(equipTableData.nameId));
		nameText.gameObject.SetActive(true);
	}

	void RefreshBackground(bool isLightenBackground)
	{
		blurImage.color = isLightenBackground ? new Color(0.945f, 0.945f, 0.094f, 0.42f) : new Color(0.094f, 0.945f, 0.871f, 0.42f);
		backgroundImge.color = isLightenBackground ? new Color(1.0f, 1.0f, 1.0f, 0.42f) : new Color(0.0f, 1.0f, 0.749f, 0.42f);
		backgroundImge.sprite = backgroundSpriteList[isLightenBackground ? 0 : 1];
	}

	public void OnClickDetailButton()
	{
		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(_slotInfo.value);
		if (equipTableData == null)
			return;

		// 장비일때만 오는거라 장비가 보이면 된다.
		// 여긴 장비를 만들어내기 전이기 때문에 equipId만 가지고 3d오브젝트를 보여줄 방법이 필요하다.
		// 이 창은 연출과도 관련없으니 스스로 StackCanvas를 처리한다.
		UIInstanceManager.instance.ShowCanvasAsync("DailyShopEquipDetailCanvas", () =>
		{
			gameObject.SetActive(false);
			DailyShopEquipDetailCanvas.instance.ShowCanvas(true, equipTableData, () =>
			{
				gameObject.SetActive(true);
				ShowCanvas(true, _slotInfo);
			});
		});
	}
}