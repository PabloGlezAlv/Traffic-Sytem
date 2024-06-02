using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField]
    int maxNumberCars = 2;

    [SerializeField]
    GameObject carPrefab;

    List<GameObject> spawn = new List<GameObject>();

    List<GameObject> carSpawned = new List<GameObject>();

    float timer = 0;

    public void addSpawnPoint(GameObject point)
    {
        spawn.Add(point);
    }

    private void Start()
    {
        for(int i = 0; i < spawn.Count; i++)
        {
            carSpawned.Add(Instantiate(carPrefab, spawn[i].transform.position, spawn[i].transform.rotation));


            carSpawned[i].GetComponent<IMovable>().setTarget(spawn[i].transform.position);


            carSpawned[i].name = "Car " + carSpawned.Count;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer > 5 && carSpawned.Count < maxNumberCars)
        {
            int i = 0;
            while(i < spawn.Count && carSpawned.Count < maxNumberCars )
            {
                carSpawned.Add(Instantiate(carPrefab, spawn[i].transform.position, spawn[i].transform.rotation));
                carSpawned[carSpawned.Count - 1].GetComponent<IMovable>().setTarget(spawn[i].transform.position);
                carSpawned[carSpawned.Count - 1].name = "Car " + carSpawned.Count;

                i++;
            }
            timer = 0;
        }
    }
}
