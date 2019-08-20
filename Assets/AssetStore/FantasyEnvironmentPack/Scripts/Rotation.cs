using UnityEngine;
using System.Collections;

public class Rotation : MonoBehaviour {

    public float speed = 0;
    public bool y = false;

	Transform _transform;

	// Use this for initialization
	void Start () {
		_transform = transform;
	}
	
	// Update is called once per frame
	void Update () {

        if (!y)
        _transform.Rotate(0,0,speed*Time.deltaTime);
        if (y)
            _transform.Rotate(0, speed * Time.deltaTime, 0);

    }
}
