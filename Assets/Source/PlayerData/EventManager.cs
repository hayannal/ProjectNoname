using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
	public static EventManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("EventManager")).AddComponent<EventManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static EventManager _instance = null;

	public void OnEventClearChapter(int chapter, string newCharacterId)
	{
		// 정산창에서 호출될거다. 인벤 동기화 패킷을 따로 날려도 되긴한데 괜히 시간걸릴까봐 클라단에서 선처리해서 캐릭터 넣어둔다. 아이디는 전달받은거로 셋팅
		if (chapter == 1)
		{
			PlayerData.instance.AddNewCharacter("Actor002", newCharacterId);

			// 연출 플래그도 걸어야한다.(이건 서버에서 걸어주니 할필요 없나..)

			// 클라용 챕터 표시 플래그도 걸어야한다.
		}
		else if (chapter == 2)
		{
			PlayerData.instance.AddNewCharacter("Actor003", newCharacterId);
		}
	}
}