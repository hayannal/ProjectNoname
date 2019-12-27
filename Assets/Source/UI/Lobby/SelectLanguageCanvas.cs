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
		string languageList = BattleInstanceManager.instance.GetCachedGlobalConstantString("LanguageList");
		string[] split = languageList.Split(',');
		for (int i = 0; i < split.Length; ++i)
		{
			GameObject newObject = Instantiate<GameObject>(buttonObject, gridRootTransform);
			SelectLanguageButton selectLanguageButton = newObject.GetComponent<SelectLanguageButton>();
			selectLanguageButton.region = split[i];
		}
		buttonObject.SetActive(false);
	}
}
