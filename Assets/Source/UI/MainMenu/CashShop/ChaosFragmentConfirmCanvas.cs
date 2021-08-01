using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChaosFragmentConfirmCanvas : MonoBehaviour
{
	public static ChaosFragmentConfirmCanvas instance;

	public Transform iconRootTransform;

	public Text priceText;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;
	public Text countText;

	void Awake()
	{
		instance = this;
	}

	public int slotIndex { get; set; }
	int _price;
	void OnEnable()
	{
		_price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosPowerPointsCost");

		priceText.text = _price.ToString("N0");
		bool disablePrice = (PlayerData.instance.chaosFragmentCount < _price);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;

		countText.SetLocalizedText(string.Format("{0} / {1}", UIString.instance.GetString("GameUI_ReturnScrollCount", PlayerData.instance.chaosFragmentCount),
			BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosFragmentMax")));
	}

	public void OnClickDetailButton()
	{
		string text = UIString.instance.GetString("ShopUI_PpTransformerMore");
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 350, iconRootTransform, new Vector2(0.0f, -85.0f));
	}

	DropProcessor _cachedDropProcessor;
	public void OnClickOkButton()
	{
		if (PlayerData.instance.chaosFragmentCount < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughChaosFragment"), 2.0f);
			return;
		}

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(PlayerData.instance.highestPlayChapter);
		if (chapterTableData == null)
			return;

		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, chapterTableData.chaosFragmentDropId, "", true, true);
		if (CheatingListener.detectedCheatTable)
			return;

		PlayFabApiManager.instance.RequestTransformChaosFragment(slotIndex, _price, OnRecvTransformChaosFragment);
	}

	void OnRecvTransformChaosFragment()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ChaosFragmentResultCanvas", () =>
		{
			// drop연출을 할게 아니므로 드랍프로세서 꺼둔다.
			_cachedDropProcessor.gameObject.SetActive(false);

			// Confirm팝업이 닫히면서 Result팝업이 열리면 된다.
			gameObject.SetActive(false);
		});
	}
}