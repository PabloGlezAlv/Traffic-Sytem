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

    int index = 0;

    void Start()
    {
        barrier[index].SetActive(false);
        for(int i = 1; i < lights.Count; i++)
        {
            lights[i].color = Color.red;
        }

        Invoke("setRed", greenTime);
    }


    private void setRed()
    {
        barrier[index].SetActive(true);
        lights[index].color = Color.red;
        Invoke("setGreen", waitNextTime);
    }

    private void setGreen()
    {
        index = ++index%barrier.Count;

        barrier[index].SetActive(false);
        lights[index].color = Color.green;
        Invoke("setRed", greenTime);
    }
}
