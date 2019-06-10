using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CooltimeProcessor : MonoBehaviour {

	Dictionary<string, Cooltime> m_dicCoolTimeInfo = new Dictionary<string, Cooltime>();

	public Cooltime InitializeCoolTime(string cooltimeID, float maxCoolTime, float initCoolTime = 0.0f)
	{
		Cooltime coolTimeInfo = null;
		if (m_dicCoolTimeInfo.ContainsKey(cooltimeID))
			coolTimeInfo = m_dicCoolTimeInfo[cooltimeID];
		else
		{
			coolTimeInfo = new Cooltime();
			m_dicCoolTimeInfo.Add(cooltimeID, coolTimeInfo);
		}
		coolTimeInfo.maxCooltime = maxCoolTime;
		coolTimeInfo.cooltime = initCoolTime;
		return coolTimeInfo;
	}

	public Cooltime GetCoolTime(string cooltimeID)
	{
		if (m_dicCoolTimeInfo.ContainsKey(cooltimeID))
			return m_dicCoolTimeInfo[cooltimeID];
		return null;
	}

	void Update()
	{
		Dictionary<string, Cooltime>.Enumerator e = m_dicCoolTimeInfo.GetEnumerator();
		while(e.MoveNext())
			e.Current.Value.UpdateCooltime(Time.deltaTime);
	}
}
