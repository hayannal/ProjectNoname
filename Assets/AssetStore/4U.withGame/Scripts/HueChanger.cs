using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum Euse
{
    ForDynamicBatching=0,
    ForGPUInstancing=1,
    ForSRPBatchingUnity20191Over=2,
    SkinnedMeshRenderer=3,

}
public class HueChanger : MonoBehaviour
{
    [SerializeField] [Range(0f,1f)] float hueMaxVolume=1;
    [SerializeField, TooltipAttribute("Material property block can not be used with SRP")] Euse setting =0;
    

    void OnEnable()
    {
        
        float hueRan = Random.Range(0f, hueMaxVolume);
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        int propId;

        switch (setting)
        {
            case Euse.ForGPUInstancing:
                propId = Shader.PropertyToID("_HueCI");
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                props.SetFloat(propId, hueRan);
                foreach (var renderer in renderers)
                {
                    renderer.SetPropertyBlock(props);
                }
                break;
            case Euse.ForSRPBatchingUnity20191Over:
                propId = Shader.PropertyToID("_HueCB");
                foreach (var renderer in renderers)
                {
                    renderer.material.SetFloat(propId, hueRan);
                }
                break;
            case Euse.SkinnedMeshRenderer:
                propId = Shader.PropertyToID("_HueC");
                SkinnedMeshRenderer[] sRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var sRenderer in sRenderers)
                {
                    Material[] mats= sRenderer.materials;
                    foreach(var mat in mats)
                    {
                        hueRan= Random.Range(0f, hueMaxVolume);
                        mat.SetFloat(propId, hueRan);
                    }
                    //sRenderer.material.SetFloat("_HueC", hueRan);
                }
                break;
            default:
                break;
        }
        /*
        if (srpBatching==true) //for Unity19.1
        {
            propId = Shader.PropertyToID("_HueCB");
            foreach (var renderer in renderers)
            {
                renderer.material.SetFloat(propId, hueRan);
            }
        }
        else //GPU Instancing
        {
            propId = Shader.PropertyToID("_HueCI");
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            props.SetFloat(propId, hueRan);
            foreach (var renderer in renderers)
            {
                renderer.SetPropertyBlock(props);
            }
        }
        */
    }
}
