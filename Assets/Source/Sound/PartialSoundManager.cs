using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Framework에는 핵심 플레이 함수만 두고 게임쪽 Source에다가 Addressable 로드해서 넘기는 코드를 작성한다.
public partial class SoundManager : MonoBehaviour
{
	string _reservedBGMAddress;
	float _fadeTime;
	public void PlayBgm(string address, float fadeTime)
	{
		_reservedBGMAddress = address;
		_fadeTime = fadeTime;
		AddressableAssetLoadManager.GetAddressableGameObject(address, "Sound", OnLoadedBGM);
	}

	void OnLoadedBGM(GameObject prefab)
	{
		BGMInfo bgmInfo = prefab.GetComponent<BGMInfo>();
		if (bgmInfo == null)
			return;

		// 여러번의 동시 호출이 일어났을때를 대비해서 마지막꺼인지 검사하는 로직이 필요하다.
		if (bgmInfo.addressForVerify != _reservedBGMAddress)
			return;

		PlayBgm(bgmInfo.audioClip, bgmInfo.volume, _fadeTime);
	}

	SFXContainer _inApkSFXContainer;
	public void LoadInApkSFXContainer()
	{
		// 이미 로드했으면 패스
		if (_inApkSFXContainer != null)
			return;

		AddressableAssetLoadManager.GetAddressableGameObject("InApkSFXContainer", "Sound", OnLoadedSFX);
	}

	void OnLoadedSFX(GameObject prefab)
	{
		SFXContainer sfxContainer = prefab.GetComponent<SFXContainer>();
		if (sfxContainer == null)
			return;

		_inApkSFXContainer = sfxContainer;
	}

	public void PlaySFX(string name)
	{
		if (_inApkSFXContainer == null)
			return;

		for (int i = 0; i < _inApkSFXContainer.sfxData.Count; ++i)
		{
			if (_inApkSFXContainer.sfxData[i].name == name)
			{
				PlaySFX(_inApkSFXContainer.sfxData[i].audioClip, _inApkSFXContainer.sfxData[i].volume);
				return;
			}
		}

		Debug.LogErrorFormat("Not found SFX. name = {0}", name);
	}


	#region Bgm Helper
	public void PlayLobbyBgm(float fadeTime = 1.0f)
	{
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(PlayerData.instance.selectedChapter);
		if (chapterTableData == null)
			return;
		PlayBgm(chapterTableData.chapterMusic, fadeTime);
	}

	public void PlayBattleBgm(string actorId, float fadeTime = 1.0f)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData != null && string.IsNullOrEmpty(actorTableData.battltMusicOverriding) == false)
		{
			PlayBgm(actorTableData.battltMusicOverriding, fadeTime);
			return;
		}
		PlayBgm("BGM_ChapterBattle", fadeTime);
	}

	public void PlayBossBgm(float fadeTime = 1.0f)
	{
		//PlayBgm()
	}

	public void PlayNodeWarBgm(float fadeTime = 1.0f)
	{

	}
	#endregion
}