using guiraffe.SubstanceOrb;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class OrbFill : OrbBehaviour
{
    public float AnimationSpeed = 5.0f;
    [Range(0.0f, 1.0f)] public float Fill = 1.0f;

    float startFill;

    void Awake()
    {
        startFill = Material.GetFloat(OrbVariable.FILL);
    }

    void OnDestroy()
    {
        Material.SetFloat(OrbVariable.FILL, startFill);
    }

    void Update()
    {
        float rate = Time.deltaTime * AnimationSpeed;
        Material.SetFloat(OrbVariable.FILL, Mathf.Lerp(Material.GetFloat(OrbVariable.FILL), Fill, rate));
    }
}