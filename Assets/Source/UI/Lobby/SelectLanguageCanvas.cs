using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectLanguageCanvas : MonoBehaviour
{
	public GameObject buttonObject;
	public Transform gridRootTransform;

    // Start is called before the first frame update
    void Start()
    {
		List<LanguageTableData> listLanguageTableData = new List<LanguageTableData>();
		for (int i = 0; i < UIString.instance.languageTable.dataArray.Length; ++i)
			listLanguageTableData.Add(UIString.instance.languageTable.dataArray[i]);

		listLanguageTableData.Sort(delegate (LanguageTableData x, LanguageTableData y)
		{
			if (x.order < y.order) return -1;
			else if (x.order > y.order) return 1;
			return 0;
		});

		for (int i = 0; i < listLanguageTableData.Count; ++i)
		{
			GameObject newObject = Instantiate<GameObject>(buttonObject, gridRootTransform);
			SelectLanguageButton selectLanguageButton = newObject.GetComponent<SelectLanguageButton>();
			selectLanguageButton.languageId = listLanguageTableData[i].id;
			selectLanguageButton.languageName = listLanguageTableData[i].languageName;
		}
		buttonObject.SetActive(false);
	}
}
