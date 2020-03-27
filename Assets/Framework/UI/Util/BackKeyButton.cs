using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackKeyButton : MonoBehaviour {

    Button _button;

    void OnEnable()
    {
        BackKeyButton.Push(this);
    }

    void OnDisable()
    {
        BackKeyButton.Pop(this);
    }

	// Use this for initialization
	void Start () {
        _button = GetComponent<Button>();
	}
	
	// Update is called once per frame
	void Update () {
        if (_button == null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
			if (DragThresholdController.instance.IsEnableEventSystem() == false)
				return;

            for (int i = s_backKeyButtonList.Count - 1; i >= 0; --i)
            {
                if (s_backKeyButtonList[i] == null)
                {
                    s_backKeyButtonList.RemoveAt(i);
                    continue;
                }
                if (s_backKeyButtonList[i] == this)
				{
					if (_button.interactable)
                    	_button.onClick.Invoke();
				}
                break;
            }
        }
	}


#region Manager
    static List<BackKeyButton> s_backKeyButtonList;
    public static void Push(BackKeyButton backKeyButton)
    {
        if (s_backKeyButtonList == null)
            s_backKeyButtonList = new List<BackKeyButton>();

        s_backKeyButtonList.Add(backKeyButton);
    }

    public static void Pop(BackKeyButton backKeyButton)
    {
        if (s_backKeyButtonList == null)
            return;

        s_backKeyButtonList.Remove(backKeyButton);
    }
#endregion
}
