using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class SwapCanvas : MonoBehaviour
{
	public static SwapCanvas instance;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		RefreshSwapInfo();
	}

	void RefreshSwapInfo()
	{
		RefreshBossInfo();
		RefreshGrid();
	}

	void RefreshBossInfo()
	{

	}

	void RefreshGrid()
	{

	}

	float _buttonClickTime;
	public void OnClickYesButton()
	{
		_buttonClickTime = Time.time;
		DelayedLoadingCanvas.instance.gameObject.SetActive(true);

		// 제대로 하려면 BattleInstanceManager한테 혹시 이미 만들었던 플레이어 캐릭터인지를 물어보고
		// 있으면 해당 오브젝트를 가져오고 없으면 어드레스 로딩을 시작해야한다.
		// 우선은 강제로 동적로딩하게 해둔다.

		AddressableAssetLoadManager.GetAddressableGameObject("Ganfaul", "", OnLoadedPlayerActor);
	}

	void OnLoadedPlayerActor(GameObject prefab)
	{
		// 먼저 교체가능 UI를 끈다.
		PlayerIndicatorCanvas.Show(false, null);

		// 그리고 기존 캐릭터를 위치 정보 얻어온 후 꺼두고
		Vector3 position = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);

		// 새 캐릭터 생성 후
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(prefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(prefab);
#endif

		// 포지션 맞춰주고
		newObject.transform.position = position;

		// 이펙트를 출력

		// 레벨팩 이전

		// 걸린 시간 표시
		float deltaTime = Time.time - _buttonClickTime;
		Debug.LogFormat("Change Time : {0}", deltaTime);

		// 로딩 대기창 닫는다.
		DelayedLoadingCanvas.instance.gameObject.SetActive(false);

		// SwapCanvas를 닫는다.
		gameObject.SetActive(false);
	}
}