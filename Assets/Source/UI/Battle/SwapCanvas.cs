using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using System.Text;

public class SwapCanvas : MonoBehaviour
{
	public static SwapCanvas instance;

	public Text suggestText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		if (MainSceneBuilder.instance.lobby)
			RefreshChapterInfo();
		else
			RefreshSwapInfo();
	}

	void RefreshChapterInfo()
	{

	}

	void RefreshSwapInfo()
	{
		RefreshBossInfo();
		RefreshGrid();
	}

	void RefreshBossInfo()
	{
		if (StageManager.instance.nextMapTableData == null)
			return;
		if (string.IsNullOrEmpty(StageManager.instance.currentBossPreviewAddress))
			return;

		string suggestString = GetSuggestString(StageManager.instance.nextMapTableData.descriptionId, StageManager.instance.nextMapTableData.suggestedActorId);
		//suggestText.SetLocalizedText(suggestString);
	}

	void RefreshGrid()
	{

	}

	float _buttonClickTime;
	public void OnClickYesButton()
	{
		_buttonClickTime = Time.time;
		DelayedLoadingCanvas.instance.gameObject.SetActive(true);

		string changeActorId = "Actor001";

		// 이미 만들었던 플레이어 캐릭터라면 다시 만들필요 없으니 가져다쓰고 없으면 어드레스 로딩을 시작해야한다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(changeActorId);
		if (playerActor != null)
		{
			SwapCharacter(playerActor);
			return;
		}
		AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(changeActorId), "", OnLoadedPlayerActor);
	}

	void OnLoadedPlayerActor(GameObject prefab)
	{
		// 새 캐릭터 생성 후
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(prefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(prefab);
#endif

		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor == null)
			return;

		SwapCharacter(playerActor);
	}

	void SwapCharacter(PlayerActor newPlayerActor)
	{
		// 먼저 교체가능 UI를 끈다.
		PlayerIndicatorCanvas.Show(false, null);

		// PlayerActor에 Swap을 알린다.
		BattleInstanceManager.instance.standbySwapPlayerActor = true;

		// 미리 꺼두면 레벨팩 이전받을 캐릭을 찾지 못해서 안된다. 다 이전시키고 스왑 처리하는 곳에서 알아서 끌테니 그냥 두면 된다.
		//BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);

		// 그리고 기존 캐릭터를 위치 정보 얻어온 후 꺼두고
		Vector3 position = BattleInstanceManager.instance.playerActor.cachedTransform.position;

		// 새 캐릭터 재활성화
		if (newPlayerActor.gameObject.activeSelf == false)
			newPlayerActor.gameObject.SetActive(true);

		// 포지션 맞춰주고
		newPlayerActor.cachedTransform.position = position;

		// 이펙트를 출력

		/////////////////////////////////////////////////////////////////////
		// 여기서 제일 문제가..
		// 생성 직후엔 Start가 호출되지 않은 상태라서 스탯 계산이 아직 안되어있다.
		// 같은 프레임이긴 한데 여기 Swap코드보다 나중에 호출되는 구조다.
		// 결국 순서 맞추려면 PlayerActor Start할때 RegisterBattleInstance 호출될때 Initialize 다 끝난 후
		// 기존 플레이어액터 정보 구해와서 피, 스왑힐,레벨팩 이전 하는게 가장 맞다.
		// 여기서는 아무것도 하지 않는다.
		/////////////////////////////////////////////////////////////////////

		// 걸린 시간 표시
		float deltaTime = Time.time - _buttonClickTime;
		Debug.LogFormat("Change Time : {0}", deltaTime);

		// 로딩 대기창 닫는다.
		DelayedLoadingCanvas.instance.gameObject.SetActive(false);

		// SwapCanvas를 닫는다.
		gameObject.SetActive(false);
	}

	StringBuilder _stringBuilderFull = new StringBuilder();
	StringBuilder _stringBuilderActor = new StringBuilder();
	string GetSuggestString(string descriptionId, string[] suggestedActorIdList)
	{
		_stringBuilderFull.Remove(0, _stringBuilderFull.Length);
		_stringBuilderActor.Remove(0, _stringBuilderActor.Length);
		for (int i = 0; i < suggestedActorIdList.Length; ++i)
		{
			string actorId = suggestedActorIdList[i];
			if (PlayerData.instance.ContainsActor(actorId) == false)
				continue;
			if (_stringBuilderActor.Length > 0)
				_stringBuilderActor.Append(", ");
			_stringBuilderActor.Append(CharacterData.GetNameByActorId(actorId));
		}
		if (_stringBuilderActor.Length == 0)
			_stringBuilderActor.Append(CharacterData.GetNameByActorId(suggestedActorIdList[0]));
		_stringBuilderFull.AppendFormat(UIString.instance.GetString(descriptionId), _stringBuilderActor.ToString());
		return _stringBuilderFull.ToString();
	}
}