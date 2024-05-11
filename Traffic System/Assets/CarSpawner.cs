using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField]
    int maxNumberCars = 2;

    [SerializeField]
    GameObject carPrefab;

    List<GameObject> spawn = new List<GameObject>();

    List<GameObject> carSpawned = new List<GameObject>();


    public void addSpawnPoint(GameObject point)
    {
        spawn.Add(point);
    }

    private void Start()
    {
        for(int i = 0; i < spawn.Count; i++)
        {
            carSpawned.Add(Instantiate(carPrefab, spawn[i].transform.position, spawn[i].transform.rotation));


            carSpawned[i].GetComponent<CarMovement>().setTarget(spawn[i].transform.position);
        }
    }
}
