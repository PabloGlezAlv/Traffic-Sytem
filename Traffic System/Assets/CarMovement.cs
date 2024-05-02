using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarMovement : MonoBehaviour
{
    [SerializeField]
    float frontCarDistance = 15;
    [SerializeField]
    float speed = 2;


    void Start()
    {
        
    }

    
    void Update()
    {
        RaycastHit hit;

        Physics.Raycast(transform.position, transform.forward, out hit, frontCarDistance);


        if(hit.transform) // Collision with something
        {
            Debug.Log("Car collison");
            if (hit.transform.tag == "Car")
            {
                Stop();
            }
        }
        else
        {
            Drive();
        }
    }

    private void Stop()
    {
        transform.position += Vector3.zero;
    }

    private void Drive()
    {
        transform.position += new Vector3(speed * Time.deltaTime, 0, 0);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * frontCarDistance);
    }

}
