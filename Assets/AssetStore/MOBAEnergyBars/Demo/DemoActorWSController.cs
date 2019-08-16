using UnityEngine;
using System.Collections;

public class DemoActorWSController : MonoBehaviour {
    float timer;
    MOBAEnergyBar health;
    string healthText;
    string maxHealthText;
    Vector3 startPos;

    void Start()
    {
        health = GetComponentInChildren<MOBAHealthBarPanel>().HealthBar;
        healthText = health.Value.ToString();
        maxHealthText = health.MaxValue.ToString();
        startPos = transform.position;
    }

    void Update()
    {
        transform.position = new Vector3(Mathf.Sin(timer / 2f), 0.0f, Mathf.Cos(timer / 2f)) * 2.5f + startPos;
        timer += Time.deltaTime;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(Screen.width-100, 10, 90, 40), "World space health panel");

        if (GUI.Button(new Rect(Screen.width - 100, 60, 90, 40), "Damage 50"))
            health.Value -= 50f;

        healthText = GUI.TextField(new Rect(Screen.width - 180, 130, 50, 30), healthText);
        if (GUI.Button(new Rect(Screen.width - 120, 130, 120, 30), "Set health"))
            health.SetValueNoBurn(float.Parse(healthText));

        maxHealthText = GUI.TextField(new Rect(Screen.width - 180, 180, 50, 30), maxHealthText);
        if (GUI.Button(new Rect(Screen.width - 120, 180, 120, 30), "Set max health"))
        {
            health.MaxValue = float.Parse(maxHealthText);
            health.SetValueNoBurn(Mathf.Min(health.MaxValue, health.Value));
        }
    }
}
