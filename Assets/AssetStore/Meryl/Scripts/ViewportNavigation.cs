
/*

	This script is a modification of the Maya style navigation script by Daniel Skovli.
	
	Changes:
		- Exclude Alt from hotkey
		- Panning is restricted to just up and down
		- Removes focus and selection functions
	
	The original script:
	http://danielskovli.com/folio/media/code/js-maya-style-navigation/maya-style-navigation.js.php
	
	Controls:
		LMB: Rotate view
		RMB: Zoom view
		MMB: Pan up and down
		
	miica - 2015

	It is converted to C# at March 2018
*/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewportNavigation : MonoBehaviour {

    public float zoomSpeed = 1.2f;
    public float moveSpeed = 0.1f;
    public float rotateSpeed = 10.0f;

    private GameObject orbitVector;
    
    // Use this for initialization
    void Start () {
        // Create a capsule (which will be the lookAt target and global orbit vector)
        orbitVector = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        orbitVector.transform.position = new Vector3(0.0f, 0.5f, 0.0f);

        // Snap the camera to align with the grid in set starting position (otherwise everything gets a bit wonky)
        transform.position = new Vector3(0, 1.5f, 5);

        // Point the camera towards the capsule
        transform.LookAt(orbitVector.transform.position, Vector3.up);

        // Hide the capsule (disable the mesh renderer)
        orbitVector.GetComponent<Renderer>().enabled = false;
	}

    // Call all of our functionality in LateUpdate() to avoid weird behaviour (as seen in Update())
    void LateUpdate ()
    {
        // Get mouse vectors
        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");

        // Distance between camera and orbitVector. We'll need this in a few places
        float distanceToOrbit = Vector3.Distance(transform.position, orbitVector.transform.position);
    
        // RMB - ZOOM
        if(Input.GetMouseButton(1))
        {
            // Refine the rotateSpeed based on distance to orbitVector
            float currentZoomSpeed = Mathf.Clamp(zoomSpeed * (distanceToOrbit / 50), 0.1f, 2.0f);

            // Move the camera in/out
            transform.Translate(Vector3.forward * (x * currentZoomSpeed));
        }

        // LMB - PIVOT
        else if (Input.GetMouseButton(0))
        {
            // Refine the rotateSpeed based on distance to orbitVector
            float currentRotateSpeed = Mathf.Clamp(rotateSpeed * (distanceToOrbit / 50), 1.0f, rotateSpeed);

            // Temporarily parent the camera to orbitVector and rotate orbitVector as desired
            transform.parent = orbitVector.transform;
            orbitVector.transform.Rotate(Vector3.right * (y * currentRotateSpeed));
            orbitVector.transform.Rotate(Vector3.up * (x * currentRotateSpeed), Space.World);
            transform.parent = null;
        }

        // MMB - PAN
        else if(Input.GetMouseButton(2))
        {
            // Calcuate move speed
            float translateY = (y * moveSpeed) * -1;
            transform.Translate(new Vector3(0.0f, translateY, 0.0f));
        }

    }
}
