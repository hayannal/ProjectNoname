using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

// 스크린 스페이스 캔버스라서 캐릭터에 썼던 이름과 비슷하게 지어둔다.
public class ResearchInfoGrowthCanvas : MonoBehaviour
{
	public static ResearchInfoGrowthCanvas instance;

	public Transform researchTextTransform;
	public Text researchText;
	public Button leftButton;
	public Button rightButton;

	public RectTransform positionRectTransform;
	public Text levelText;
	public Button levelResetButton;
	public Image gaugeImage;
	public GameObject hpObject;
	public GameObject attackObject;
	public GameObject diaObject;
	public Text hpText;
	public Text attackText;
	public Text diaText;
	public Text gaugeText;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect goldGrayscaleEffect;
	public GameObject disableButtonObject;
	public Image disableButtonImage;
	public Text disableButtonText;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
    {
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	Vector2 _leftTweenPosition = new Vector2(-150.0f, 0.0f);
	Vector2 _rightTweenPosition = new Vector2(150.0f, 0.0f);
	void MoveTween(bool left)
	{
		positionRectTransform.gameObject.SetActive(false);
		positionRectTransform.gameObject.SetActive(true);
		positionRectTransform.anchoredPosition = left ? _leftTweenPosition : _rightTweenPosition;
		positionRectTransform.DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.OutQuad);
	}

	void OnEnable()
	{
		RefreshInfo();
		MoveTween(true);
	}

	int _selectedLevel;
	public void RefreshInfo()
	{
		researchText.text = UIString.instance.GetString("ResearchUI_Research");

		// 처음 켜질땐 항상 현재 도달하려는 연구레벨로 켜지면 된다.
		_selectedLevel = PlayerData.instance.researchLevel + 1;
		RefreshLevelInfo();
	}

	int _price;
	bool _needSumLevel;
	void RefreshLevelInfo()
	{
		levelText.text = UIString.instance.GetString("GameUI_Lv", _selectedLevel);

		gaugeImage.fillAmount = 0.56f;
		hpObject.SetActive(false);
		gaugeText.text = UIString.instance.GetString("GameUI_StageFraction", 1214, 242);

		bool selectCurrentLevel = (_selectedLevel == (PlayerData.instance.researchLevel + 1));
		levelResetButton.gameObject.SetActive(!selectCurrentLevel);
		leftButton.gameObject.SetActive(_selectedLevel != 1);
		rightButton.gameObject.SetActive(_selectedLevel != 10); // max

		if (selectCurrentLevel)
		{
			int requiredGold = 2000;
			int current = 1214;
			int max = 242;
			gaugeText.text = UIString.instance.GetString("GameUI_StageFraction", current, max);

			bool disablePrice = (CurrencyData.instance.gold < requiredGold || current < max);
			priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
			priceText.color = !disablePrice ? Color.white : Color.gray;
			goldGrayscaleEffect.enabled = disablePrice;
			priceButtonObject.SetActive(true);
			disableButtonObject.SetActive(false);
			_price = requiredGold;
		}
		else
		{
			int current = 0;
			int max = 0;
			if (_selectedLevel < (PlayerData.instance.researchLevel + 1))
			{
				current = 50;
				max = 50;
				gaugeImage.fillAmount = 1.0f;
			}

			if (_selectedLevel > (PlayerData.instance.researchLevel + 1))
			{
				current = 0;
				max = 500;
				gaugeImage.fillAmount = 0.0f;
			}
			gaugeText.text = UIString.instance.GetString("GameUI_StageFraction", current, max);
			
			priceButtonObject.SetActive(false);
			disableButtonImage.color = ColorUtil.halfGray;
			disableButtonText.color = ColorUtil.halfGray;
			disableButtonObject.SetActive(true);
		}
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("ResearchUI_ResearchMore"), 250, researchTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickLevelResetButton()
	{
		bool left = (_selectedLevel < (PlayerData.instance.researchLevel + 1));
		_selectedLevel = PlayerData.instance.researchLevel + 1;
		RefreshLevelInfo();
		MoveTween(left);
	}

	public void OnClickLeftButton()
	{
		_selectedLevel -= 1;
		RefreshLevelInfo();
		MoveTween(false);
	}

	public void OnClickRightButton()
	{
		_selectedLevel += 1;
		RefreshLevelInfo();
		MoveTween(true);
	}

	public void OnEndDrag(BaseEventData baseEventData)
	{
		PointerEventData pointerEventData = baseEventData as PointerEventData;
		if (pointerEventData == null)
			return;

		bool left = (pointerEventData.delta.x < 0.0f);
		if (left)
			OnClickLeftButton();
		else
			OnClickRightButton();
	}

	public void OnClickButton()
	{
		if (_needSumLevel)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ResearchUI_NotEnoughLevel"), 2.0f);
			return;
		}

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}
	}

	public void OnClickDisableButton()
	{
		if (_selectedLevel < (PlayerData.instance.researchLevel + 1))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ResearchUI_Done"), 2.0f);
			return;
		}

		if (_selectedLevel > (PlayerData.instance.researchLevel + 1))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ResearchUI_FormerFirst"), 2.0f);
			return;
		}
	}
}