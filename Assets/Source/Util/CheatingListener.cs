using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatingListener : MonoBehaviour
{
	public void OnDetect()
	{
		Application.Quit();
	}
}
