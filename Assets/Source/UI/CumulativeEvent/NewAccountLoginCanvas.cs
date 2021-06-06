using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewAccountLoginCanvas : MonoBehaviour
{
	public Text headerText;

	void OnEnable()
	{
		int currentLoginEventCount = CumulativeEventData.instance.newAccountLoginEventCount;

	}
}