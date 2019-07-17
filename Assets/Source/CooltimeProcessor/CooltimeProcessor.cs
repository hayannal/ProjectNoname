using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CooltimeProcessor : MonoBehaviour {

	Dictionary<string, Cooltime> _dicCoolTimeInfo = new Dictionary<string, Cooltime>();

	void OnEnable()
	{
		Dictionary<string, Cooltime>.Enumerator e = _dicCoolTimeInfo.GetEnumerator();
		while (e.MoveNext())
			e.Current.Value.cooltime = 0.0f;
	}

	public Cooltime InitializeCoolTime(string cooltimeId, float maxCoolTime, float initCoolTime = 0.0f)
	{
		Cooltime coolTimeInfo = null;
		if (_dicCoolTimeInfo.ContainsKey(cooltimeId))
			coolTimeInfo = _dicCoolTimeInfo[cooltimeId];
		else
		{
			coolTimeInfo = new Cooltime();
			_dicCoolTimeInfo.Add(cooltimeId, coolTimeInfo);
		}
		coolTimeInfo.maxCooltime = maxCoolTime;
		coolTimeInfo.cooltime = initCoolTime;
		return coolTimeInfo;
	}

	public Cooltime GetCoolTime(string cooltimeId)
	{
		if (_dicCoolTimeInfo.ContainsKey(cooltimeId))
			return _dicCoolTimeInfo[cooltimeId];
		return null;
	}

	void Update()
	{
		Dictionary<string, Cooltime>.Enumerator e = _dicCoolTimeInfo.GetEnumerator();
		while(e.MoveNext())
			e.Current.Value.UpdateCooltime(Time.deltaTime);
	}
}
