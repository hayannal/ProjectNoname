using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterBoxResultCanvas : MonoBehaviour
{
	public static CharacterBoxResultCanvas instance;

	public RectTransform goldDiaRootRectTransform;
	public RectTransform characterRootRectTransform;

	public Text goldValueText;
	public RectTransform diaGroupRectTransform;
	public Text diaValueText;

	public Text gainCharacterText;

	public GameObject originContentItemPrefab;
	public RectTransform originContentRootRectTransform;
	public GameObject ppLineObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public GameObject levelUpPossibleTextObject;

	public class CustomItemSubContainer : CachedItemHave<CharacterBoxResultListItem>
	{
	}
	CustomItemSubContainer _originContainer = new CustomItemSubContainer();

	public class CustomItemContainer : CachedItemHave<CharacterBoxResultListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
		originContentItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	public void RefreshInfo()
	{
		// 인자가 없이 오면 DropManager에 들어있는걸 보여준다.
		int addGold = DropManager.instance.GetLobbyGoldAmount();
		int addDia = DropManager.instance.GetLobbyDiaAmount();
		List<DropManager.CharacterPpRequest> listPpInfo = DropManager.instance.GetPowerPointInfo();
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		List<DropManager.CharacterTrpRequest> listTrpInfo = DropManager.instance.GetTranscendPointInfo();
		RefreshInfo(addGold, addDia, listPpInfo, listGrantInfo, listTrpInfo);
	}

	List<CharacterBoxResultListItem> _listOriginResultItem = new List<CharacterBoxResultListItem>();
	List<CharacterBoxResultListItem> _listResultListItem = new List<CharacterBoxResultListItem>();
	public void RefreshInfo(int addGold, int addDia, List<DropManager.CharacterPpRequest> listPpInfo, List<string> listGrantInfo, List<DropManager.CharacterTrpRequest> listTrpInfo)
	{
		// 골드나 다이아가 
		bool goldDia = (addGold > 0) || (addDia > 0);
		goldDiaRootRectTransform.gameObject.SetActive(goldDia);
		if (goldDia)
		{
			characterRootRectTransform.offsetMax = new Vector2(characterRootRectTransform.offsetMax.x, -goldDiaRootRectTransform.sizeDelta.y);
			goldValueText.text = addGold.ToString("N0");
			diaGroupRectTransform.gameObject.SetActive(addDia > 0);
			diaValueText.text = addDia.ToString("N0");
		}
		else
		{
			characterRootRectTransform.offsetMax = Vector2.zero;
		}

		bool needOriginGroup = false;
		int originCount = listGrantInfo.Count + listTrpInfo.Count;
		if (originCount == 0)
		{
			// 구분선 없이 pp리스트만 출력한다.
			gainCharacterText.SetLocalizedText(UIString.instance.GetString("ShopUI_PpReward"));
		}
		else
		{
			gainCharacterText.SetLocalizedText(UIString.instance.GetString("ShopUI_CharacterReward"));
			needOriginGroup = true;
		}

		for (int i = 0; i < _listResultListItem.Count; ++i)
			_listResultListItem[i].gameObject.SetActive(false);
		_listResultListItem.Clear();
		for (int i = 0; i < _listOriginResultItem.Count; ++i)
			_listOriginResultItem[i].gameObject.SetActive(false);
		_listOriginResultItem.Clear();

		for (int i = 0; i < listGrantInfo.Count; ++i)
		{
			CharacterBoxResultListItem resultListItem = _originContainer.GetCachedItem(originContentItemPrefab, originContentRootRectTransform);
			int powerLevel = 0;
			CharacterData characterData = PlayerData.instance.GetCharacterData(listGrantInfo[i]);
			if (characterData != null)
				powerLevel = characterData.powerLevel;
			resultListItem.characterListItem.Initialize(listGrantInfo[i], powerLevel, 0, 0, null, null);
			resultListItem.Initialize("ShopUI_NewCharacter", 0);
			_listOriginResultItem.Add(resultListItem);
		}

		for (int i = 0; i < listTrpInfo.Count; ++i)
		{
			CharacterBoxResultListItem resultListItem = _originContainer.GetCachedItem(originContentItemPrefab, originContentRootRectTransform);
			int powerLevel = 0;
			CharacterData characterData = PlayerData.instance.GetCharacterData(listTrpInfo[i].actorId);
			if (characterData != null)
				powerLevel = characterData.powerLevel;
			resultListItem.characterListItem.Initialize(listTrpInfo[i].actorId, powerLevel, 0, 0, null, null);
			resultListItem.Initialize("ShopUI_TranscendReward", 0);
			_listOriginResultItem.Add(resultListItem);
		}

		originContentRootRectTransform.gameObject.SetActive(needOriginGroup);
		ppLineObject.SetActive(needOriginGroup);

		bool levelUpPossible = false;
		for (int i = 0; i < listPpInfo.Count; ++i)
		{
			CharacterBoxResultListItem resultListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			int powerLevel = 0;
			bool showPlusAlarm = false;
			CharacterData characterData = PlayerData.instance.GetCharacterData(listPpInfo[i].actorId);
			if (characterData != null)
			{
				powerLevel = characterData.powerLevel;
				showPlusAlarm = characterData.IsPlusAlarmState();
			}
			resultListItem.characterListItem.Initialize(listPpInfo[i].actorId, powerLevel, 0, 0, null, null);
			resultListItem.characterListItem.ShowAlarm(false);
			if (showPlusAlarm)
			{
				resultListItem.characterListItem.ShowAlarm(true, true);
				levelUpPossible = true;
			}
			resultListItem.Initialize("", listPpInfo[i].add);
			_listResultListItem.Add(resultListItem);

			// 빈슬롯과 함께 포함되어있는채로 재활용 해야하니 형제들 중 가장 마지막으로 밀어서 순서를 맞춘다.
			resultListItem.cachedRectTransform.SetAsLastSibling();
		}
		levelUpPossibleTextObject.SetActive(levelUpPossible);

		// pp 획득가능한 곳이라 호출
		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.RefreshCharacterAlarmObject(false);
		LobbyCanvas.instance.RefreshTutorialPlusAlarmObject();

		// pp 뿐만 아니라 transcendPoint도 획득 가능한 곳이다.
		if (listTrpInfo.Count > 0)
		{
			if (DotMainMenuCanvas.instance != null)
				DotMainMenuCanvas.instance.RefreshCharacterAlarmObject(false);
			LobbyCanvas.instance.RefreshAlarmObject(DotMainMenuCanvas.eButtonType.Character, true);

			// 초월메뉴 최초로 보이는 것도 확인해야한다.
			if (CharacterInfoCanvas.instance != null)
				CharacterInfoCanvas.instance.RefreshOpenMenuSlotByTranscendPoint();
		}

		// 모든 표시가 끝나면 DropManager에 있는 정보를 강제로 초기화 시켜줘야한다.
		DropManager.instance.ClearLobbyDropInfo();
	}

	public void OnClickExitButton()
	{
		gameObject.SetActive(false);
		RandomBoxScreenCanvas.instance.gameObject.SetActive(false);
	}
}