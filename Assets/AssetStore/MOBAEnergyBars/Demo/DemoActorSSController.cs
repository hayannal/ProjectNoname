using UnityEngine;
using System.Collections;

public class DemoActorSSController : MonoBehaviour {

    float timer;
    MOBAEnergyBar health;
    string healthText;
    string maxHealthText;
    Vector3 startPos;

    void Start ()
    {
        health = GetComponentInChildren<ScreenSpacePanel>().PanelUIElement.GetComponent<MOBAHealthBarPanel>().HealthBar;
        healthText = health.Value.ToString();
        maxHealthText = health.MaxValue.ToString();
        startPos = transform.position;
    }

	void Update ()
    {
        transform.position = new Vector3(Mathf.Sin(timer/2f), 0.0f, Mathf.Cos(timer/2f))*2.5f + startPos;
        timer += Time.deltaTime;
	}

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 90, 40), "Screen space health panel");

        if (GUI.Button(new Rect(10, 60, 90, 40), "Damage 50"))
            health.Value -= 50f;

        healthText = GUI.TextField(new Rect(10, 130, 50, 30), healthText);
        if (GUI.Button(new Rect(70, 130, 120, 30), "Set health"))
            health.SetValueNoBurn(float.Parse(healthText));

        maxHealthText = GUI.TextField(new Rect(10, 180, 50, 30), maxHealthText);
        if (GUI.Button(new Rect(70, 180, 120, 30), "Set max health"))
        {
            health.MaxValue = float.Parse(maxHealthText);
            health.SetValueNoBurn(Mathf.Min(health.MaxValue, health.Value));
        }
    }
}
