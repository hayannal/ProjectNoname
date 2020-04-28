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

	public void RefreshInfo(bool origin)
	{
		// 인자가 없이 오면 DropManager에 들어있는걸 보여준다.
		int addGold = DropManager.instance.GetLobbyGoldAmount();
		int addDia = DropManager.instance.GetLobbyDiaAmount();
		List<DropManager.CharacterPpRequest> listPpInfo = DropManager.instance.GetPowerPointInfo();
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		List<DropManager.CharacterLbpRequest> listLbpInfo = DropManager.instance.GetLimitBreakPointInfo();
		RefreshInfo(origin, addGold, addDia, listPpInfo, listGrantInfo, listLbpInfo);
	}

	bool _origin;
	List<CharacterBoxResultListItem> _listOriginResultItem = new List<CharacterBoxResultListItem>();
	List<CharacterBoxResultListItem> _listResultListItem = new List<CharacterBoxResultListItem>();
	public void RefreshInfo(bool origin, int addGold, int addDia, List<DropManager.CharacterPpRequest> listPpInfo, List<string> listGrantInfo, List<DropManager.CharacterLbpRequest> listLbpInfo)
	{
		// 오리진일때랑 아닐때의 상단 영역 조정
		_origin = origin;
		goldDiaRootRectTransform.gameObject.SetActive(origin);
		if (origin)
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
		int originCount = listGrantInfo.Count + listLbpInfo.Count;
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
			resultListItem.characterListItem.Initialize(listGrantInfo[i], powerLevel, 0, null, null);
			resultListItem.Initialize("ShopUI_NewCharacter", 0);
			_listOriginResultItem.Add(resultListItem);
		}

		for (int i = 0; i < listLbpInfo.Count; ++i)
		{
			CharacterBoxResultListItem resultListItem = _originContainer.GetCachedItem(originContentItemPrefab, originContentRootRectTransform);
			int powerLevel = 0;
			CharacterData characterData = PlayerData.instance.GetCharacterData(listLbpInfo[i].actorId);
			if (characterData != null)
				powerLevel = characterData.powerLevel;
			resultListItem.characterListItem.Initialize(listLbpInfo[i].actorId, powerLevel, 0, null, null);
			resultListItem.Initialize("ShopUI_LimitBreakReward", 0);
			_listOriginResultItem.Add(resultListItem);
		}

		originContentRootRectTransform.gameObject.SetActive(needOriginGroup);
		ppLineObject.SetActive(needOriginGroup);

		for (int i = 0; i < listPpInfo.Count; ++i)
		{
			CharacterBoxResultListItem resultListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			int powerLevel = 0;
			CharacterData characterData = PlayerData.instance.GetCharacterData(listPpInfo[i].actorId);
			if (characterData != null)
				powerLevel = characterData.powerLevel;
			resultListItem.characterListItem.Initialize(listPpInfo[i].actorId, powerLevel, 0, null, null);
			resultListItem.Initialize("", listPpInfo[i].pp);
			_listResultListItem.Add(resultListItem);

			// 빈슬롯과 함께 포함되어있는채로 재활용 해야하니 형제들 중 가장 마지막으로 밀어서 순서를 맞춘다.
			resultListItem.cachedRectTransform.SetAsLastSibling();
		}

		// 모든 표시가 끝나면 DropManager에 있는 정보를 강제로 초기화 시켜줘야한다.
		DropManager.instance.ClearLobbyDropInfo();
	}

	public void OnClickExitButton()
	{
		if (_origin)
			TreasureChest.instance.HideIndicatorCanvas(false);

		gameObject.SetActive(false);
		RandomBoxScreenCanvas.instance.gameObject.SetActive(false);
	}
}