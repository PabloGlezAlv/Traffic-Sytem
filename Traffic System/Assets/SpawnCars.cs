using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCars : MonoBehaviour
{
    [SerializeField]
    List<GameObject> cars = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        Spawn();
    }

    private void Spawn()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            int carIndex = Random.Range(0, cars.Count);

            Instantiate(cars[carIndex], transform.GetChild(i).transform.position + new Vector3(0, 1, 0), transform.GetChild(i).transform.rotation);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
