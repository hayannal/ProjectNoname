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
}
