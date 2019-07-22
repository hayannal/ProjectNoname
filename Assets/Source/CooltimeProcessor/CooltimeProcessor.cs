using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CooltimeProcessor : MonoBehaviour {

	Dictionary<string, Cooltime> _dicCooltimeInfo = new Dictionary<string, Cooltime>();

	void OnEnable()
	{
		Dictionary<string, Cooltime>.Enumerator e = _dicCooltimeInfo.GetEnumerator();
		while (e.MoveNext())
			e.Current.Value.cooltime = 0.0f;
	}

	public Cooltime ApplyCooltime(string cooltimeId, float maxCooltime)
	{
		Cooltime cooltimeInfo = null;
		if (_dicCooltimeInfo.ContainsKey(cooltimeId))
			cooltimeInfo = _dicCooltimeInfo[cooltimeId];
		else
		{
			cooltimeInfo = new Cooltime();
			_dicCooltimeInfo.Add(cooltimeId, cooltimeInfo);
		}
		cooltimeInfo.maxCooltime = maxCooltime;
		cooltimeInfo.ApplyCooltime();
		return cooltimeInfo;
	}

	public bool CheckCooltime(string cooltimeId)
	{
		Cooltime cooltime = GetCooltime(cooltimeId);
		if (cooltime == null)
			return false;
		return cooltime.CheckCooltime();
	}

	public Cooltime GetCooltime(string cooltimeId)
	{
		if (_dicCooltimeInfo.ContainsKey(cooltimeId))
			return _dicCooltimeInfo[cooltimeId];
		return null;
	}

	void Update()
	{
		Dictionary<string, Cooltime>.Enumerator e = _dicCooltimeInfo.GetEnumerator();
		while(e.MoveNext())
			e.Current.Value.UpdateCooltime(Time.deltaTime);
	}
}
