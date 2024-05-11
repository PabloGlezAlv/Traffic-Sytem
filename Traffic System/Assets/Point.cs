using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Point : MonoBehaviour
{
    [SerializeField]
    bool endPoint = false;

    [SerializeField]
    List<Vector3> nextPoints = new List<Vector3>();


    public void AddConexion(Vector3 p)
    {
        nextPoints.Add(p);
    }

    public List<Vector3> getNextPoint()
    {
        return nextPoints;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Car") 
        {
            CarMovement car = other.transform.parent.GetComponent<CarMovement>();
            
            Vector3 target = car.getTarget();

            if (Vector3.Distance(transform.position, target) < 0.1) 
            {
                car.setTarget(nextPoints);
            }
            
        }
    }
}
