using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDDPS : MonoBehaviour
{
#if UNITY_EDITOR
	public static HUDDPS instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("HUDDPS")).AddComponent<HUDDPS>();
			return _instance;
		}
	}
	static HUDDPS _instance = null;

	public static bool isActive { get { return (_instance != null); } }

	[System.Serializable]
	public class DpsData
	{
		public int chapter;
		public int stage;
		public bool boss;
		public float clearTime;
		public float dps;
		public float overDps;
	}
	public List<DpsData> listDpsData = new List<DpsData>();

	int _playChapter;
	int _playStage;
	bool _bossStage;
	bool _playing;
	float _startTime;
	double _sumDamage = 0.0;
	double _sumOverDamage = 0.0;
	public void OnStartStage(int chapter, int stage, bool boss)
	{
		if (_playing)
			return;

		_playChapter = chapter;
		_playStage = stage;
		_bossStage = boss;
		_playing = true;
		_startTime = Time.time;
		_sumDamage = _sumOverDamage = 0.0;
	}

	public void OnClearStage()
	{
		if (_playing == false)
			return;

		DpsData dpsData = new DpsData();
		dpsData.chapter = _playChapter;
		dpsData.stage = _playStage;
		dpsData.boss = _bossStage;
		dpsData.clearTime = Time.time - _startTime;
		dpsData.dps = (float)(_sumDamage / dpsData.clearTime);
		dpsData.overDps = (float)(_sumOverDamage / dpsData.clearTime);
		listDpsData.Add(dpsData);
		_playing = false;

		_avgDps = GetAvgDps(false);
		_avgClearTime = GetAvgClearTime(false);
		_avgDpsBoss = GetAvgDps(true);
		_avgClearTimeBoss = GetAvgClearTime(true);
	}
	
	public void AddDamage(float damage, float overDamage)
	{
		if (_playing == false)
			return;

		_sumDamage += damage;
		_sumOverDamage += overDamage;
	}

	public void CopyData()
	{
		if (listDpsData.Count == 0)
			return;

		string data = "";
		for (int i = 0; i < listDpsData.Count; ++i)
		{
			string line = string.Format("{0}\t{1}\t{2}\t{3}\t{4}", listDpsData[i].chapter, listDpsData[i].stage, listDpsData[i].clearTime, listDpsData[i].dps, listDpsData[i].overDps);
			data += line;

			if (i < (listDpsData.Count - 1))
				data += "\n";
		}

		TextEditor te = new TextEditor();
		te.text = data;
		te.SelectAll();
		te.Copy();
	}



	float _avgDps;
	float _avgClearTime;
	float _avgDpsBoss;
	float _avgClearTimeBoss;
	float GetAvgDps(bool boss)
	{
		if (listDpsData.Count == 0)
			return 0.0f;

		double sumDps = 0.0;
		for (int i = 0; i < listDpsData.Count; ++i)
		{
			if (listDpsData[i].boss != boss)
				continue;
			sumDps += listDpsData[i].dps;
		}
		return (float)(sumDps / listDpsData.Count);
	}

	float GetAvgClearTime(bool boss)
	{
		if (listDpsData.Count == 0)
			return 0.0f;

		double sumClearTime = 0.0;
		for (int i = 0; i < listDpsData.Count; ++i)
		{
			if (listDpsData[i].boss != boss)
				continue;
			sumClearTime += listDpsData[i].clearTime;
		}
		return (float)(sumClearTime / listDpsData.Count);
	}


	public Rect startRect = new Rect(10, 160, 200, 200); // The rect the window is initially displayed at.
	public bool allowDrag = true; // Do you want to allow the dragging of the FPS window
	private GUIStyle style; // The style the text will be displayed at, based en defaultSkin.label.

	void OnGUI()
	{
		// Copy the default label skin, change the color and the alignement
		if (style == null)
		{
			style = new GUIStyle(GUI.skin.label);
			style.normal.textColor = Color.white;
			style.alignment = TextAnchor.MiddleCenter;
			style.fontSize = 24;
		}

		GUI.color = Color.white;
		startRect = GUI.Window(1, startRect, DoMyWindow, "");
	}

	void DoMyWindow(int windowID)
	{
		GUI.Label(new Rect(0, 0, startRect.width, startRect.height), _avgDps + " DPS\n" + _avgClearTime + " Time\n" + "------------\n" + _avgDpsBoss + " DPS\n" + _avgClearTimeBoss + " Time", style);
		if (allowDrag) GUI.DragWindow(new Rect(0, 0, Screen.width, Screen.height));
	}
#endif
}
