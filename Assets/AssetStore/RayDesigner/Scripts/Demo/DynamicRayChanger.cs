using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DynamicRayChanger : MonoBehaviour
{
    private GameObject dragObj;
    public GameObject[] Rays;

    void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit rayHit = new RaycastHit();

            if (Physics.Raycast(ray, out rayHit, 150f))
            {
                dragObj = rayHit.transform.gameObject;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragObj = null;
        }

        if (dragObj != null)
        {
            Vector3 pos = Input.mousePosition;
            pos.z = 22f;
            dragObj.transform.position = Camera.main.ScreenToWorldPoint(pos);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Rays[0].SetActive(true);
            Rays[1].SetActive(false);
            Rays[2].SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Rays[0].SetActive(false);
            Rays[1].SetActive(true);
            Rays[2].SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Rays[0].SetActive(false);
            Rays[1].SetActive(false);
            Rays[2].SetActive(true);
        }

    }

    public void LoadScene(int i)
    {
        SceneManager.LoadScene(i);
    }
}
