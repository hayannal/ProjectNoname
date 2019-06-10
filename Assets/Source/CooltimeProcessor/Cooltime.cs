using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public class Cooltime
{
	public float maxCooltime;
	public float cooltime;
	public Action cooltimeStartAction;
	public Action cooltimeEndAction;

	public float cooltimeRatio
	{
		get
		{
			if (maxCooltime == 0.0f)
				return 0.0f;
			return cooltime / maxCooltime;
		}
	}

	StringBuilder _sb = new StringBuilder();
	public string cooltimeRatioText
	{
		get
		{
			_sb.Remove(0, _sb.Length);
			if (cooltime > 1.0f) _sb.AppendFormat("{0}", ((int)(cooltime + 1.0f)));
			else _sb = _sb.AppendFormat("{0:0.0}", cooltime);
			return _sb.ToString();
		}
	}

	public void ApplyCooltime()
	{
		cooltime = maxCooltime;
		if (cooltimeStartAction != null)
			cooltimeStartAction();
	}

	public bool CheckCooltime()
	{
		return (cooltime > 0.0f);
	}

	public void UpdateCooltime(float deltaTime)
	{
		if (CheckCooltime())
		{
			cooltime -= deltaTime;
			if (cooltime < 0.0f)
			{
				cooltime = 0.0f;
				if (cooltimeEndAction != null)
					cooltimeEndAction();
			}
		}
	}
}
