using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class BattleResultCanvas : MonoBehaviour
{
	public static BattleResultCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.battleResultPrefab).GetComponent<BattleResultCanvas>();
			}
			return _instance;
		}
	}
	static BattleResultCanvas _instance = null;

	public Text chapterRomanNumberText;
	public Text chapterNameText;
	public Button chapterInfoButton;
	public Image chapterInfoImage;

	public Text stageValueText;
	public Text stageValueMaxText;
	public Text clearText;
	public Text clearRewardText;

	public GameObject goldGroupObject;
	public DOTweenAnimation goldImageTweenAnimation;
	public Text goldValueText;

	public GameObject sealGroupObject;
	public RectTransform sealInfoButtonTransform;
	public RectTransform sealIconTransform;
	public RectTransform sealIconSubTransform;
	public Text sealCompletedText;
	public GameObject sealImageGroupObject;
	public GameObject[] sealCountImageList;
	public GameObject sealUnacquiredTextObject;

	public GameObject itemGroupObject;
	public Slider itemLineSlider;
	public GameObject gainItemTextObject;
	public GameObject itemScrollViewObject;
	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public GameObject exitGroupObject;

	public class CustomItemContainer : CachedItemHave<SwapCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		Time.timeScale = 0.0f;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		RefreshChapterInfo();
	}

	void OnDisable()
	{
		Time.timeScale = 1.0f;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	void Update()
	{
		UpdateStageText();
		UpdateGoldText();
	}

	bool _clear = false;
	void RefreshChapterInfo()
	{
		_clear = true;
		if (BattleInstanceManager.instance.playerActor.actorStatus.IsDie())
			_clear = false;

		int playStage = StageManager.instance.playStage;
		int maxStage = StageManager.instance.GetMaxStage(StageManager.instance.playChapter);
		if (_clear && playStage != maxStage)
			_clear = false;

		stageValueMaxText.text = string.Format("/ {0}", maxStage);

		if (StageManager.instance.playChapter == 0)
		{
			chapterRomanNumberText.text =  UIString.instance.GetString("GameUI_Chapter", "?");
			chapterNameText.SetLocalizedText(UIString.instance.GetString("GameUI_TrainingChapterName"));
			chapterInfoButton.interactable = false;
			chapterInfoImage.gameObject.SetActive(false);
			clearRewardText.SetLocalizedText(UIString.instance.GetString(_clear ? "GameUI_Chp0ClearRewardGot" : "GameUI_Chp0ClearReward"));
			clearRewardText.gameObject.SetActive(true);
			return;
		}

		if (StageManager.instance.playChapter == 1)
		{
			clearRewardText.SetLocalizedText(UIString.instance.GetString(_clear ? "GameUI_Chp1ClearRewardGot" : "GameUI_Chp1ClearReward"));
			clearRewardText.gameObject.SetActive(true);
		}

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;

		chapterRomanNumberText.text = SwapCanvas.GetChapterRomanNumberString(StageManager.instance.playChapter);
		if (PlayerData.instance.chaosMode)
			chapterNameText.SetLocalizedText(UIString.instance.GetString("GameUI_ChaosMode"));
		else
			chapterNameText.SetLocalizedText(UIString.instance.GetString(chapterTableData.nameId));
	}

	public void OnClickChapterInfoButton()
	{
		string descriptionId = "";
		if (PlayerData.instance.chaosMode)
			descriptionId = "GameUI_ChaosModeDesc";
		else
		{
			ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
			if (chapterTableData != null)
				descriptionId = chapterTableData.descriptionId;
		}

		if (string.IsNullOrEmpty(descriptionId))
			return;

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(descriptionId), 200, chapterInfoButton.transform, new Vector2(0.0f, -35.0f));
	}

	#region Stage
	public void OnEventIncreaseStage()
	{
		_stageChangeRemainTime = stageChangeTime;
		_stageChangeSpeed = (StageManager.instance.playStage - 1) / _stageChangeRemainTime;
		_currentStage = 1.0f;
		_updateStageText = true;

		StartCoroutine(StageProcess());
	}

	IEnumerator StageProcess()
	{
		yield return new WaitForSecondsRealtime(stageChangeTime);

		if (_clear)
			clearText.gameObject.SetActive(true);

		yield return new WaitForSecondsRealtime(0.2f);

		goldGroupObject.SetActive(true);
	}

	const float stageChangeTime = 0.4f;
	float _stageChangeRemainTime;
	float _stageChangeSpeed;
	float _currentStage;
	int _lastStage;
	bool _updateStageText;
	void UpdateStageText()
	{
		if (_updateStageText == false)
			return;

		_currentStage += _stageChangeSpeed * Time.unscaledDeltaTime;
		int currentStageInt = (int)_currentStage;
		if (currentStageInt >= StageManager.instance.playStage)
		{
			currentStageInt = StageManager.instance.playStage;
			_updateStageText = false;
		}
		if (currentStageInt != _lastStage)
		{
			_lastStage = currentStageInt;
			stageValueText.text = _lastStage.ToString();
		}
	}
	#endregion

	#region Gold
	public void OnEventIncreaseGold()
	{
		_goldChangeRemainTime = goldChangeTime;
		_goldChangeSpeed = DropManager.instance.GetStackedDropGold() / _goldChangeRemainTime;
		_currentGold = 0.0f;
		_updateGoldText = true;

		goldImageTweenAnimation.DOPlay();

		StartCoroutine(GoldProcess());
	}

	IEnumerator GoldProcess()
	{
		yield return new WaitForSecondsRealtime(goldChangeTime);
		
		if (StageManager.instance.playChapter == 0)
		{
			yield return new WaitForSecondsRealtime(0.2f);
			exitGroupObject.SetActive(true);
			yield break;
		}

		if (PlayerData.instance.sharedDailyBoxOpened)
			sealCompletedText.gameObject.SetActive(true);
		else
		{
			int sealCount = DropManager.instance.GetStackedDropSeal();
			if (sealCount == 0)
			{
				sealUnacquiredTextObject.SetActive(true);
			}
			else if (sealCount <= 6)
			{
				sealImageGroupObject.SetActive(true);
			}
			else
			{
				sealImageGroupObject.SetActive(true);
				sealIconTransform.anchoredPosition = sealIconSubTransform.anchoredPosition;
			}
		}

		sealGroupObject.SetActive(true);

		yield return null;
		yield return null;

		sealInfoButtonTransform.pivot = new Vector2(0.5f, sealInfoButtonTransform.pivot.y);
		sealInfoButtonTransform.anchoredPosition = new Vector2(sealInfoButtonTransform.sizeDelta.x * 0.5f, sealInfoButtonTransform.anchoredPosition.y);
	}

	const float goldChangeTime = 0.4f;
	float _goldChangeRemainTime;
	float _goldChangeSpeed;
	float _currentGold;
	int _lastGold;
	bool _updateGoldText;
	void UpdateGoldText()
	{
		if (_updateGoldText == false)
			return;

		_currentGold += _goldChangeSpeed * Time.unscaledDeltaTime;
		int currentGoldInt = (int)_currentGold;
		if (currentGoldInt >= DropManager.instance.GetStackedDropGold())
		{
			currentGoldInt = DropManager.instance.GetStackedDropGold();
			_updateGoldText = false;
		}
		if (currentGoldInt != _lastGold)
		{
			_lastGold = currentGoldInt;
			goldValueText.text = _lastGold.ToString();
		}
	}
	#endregion

	#region Seal
	public void OnEventIncreaseSeal()
	{
		StartCoroutine(SealProcess());
	}

	IEnumerator SealProcess()
	{
		if (sealCompletedText.gameObject.activeSelf || sealUnacquiredTextObject.activeSelf)
		{
			yield return new WaitForSecondsRealtime(0.2f);
			itemGroupObject.SetActive(true);
			StartCoroutine(ItemProcess());
			yield break;
		}

		if (sealImageGroupObject.activeSelf)
		{
			int sealCount = DropManager.instance.GetStackedDropSeal();
			for (int i = 0; i < sealCount; ++i)
			{
				sealCountImageList[i].SetActive(true);
				yield return new WaitForSecondsRealtime(0.2f);
			}

			itemGroupObject.SetActive(true);
			StartCoroutine(ItemProcess());
		}
	}

	public void OnClickSealInfoButton()
	{
		string text = UIString.instance.GetString("GameUI_SealDesc");

		// check highest
		if (PlayerData.instance.highestPlayChapter != PlayerData.instance.selectedChapter && PlayerData.instance.sharedDailyBoxOpened == false)
			text = string.Format("{0}.\n\n{1}.", text, UIString.instance.GetString("GameUI_SealAddedDesc"));

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 200, sealInfoButtonTransform, new Vector2(25.0f, -35.0f));
	}
	#endregion

	#region Item
	IEnumerator ItemProcess()
	{
		DOTween.To(() => itemLineSlider.value, x => itemLineSlider.value = x, 1.0f, 0.4f).SetEase(Ease.Linear).SetUpdate(true);

		yield return new WaitForSecondsRealtime(0.3f);

		gainItemTextObject.SetActive(true);

		yield return new WaitForSecondsRealtime(0.1f);

		if (DropManager.instance.GetDropItemCount() == 0)
		{
			yield return new WaitForSecondsRealtime(0.2f);
			exitGroupObject.SetActive(true);
			yield break;
		}

		itemScrollViewObject.SetActive(true);
	}

	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	void RefreshGrid()
	{
		
	}

	public void OnClickListItem(string actorId)
	{
		
	}
	#endregion

	public void OnClickExitButton()
	{
		SceneManager.LoadScene(0);
	}
}