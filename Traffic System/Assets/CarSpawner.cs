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
        carSpawned.Add(Instantiate(carPrefab, spawn[0].transform.position, spawn[0].transform.rotation));

        
        carSpawned[0].GetComponent<CarMovement>().setTarget(spawn[0].transform.position);
    }
}
