using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RandomGenerate: MonoBehaviour
{


    //プレハブを変数に代入
    [SerializeField] GameObject[] prefabs=null;
    [SerializeField] int appearance=50;
    [SerializeField] Vector3 area ;
    [SerializeField] bool randamRotation= false;

    void Start()
    {
        int rr;
        
        float x ;
        float y ;
        float z ;
        Vector3 position;
        for (int i = 0; i < appearance; i++)
        {
            if (randamRotation == true)
            {
                rr = Random.Range(0, 180);
            }
            else
            {
                rr = 0;
            }
            foreach (var prefab in prefabs)
            {
                //オブジェクトの座標
                x = Random.Range(-area.x, area.x);
                y = Random.Range(0,area.y);
                z = Random.Range(0, area.z);
                position = new Vector3(x, y, z);
                //オブジェクトを生産
                Instantiate(prefab, position, Quaternion.Euler(0, rr, 0));
            }
            
        }
    }
}