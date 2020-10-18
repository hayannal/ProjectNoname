using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDownloadCanvas : MonoBehaviour
{
	public static LobbyDownloadCanvas instance;

	public Image progressImage;
	public Text progressText;

	void Awake()
	{
		instance = this;
	}
}