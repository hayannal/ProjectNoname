using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using PlayFab.ClientModels;

public class EquipBoxResultCanvas : MonoBehaviour
{
	public static EquipBoxResultCanvas instance;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public EquipListStatusInfo equipSmallStatusInfo;
	public RectTransform smallStatusBackBlurImage;

	public class CustomItemContainer : CachedItemHave<EquipCanvasListItem>
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
		smallStatusBackBlurImage.SetAsFirstSibling();
	}

	void OnEnable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		equipSmallStatusInfo.gameObject.SetActive(false);
		_materialSmallStatusInfoShowRemainTime = 0.0f;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	void Update()
	{
		UpdateEquipSmallStatusInfo();
	}

	List<EquipCanvasListItem> _listEquipCanvasListItem = new List<EquipCanvasListItem>();
	public void RefreshInfo(List<ItemInstance> listGrantItem)
	{
		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].gameObject.SetActive(false);
		_listEquipCanvasListItem.Clear();

		if (listGrantItem == null)
			return;

		for (int i = 0; i < listGrantItem.Count; ++i)
		{
			EquipData newEquipData = new EquipData();
			newEquipData.equipId = listGrantItem[i].ItemId;
			newEquipData.Initialize(listGrantItem[i].CustomData);

			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(newEquipData, OnClickListItem);
			_listEquipCanvasListItem.Add(equipCanvasListItem);
		}
	}

	public void OnClickListItem(EquipData equipData)
	{
		equipSmallStatusInfo.RefreshInfo(equipData, false);
		equipSmallStatusInfo.gameObject.SetActive(false);
		equipSmallStatusInfo.gameObject.SetActive(true);
		_materialSmallStatusInfoShowRemainTime = 2.0f;
	}

	float _materialSmallStatusInfoShowRemainTime;
	void UpdateEquipSmallStatusInfo()
	{
		if (_materialSmallStatusInfoShowRemainTime > 0.0f)
		{
			_materialSmallStatusInfoShowRemainTime -= Time.deltaTime;
			if (_materialSmallStatusInfoShowRemainTime <= 0.0f)
			{
				_materialSmallStatusInfoShowRemainTime = 0.0f;
				equipSmallStatusInfo.gameObject.SetActive(false);
			}
		}
	}

	public void OnClickExitButton()
	{
		gameObject.SetActive(false);
		RandomBoxScreenCanvas.instance.gameObject.SetActive(false);
	}
}