using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
	public Actor monsterActorPrefab;



    // Start is called before the first frame update
    void Start()
    {
		monsterActorPrefab.gameObject.SetActive(false);

		GameObject newObject = Instantiate(monsterActorPrefab.gameObject, new Vector3(0.0f, 0.0f, 4.0f), Quaternion.Euler(0.0f, 180.0f, 0.0f));
		newObject.SetActive(true);
		Team team = newObject.GetComponent<Team>();
		if (team != null)
			team.teamID = 2;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
