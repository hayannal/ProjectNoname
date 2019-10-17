using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterDemo : MonoBehaviour {


    public Animator anim;
    public Button btnNext, btnPrev;
    public Text animText;
    private List<string> lstAnimNames = new List<string>();
    int currentIndex = 0;
	// Use this for initialization
	void Start () {
		
        foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
        {
            lstAnimNames.Add(clip.name);
        }
        currentIndex = 0;
		animText.text = lstAnimNames[0];
        btnNext.onClick.AddListener(OnBtnNextClicked);
        btnPrev.onClick.AddListener(OnBtnPrevClicked);
	}
	
    void OnBtnNextClicked()
    {
        currentIndex = (currentIndex < lstAnimNames.Count - 1) ? currentIndex + 1 : 0;
        anim.CrossFade(lstAnimNames[currentIndex], 0.2f);
        animText.text = lstAnimNames[currentIndex];
    }

    void OnBtnPrevClicked()
    {
        currentIndex = (currentIndex > 0) ? currentIndex - 1 : lstAnimNames.Count - 1;
        anim.CrossFade(lstAnimNames[currentIndex], 0.2f);
        animText.text = lstAnimNames[currentIndex];
    }
}
