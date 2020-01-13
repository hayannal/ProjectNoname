using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossDescriptionObjectIndicatorCanvas : ObjectIndicatorCanvas
{
	public Text bossNameText;
	public GameObject infoRootObject;
	public Transform imageRootTransform;

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
		RefreshBossInfo();
	}

	void OnDisable()
	{
		infoRootObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
		UpdateObjectIndicator();
	}

	GameObject _cachedImageObject;
	void RefreshBossInfo()
	{
		if (_cachedImageObject != null)
		{
			_cachedImageObject.SetActive(false);
			_cachedImageObject = null;
		}

		MapTableData nextBossMapTableData = StageManager.instance.nextBossMapTableData;
		if (nextBossMapTableData == null)
			return;

		if (string.IsNullOrEmpty(nextBossMapTableData.bossName) == false)
		{
			AddressableAssetLoadManager.GetAddressableGameObject(string.Format("Indicator_{0}", nextBossMapTableData.bossName), "Preview", (prefab) =>
			{
				_cachedImageObject = UIInstanceManager.instance.GetCachedObject(prefab, imageRootTransform);
			});
		}
		bossNameText.SetLocalizedText(UIString.instance.GetString(nextBossMapTableData.nameId));
	}
}