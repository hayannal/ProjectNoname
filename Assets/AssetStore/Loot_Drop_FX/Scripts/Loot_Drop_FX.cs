using UnityEngine;
using System.Collections;

public class Loot_Drop_FX : MonoBehaviour
{
    public GameObject lootStartFX;
    public GameObject lootBaseFX;
    public GameObject lootObjectFX;
    public GameObject lootEndFX;
    public GameObject loot_Object;

    public ParticleSystem ground_Glow;
    public GameObject ground_Glow_Group;
    public ParticleSystem ground_Beam;
    public ParticleSystem ground_Rings;
    public GameObject loot_Glow_Group;
    public ParticleSystem loot_Rings;
    public GameObject loot_Rings_Group;
    public ParticleSystem loot_Sparks;
    public ParticleSystem loot_Electricity;

    public AudioSource loot_Loop_Audio;


    public Light loot_Light;
    private float fadeStart = 2.0f;
    private float fadeEnd = 0;
    private float fadeTime = 1;
    private float t = 0.0f;

    private bool lootActive = false;

    void Start()
    {


        lootStartFX.SetActive(false);
        lootBaseFX.SetActive(false);
        lootObjectFX.SetActive(false);
        lootEndFX.SetActive(false);

    }


void Update()
{

    if (Input.GetButtonDown("Fire1"))
    {


            if (lootActive == false)
            {

                StartCoroutine("LootDrop");

            }

    }


    if (Input.GetButtonDown("Fire2"))
    {

        StartCoroutine("LootStop");

    }

}

    IEnumerator LootDrop()
    {

        ground_Glow_Group.SetActive(true);
        loot_Glow_Group.SetActive(true);
        loot_Rings_Group.SetActive(true);
        loot_Object.SetActive(true);

        lootActive = true;

        yield return new WaitForSeconds(0.1f);

        lootStartFX.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        lootBaseFX.SetActive(true);

        loot_Loop_Audio.Play();

        yield return new WaitForSeconds(1.0f);

        lootObjectFX.SetActive(true);


    }


    IEnumerator LootStop()
    {

        lootEndFX.SetActive(true);

        ground_Glow_Group.SetActive(false);
        loot_Glow_Group.SetActive(false);
        loot_Rings_Group.SetActive(false);
        loot_Object.SetActive(false);

        loot_Loop_Audio.Stop();

        StartCoroutine("FadeLight");

        ground_Rings.Stop();
        ground_Beam.Stop();
        loot_Rings.Stop();
        loot_Sparks.Stop();
        loot_Electricity.Stop();
    
 
        yield return new WaitForSeconds(2.5f);


        lootStartFX.SetActive(false);
        lootBaseFX.SetActive(false);
        lootObjectFX.SetActive(false);
        lootEndFX.SetActive(false);

        lootActive = false;

    }

    IEnumerator FadeLight()
    {

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            loot_Light.intensity = Mathf.Lerp(fadeStart, fadeEnd, t / fadeTime);
            yield return 0;
        }

        t = 0;

    }


}
