using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("PaintGenerator")]
	public TextAsset paintDataText;
	public bool useWorldSpaceDirection;
	public float centerAngleY = 0.0f;
	public float betweenAngle = 4.0f;
	public float lineInterval = 0.1f;

	private List<List<int>> _paintData;
	float _startAngleY;
	int _lineIndex;
	float _remainLineIntervalTime;

	void Start()
	{
		_paintData = LoadPaintData();
		if (_paintData == null || _paintData.Count <= 0)
			gameObject.SetActive(false);
		_startAngleY = centerAngleY - (_paintData[0].Count % 2 == 0 ? (betweenAngle * _paintData[0].Count / 2f) + (betweenAngle / 2f) : betweenAngle * Mathf.Floor(_paintData[0].Count / 2f));
	}

	void OnEnable()
	{
		_lineIndex = 0;
		_remainLineIntervalTime = 0.0f;

		if (paintDataText == null)
			gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
		if (CheckChangeState())
		{
			gameObject.SetActive(false);
			return;
		}

		_remainLineIntervalTime -= Time.deltaTime;
		if (_remainLineIntervalTime < 0.0f)
		{
			_remainLineIntervalTime += lineInterval;

			List<int> lineData = _paintData[_lineIndex];
			for (int i = 0; i < lineData.Count; ++i)
			{
				if (lineData[i] != 1)
					continue;

				if (useWorldSpaceDirection)
					Generate(cachedTransform.position, Quaternion.Euler(0.0f, _startAngleY + i * betweenAngle, 0.0f));
				else
					Generate(cachedTransform.position, cachedTransform.rotation * Quaternion.Euler(0.0f, _startAngleY + i * betweenAngle, 0.0f));
			}

			++_lineIndex;
		}

		if (_lineIndex >= _paintData.Count)
			gameObject.SetActive(false);
	}


	private static readonly string[] SPLIT_VAL = { "\n", "\r", "\r\n" };
	private List<List<int>> LoadPaintData()
	{
		if (paintDataText == null || string.IsNullOrEmpty(paintDataText.text))
		{
			Debug.LogWarning("Cannot load paint data because PaintDataText file is null or empty.");
			return null;
		}

		string[] lines = paintDataText.text.Split(SPLIT_VAL, System.StringSplitOptions.RemoveEmptyEntries);

		var paintData = new List<List<int>>(lines.Length);

		for (int i = 0; i < lines.Length; i++)
		{
			// lines beginning with "#" are ignored as comments.
			if (lines[i].StartsWith("#"))
			{
				continue;
			}
			// add line
			paintData.Add(new List<int>(lines[i].Length));

			for (int j = 0; j < lines[i].Length; j++)
			{
				// bullet is fired into position of "*".
				paintData[paintData.Count - 1].Add(lines[i][j] == '*' ? 1 : 0);
			}
		}

		// reverse because fire from bottom left.
		paintData.Reverse();

		return paintData;
	}
}