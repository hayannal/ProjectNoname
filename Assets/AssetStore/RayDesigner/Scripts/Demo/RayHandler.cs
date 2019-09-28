using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class RayHandler : MonoBehaviour {

    public List<RayDesigner> Rays = new List<RayDesigner>();
    public int UseRay = 0;
    public Transform Hand;
    public Vector3 StandardTargetPoint;
    private Vector3 MousePosDelayed = Vector3.zero;
    public Animator animator;
    private Vector3 NormalPos;

    public void LoadScene(int i)
    {
        SceneManager.LoadScene(i);
    }

    void Start()
    {
        foreach (RayDesigner r in Rays)
        {
            r.Hide();
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            animator.Play("Magic_Ray_Activate");
            MousePosDelayed = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(MousePosDelayed);
            RaycastHit rayHit = new RaycastHit();

            if (Physics.Raycast(ray, out rayHit, 100f))
            {
                Vector3 InpactPoint = rayHit.point;
                Vector3 Normal = rayHit.normal;
                
                Rays[UseRay].UpdateTargetPosition(InpactPoint, InpactPoint+Normal);
                NormalPos = Normal;
            }
        }

        if (Input.GetMouseButton(0))
        {
            MousePosDelayed = Vector3.Lerp(MousePosDelayed, Input.mousePosition, 5f * Time.deltaTime);
            Ray ray = Camera.main.ScreenPointToRay(MousePosDelayed);
            RaycastHit rayHit = new RaycastHit();

            if (Physics.Raycast(ray, out rayHit, 100f))
            {
                Vector3 InpactPoint = rayHit.point;
                Vector3 Normal = rayHit.normal * 7f;
                NormalPos = Vector3.Lerp(NormalPos, Normal, 5f * Time.deltaTime);
                Rays[UseRay].UpdateTargetPosition(InpactPoint, InpactPoint + NormalPos);
                Hand.rotation = Quaternion.Slerp(Hand.rotation, Quaternion.LookRotation(rayHit.point - Hand.transform.position), 5f * Time.deltaTime);
            }
            else {
                animator.Play("Magic_Ray_End");
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            animator.Play("Magic_Ray_End");
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            UseRay = 0;
            Rays[0].Show();
            Rays[1].Hide();
            Rays[2].Hide();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            UseRay = 1;
            Rays[0].Hide();
            Rays[1].Show();
            Rays[2].Hide();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            UseRay = 2;
            Rays[0].Hide();
            Rays[1].Hide();
            Rays[2].Show();
        }

        if (!Input.GetMouseButton(0))
        {
            Rays[0].Hide();
            Rays[1].Hide();
            Rays[2].Hide();
        }
    }

    public void Activate()
    {
        Rays[UseRay].Show();
    }

    public void Deactivate()
    {
        Rays[UseRay].Hide();
    }
}
