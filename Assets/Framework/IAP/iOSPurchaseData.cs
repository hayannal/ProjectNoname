using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct iOSReceiptData
{
	public string Store;
	public string TransactionID;
	public string Payload;

	public iOSReceiptData(string json)
	{
		iOSReceiptData data = JsonUtility.FromJson<iOSReceiptData>(json);
		this = data;
	}
}