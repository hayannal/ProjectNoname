using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class SFXContainer : MonoBehaviour
{
	[System.Serializable]
	public class SoundData
	{
		public string name;
		public AudioClip audioClip;

		[Range(0.0f, 2.0f)]
		public float volume = 1.0f;
	}

	[ReorderableList]
	public List<SoundData> sfxData;
}