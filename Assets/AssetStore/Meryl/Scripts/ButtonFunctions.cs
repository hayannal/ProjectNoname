using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonFunctions : MonoBehaviour
{

    public Animator anim;
    public Animator anim2;

    // Use this for initialization
    void Start()
    {
        //print(anim);
    }

    // Update is called once per frame
    public void RunAnimation(string animStr)
    {
        if (animStr == "Tpose")
        {
            anim.Play("Tpose", -1, 0.0f);
            anim.Play("Tpose Facial", 1, 0.0f);
            anim2.Play("Tpose", -1, 0.0f);
            anim2.Play("Tpose Facial", -1, 0.0f);
        }

        if (animStr == "Walk")
        {
            anim.Play("Walk", -1, 0.0f);
            anim.Play("Walk Facial", 1, 0.0f);
            anim2.Play("Walk", -1, 0.0f);
            anim2.Play("Walk Facial", 1, 0.0f);
        }

        if (animStr == "Run")
        {
            anim.Play("Run", -1, 0.0f);
            anim.Play("Run Facial", 1, 0.0f);
            anim2.Play("Run", -1, 0.0f);
            anim2.Play("Run Facial", 1, 0.0f);
        }

        if (animStr == "Jump")
        {
            anim.Play("Jump", -1, 0.0f);
            anim.Play("Jump Facial", 1, 0.0f);
            anim2.Play("Jump", -1, 0.0f);
            anim2.Play("Jump Facial", 1, 0.0f);
        }

        if (animStr == "Die")
        {
            anim.Play("Die", -1, 0.0f);
            anim.Play("Die Facial", 1, 0.0f);
            anim2.Play("Die", -1, 0.0f);
            anim2.Play("Die Facial", 1, 0.0f);
        }

        if (animStr == "Idle")
        {
            anim.Play("Idle", -1, 0.0f);
            anim.Play("Idle Facial", 1, 0.0f);
            anim2.Play("Idle", -1, 0.0f);
            anim2.Play("Idle Facial", 1, 0.0f);
        }

        if (animStr == "Idle2")
        {
            anim.Play("Idle2", -1, 0.0f);
            anim.Play("Idle2 Facial", 1, 0.0f);
            anim2.Play("Idle2", -1, 0.0f);
            anim2.Play("Idle2 Facial", 1, 0.0f);
        }

        if (animStr == "Cast")
        {
            anim.Play("Cast", -1, 0.0f);
            anim.Play("Cast Facial", 1, 0.0f);
            anim2.Play("Cast", -1, 0.0f);
            anim2.Play("Cast Facial", 1, 0.0f);
        }
    }
}
