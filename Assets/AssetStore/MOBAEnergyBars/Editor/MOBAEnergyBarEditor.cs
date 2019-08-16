using UnityEngine;
using UnityEditor;
using System.Collections;

public class MOBAEnergyBarEditor : MonoBehaviour {
    [MenuItem("GameObject/UI/MOBA Energy Bar", false, 10)]
    static void AddAHB(MenuCommand menuCommand)
    {
        GameObject parent = (menuCommand.context as GameObject);
        GameObject ahb = new GameObject("MOBA Energy Bar");
        ahb.AddComponent<MOBAEnergyBar>();
        ahb.transform.SetParent(parent.transform, false);
        RectTransform prt = parent.GetComponent<RectTransform>();
        RectTransform rt = ahb.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(Mathf.Min(100f, prt.rect.width-5f), Mathf.Min(100f, prt.rect.height-5f));
        Selection.activeGameObject = ahb;
    }
}
