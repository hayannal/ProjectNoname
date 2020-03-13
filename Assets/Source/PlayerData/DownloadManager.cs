using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownloadManager : MonoBehaviour
{
	public static DownloadManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("DownloadManager")).AddComponent<DownloadManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static DownloadManager _instance = null;

	public bool IsDownloaded()
	{
		// apk받고 다운로드하지 않은 상태라면 false를 리턴한다.
		// 사용하는 케이스는 두가지일거 같다.
		//
		// 1챕터 씬으로 MainScene을 빌딩하려고 할때 다운로드 하지 않았다면 0챕터를 로드하며 다운로드 모드로 전환되는 것.
		// 혹은 훈련챕터 1층에서 5층까지 진행중에 계정연동 버튼을 눌렀다면 다운로드했는지를 판단해서 받을지다.
		// 우선 지금은 풀빌드를 사용하기때문에 항상 true를 리턴하게 해둔다.
		return true;
	}

	// 다운로드 받아야 진행할 수 있게 된 상태.
	// 아마도 0챕터를 깨서 1챕터로 넘어간 상태고 로비로 들어왔는데
	// 번들 데이터가 없어서 로딩하지 못한 상태일거다.
	// 이땐 0챕터로 강제로 로드하며 이후 스테이지 캐싱도 안하고 전투 진입도 못하게 막혀야한다.
	public bool standbyDownload { get; set; }
}