using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopLightSystem : MonoBehaviour
{
    [SerializeField]
    float greenTime = 10;
    [SerializeField]
    float waitNextTime = 4;

    [SerializeField]
    List<Light> lights = new List<Light>();
    [SerializeField]
    List<GameObject> barrier = new List<GameObject>();

    List<Vector3> position = new List<Vector3>();

    int index = 0;

    private void Awake()
    {
        foreach(GameObject g in barrier)
        {
            position.Add(g.transform.position);
            
        }
    }

    void Start()
    {
        barrier[0].transform.position = new Vector3(0,-20,0);
        for (int i = 1; i < lights.Count; i++)
        {
            lights[i].color = Color.red;
        }

        Invoke("setRed", greenTime);
    }


    private void setRed()
    {
        barrier[index].transform.position = position[index];
        lights[index].color = Color.red;
        Invoke("setGreen", waitNextTime);
    }

    private void setGreen()
    {
        index = ++index%barrier.Count;

        barrier[index].transform.position = new Vector3(0, -20, 0);
        lights[index].color = Color.green;
        Invoke("setRed", greenTime);
    }
}
