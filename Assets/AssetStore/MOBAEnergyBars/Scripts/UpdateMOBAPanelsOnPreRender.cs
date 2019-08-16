using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UpdateMOBAPanelsOnPreRender : MonoBehaviour {
    public delegate void CallOnPreRender();
    public CallOnPreRender CallList;

    void OnPreRender()
    {
        if (CallList != null) CallList();
    }
}
