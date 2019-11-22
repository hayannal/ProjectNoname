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
		RefreshGrid();
	}

	void RefreshChapterInfo()
	{
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;

		if (PlayerData.instance.chaosMode)
		{
			// 카오스 모드에선 suggest 설명이 의미없으므로 표시하지 않는다.
		}
		else
		{
			// 챕터 시작에서도 사실 미리 구축해둔 정보로 10층에 나올 보스를 알 수 있지만
			// 재접시 랜덤으로 바뀔 수 있는 이 정보를 보여주는게 이상한데다가
			// 챕터 설명인데 10층 정보가 나오는건 정말 안맞기 때문에
			// 차라리 챕터의 권장 시작 캐릭터를 설정해주는 문구를 표시하는거다.
			string suggestString = GetSuggestString(chapterTableData.descriptionId, chapterTableData.suggestedActorId);
			//suggestText.SetLocalizedText(suggestString);
		}

		// 챕터 디버프 어펙터는 로비 바로 다음 스테이지에서 뽑아와서 표시해준다.(여기서 넣는거 아니다. 보여주기만 한다.)
		// 없으면 표시하지 않는다.
		// 실제로 넣는건 해당 시점에서 하니 여기서는 신경쓰지 않아도 된다.
		// 카오스에서는 여러개 들어있을 수도 있는데 이땐 아마 설명창에 여러개 중 하나가 되는 식이라고 표시될거다. 통합 스트링 제공.
		// 사실 챕터에 넣을 수 있지만 스테이지에 연결해두는 이유가
		// 언젠가 나중에 챕터 중간에도 이 디버프를 변경시킬 상황이 올까봐 미리 확장시켜서 여기에 두는 것이다.
		if (StageDataManager.instance.existNextStageInfo)
		{
			string penaltyString = "";
			if (!string.IsNullOrEmpty(StageDataManager.instance.nextStageTableData.penaltyRepresentative))
			{
				string[] penaltyParameterList = UIString.instance.ParseParameterString(StageDataManager.instance.nextStageTableData.repreParameter);
				penaltyString = UIString.instance.GetString(StageDataManager.instance.nextStageTableData.penaltyRepresentative, penaltyParameterList);
			}
			else
			{
				if (StageDataManager.instance.nextStageTableData.stagePenaltyId.Length == 1)
				{
					// 패널티가 하나만 있을땐 직접 구해와서
				}
			}
		}

		// 파워레벨은 항상 표시
		//chapterTableData.suggestedPowerLevel
	}

	void RefreshSwapInfo()
	{
		MapTableData nextBossMapTableData = StageManager.instance.nextBossMapTableData;
		if (nextBossMapTableData == null)
			return;

		string suggestString = GetSuggestString(nextBossMapTableData.descriptionId, nextBossMapTableData.suggestedActorId);
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