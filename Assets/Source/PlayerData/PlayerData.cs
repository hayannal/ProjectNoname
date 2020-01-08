using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

public class PlayerData : MonoBehaviour
{
	public static PlayerData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("PlayerData")).AddComponent<PlayerData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static PlayerData _instance = null;

	public bool loginned { get; private set; }

	// 변수 이름이 헷갈릴 수 있는데 로직상 이게 가장 필요한 정보라 그렇다.
	// 하나는 최대로 플레이한 챕터 번호고 하나는 최대로 클리어한 스테이지 번호다.
	// 무슨 말이냐 하면
	// 0-20 하다 죽었으면 highestPlayChapter는 0 / highestClearStage는 19가 저장되는 형태다.
	// 1-50 하다 죽었으면 highestPlayChapter는 1 / highestClearStage는 49가 저장되는 형태다.
	// 1-50 의 보스를 잡고 게이트 필라 치기 전에 죽었으면 위와 마찬가지로 highestPlayChapter는 1 / highestClearStage는 49가 저장되는 형태다.
	// 1-50 의 보스를 잡고 게이트 필라를 쳐서 결과창이 나왔으면 highestPlayChapter는 2 / highestClearStage는 0이 저장되는 형태다.
	// 이래야 깔끔하게 두개만 저장해서 모든걸 처리할 수 있다.
	public ObscuredInt highestPlayChapter { get; set; }
	public ObscuredInt highestClearStage { get; set; }
	public ObscuredInt selectedChapter { get; set; }
	public ObscuredBool chaosMode { get; set; }
	public ObscuredInt purifyCount { get; set; }

	public bool tutorialChapter { get { return highestPlayChapter == 0; } }

	#region Player Info
	public void OnRecvPlayerInfo()
	{
		// 디비 및 훈련챕터 들어가기 전까지 임시로 쓰는 값이다. 1챕터 정보를 부른다.
		highestPlayChapter = 2;
		highestClearStage = 0;
		selectedChapter = 1;

		// temp
		loginned = true;
	}
	#endregion


	#region Character List
	List<CharacterData> _listCharacterData = new List<CharacterData>();
	public List<CharacterData> listCharacterData { get { return _listCharacterData; } }

	public void OnRecvCharacterList()
	{
		// 지금은 패킷 구조를 모르니.. 형태만 만들어두기로 한다.
		// list를 먼저 쭉 받아서 기억해두고 메인 캐릭터 설정하면 될듯

		// list

		// 

		CharacterData characterData = new CharacterData();
		characterData.actorId = "Actor001";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor002";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor003";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor004";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor005";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor006";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor007";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor008";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor009";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor010";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor011";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor012";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor013";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor014";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor015";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor016";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor017";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor018";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor019";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor020";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor021";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor022";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor024";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor025";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor026";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor028";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor029";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor030";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor031";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor033";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor035";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor036";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor037";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor038";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor039";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor040";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor041";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
	}

	public CharacterData GetCharacterData(string actorId)
	{
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			if (_listCharacterData[i].actorId == actorId)
				return _listCharacterData[i];
		}
		return null;
	}

	string _mainCharacterId = "Actor001";
	public string mainCharacterId
	{
		get
		{
			// 디비에 저장되어있는 메인 캐릭터를 리턴
			// 우선은 임시
			return _mainCharacterId;
		}
		set
		{
			_mainCharacterId = value;
		}
	}

	public bool swappable { get { return _listCharacterData.Count > 1; } }

	public bool ContainsActor(string actorId)
	{
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			if (_listCharacterData[i].actorId == actorId)
				return true;
		}
		return false;
	}
	#endregion
}
