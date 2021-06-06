using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CumulativeEventImagePrefabInfo : MonoBehaviour
{
	public CumulativeEventData.eEventType eventType;
	public int day;

	public Text count1Text;
	public Text count2Text;

	void Start()
	{
		CumulativeEventData.EventRewardInfo eventRewardInfo = CumulativeEventData.instance.FindRewardInfo(eventType, day);
		if (eventRewardInfo == null)
			return;

		if (count1Text != null)
			count1Text.text = eventRewardInfo.count.ToString("N0");
		if (count2Text != null)
			count2Text.text = eventRewardInfo.count2.ToString("N0");
	}
}