using UnityEngine;
using System.Collections;

public class Billboard : MonoBehaviour
{
  public Camera Camera;
  public bool Active = true;
  public bool AutoInitCamera = true;
  public bool customBillboard = false;

  private GameObject myContainer;
  private Transform t, camT, contT;
  private Vector3 defaultEuler;

  private void Awake()
  {
	if (customBillboard)
	{
		defaultEuler = transform.localEulerAngles;
		Active = false;
		return;
	}

    if (AutoInitCamera) {
      Camera = Camera.main;
      Active = true;
    }

    t = transform;
    camT = Camera.transform;
    var parent = t.parent;
    myContainer = new GameObject { name = "Billboard_" + t.gameObject.name };
    contT = myContainer.transform;
    contT.position = t.position;
    t.parent = myContainer.transform;
    contT.parent = parent;
  }

	private void OnEnable()
	{
		if (customBillboard)
			transform.rotation = Quaternion.Euler(defaultEuler);
	}

	private void Update()
  {
    if (Active)
      contT.LookAt(contT.position + camT.rotation * Vector3.back, camT.rotation * Vector3.up);
  }
}