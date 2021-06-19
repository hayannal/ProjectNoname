using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;

// 체험모드 관리를 위해 만든 데이터 클래스.
// 상태가 Played와 Rewarded로 나뉘면서 PlayerData 안에 넣기엔 꽤 커졌다. 그래서 따로 관리한다.
public class ExperienceData : MonoBehaviour
{
	public static ExperienceData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("ExperienceData")).AddComponent<ExperienceData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static ExperienceData _instance = null;

	public enum eExperienceState
	{
		None,
		Played,
		Rewarded,
	}

	Dictionary<string, int> _dicExperienceState = new Dictionary<string, int>();

	public void OnRecvData(string json)
	{
		_dicExperienceState.Clear();
		if (string.IsNullOrEmpty(json))
			return;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_dicExperienceState = serializer.DeserializeObject<Dictionary<string, int>>(json);
	}

	// 이미 플레이했음이 기록되어있는지 확인한다. 이걸 사용해서 연구메뉴 오픈전에 이미 플레이 했음을 기록해둔다.
	public bool IsPlayed(string actorId)
	{
		if (_dicExperienceState.ContainsKey(actorId) && _dicExperienceState[actorId] == (int)eExperienceState.Played)
			return true;
		return false;
	}

	// 이미 보상을 받았는지 확인한다. 이걸 사용해서 보상 받기가 가능한지 UI에 표시한다.
	public bool IsRewarded(string actorId)
	{
		if (_dicExperienceState.ContainsKey(actorId) && _dicExperienceState[actorId] == (int)eExperienceState.Rewarded)
			return true;
		return false;
	}

	public void OnUseUltimateSkill(string actorId)
	{
		// 이미 등록해둔 상태라면 더이상 처리할건 없다.
		if (IsRewarded(actorId) || IsPlayed(actorId))
			return;

		// 전투중이기 때문에 OkCanvas 띄우기엔 아닌거 같아서 inputLock 없이 패킷만 보내서 처리하는거로 하는게 나아보인다.
		// inputLock이 없으니 중복 호출의 위험이 있으므로 패킷 전송 후 클라에서도 선처리해서 두번 보내지 않도록 한다.
		bool onlyRecordPlay = false;
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Research))
		{
			// 연구가 열린 상태면 바로 보상까지 주면 된다.
		}
		else
		{
			// 그렇지 않다면 체험했음만 기록해두면 된다.
			onlyRecordPlay = true;
		}
		if (_dicExperienceState.ContainsKey(actorId))
			_dicExperienceState[actorId] = onlyRecordPlay ? (int)eExperienceState.Played : (int)eExperienceState.Rewarded;
		else
			_dicExperienceState.Add(actorId, onlyRecordPlay ? (int)eExperienceState.Played : (int)eExperienceState.Rewarded);
		PlayFabApiManager.instance.RequestExperience(actorId, onlyRecordPlay, !onlyRecordPlay, false, () =>
		{
			if (GuideQuestData.instance.CheckExperienceLevel(actorId, true))
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ExperienceLevel1);

			if (onlyRecordPlay == false)
			{
				if (GuideQuestData.instance.CheckExperienceLevel(actorId, false))
					GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ExperienceLevel2);

				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TryRewardToast"), 2.0f);
				if (CharacterInfoCanvas.instance != null && CharacterInfoCanvas.instance.gameObject.activeSelf)
					CharacterInfoCanvas.instance.currencySmallInfo.RefreshInfo();
				if (ExperienceGround.instance != null && ExperienceGround.instance.gameObject.activeSelf)
					ExperienceGround.instance.tryRewardTextObject.SetActive(false);
			}
		});
	}

	// 완료처리. 플레이를 이미 했던 캐릭이라면 체험 누를때 바로 획득처리로 넘어가게 해준다.
	public void ReceiveRewardWithoutPlay(string actorId)
	{
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Research) == false)
			return;

		// IsPlayed인 상황에 한해서만 호출된다.
		_dicExperienceState[actorId] = (int)eExperienceState.Rewarded;
		PlayFabApiManager.instance.RequestExperience(actorId, false, true, true, () =>
		{
			if (GuideQuestData.instance.CheckExperienceLevel(actorId, false))
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ExperienceLevel2);

			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TryRewardToastAlready"), 2.0f);
			if (CharacterInfoCanvas.instance != null && CharacterInfoCanvas.instance.gameObject.activeSelf)
				CharacterInfoCanvas.instance.currencySmallInfo.RefreshInfo();
			if (CharacterInfoGrowthCanvas.instance != null && CharacterInfoGrowthCanvas.instance.gameObject.activeSelf)
				CharacterInfoGrowthCanvas.instance.experienceRewardGroupObject.SetActive(false);
		});
	}
}