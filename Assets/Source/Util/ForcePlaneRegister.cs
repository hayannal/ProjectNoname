using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForcePlaneRegister : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
		BattleInstanceManager.instance.planeCollider = GetComponent<Collider>();
    }
}
