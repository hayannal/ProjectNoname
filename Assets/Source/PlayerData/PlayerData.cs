using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	public bool tutorialChapter { get { return true; } }



	void Awake()
	{
		// temp
		OnRecvCharacterList();
	}



	#region Character List
	List<CharacterData> _listCharacterData = new List<CharacterData>();

	public void OnRecvCharacterList()
	{
		// 지금은 패킷 구조를 모르니.. 형태만 만들어두기로 한다.
		// list를 먼저 쭉 받아서 기억해두고 메인 캐릭터 설정하면 될듯

		// list

		// 

		CharacterData characterData = new CharacterData();
		characterData.actorId = "Actor001";
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor002";
		_listCharacterData.Add(characterData);
	}

	string _mainCharacterId = "Actor002";
	public string mainCharacterId
	{
		get
		{
			// 디비에 저장되어있는 메인 캐릭터를 리턴
			// 우선은 임시
			return _mainCharacterId;
		}
	}

	public bool swappable { get { return _listCharacterData.Count > 1; } }
	#endregion
}
