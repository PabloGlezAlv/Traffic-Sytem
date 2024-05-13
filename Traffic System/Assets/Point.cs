﻿using System;
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
    float speedLimit = -1;

    [SerializeField]
    List<Vector3> nextPoints = new List<Vector3>();

    [SerializeField]
    List<Vector3> endTrail = new List<Vector3>();

    void SendCarSpeedLimit(CarMovement car)
    {
        if (speedLimit == -1) return;

        car.setSpeedLimit(speedLimit);
    }

    public void setSpeedLimit(float speedLimit)
    { 
        this.speedLimit = speedLimit;
    }

    public void AddTrailEnd(Vector3 p)
    {
        endTrail.Add(p);
    }

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
                SendCarSpeedLimit(car);
                car.setTarget(nextPoints, endTrail, endPoint);
            }
            
        }
    }
}
