using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class PlayerIndicatorCanvas : ObjectIndicatorCanvas
{
	static PlayerIndicatorCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.playerIndicatorPrefab).GetComponent<PlayerIndicatorCanvas>();
#if UNITY_EDITOR
				AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
				if (settings.ActivePlayModeDataBuilderIndex == 2)
					ObjectUtil.ReloadShader(_instance.gameObject);
#endif
			}
			return _instance;
		}
	}
	static PlayerIndicatorCanvas _instance = null;

	public static void Show(bool show, Transform targetTransform)
	{
		if (show)
		{
			instance.targetTransform = targetTransform;
			instance.gameObject.SetActive(true);
		}
		else
		{
			if (_instance == null)
				return;
			_instance.gameObject.SetActive(false);
		}
	}

	public GameObject buttonRootObject;
	public Button buttonGroup;

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
	}

	void OnDisable()
	{
		buttonRootObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
		UpdateObjectIndicator();
	}

	public void OnClickSwapButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("SwapCanvas", null);
	}
}
