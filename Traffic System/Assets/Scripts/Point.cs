﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static CarMovement;

public class Point : MonoBehaviour
{
    public enum PointType { Start, End, Mid }

    [SerializeField]
    PointType type = PointType.Mid;

    [SerializeField]
    DrivingLane lane = DrivingLane.OneLane;

    [SerializeField]
    float speedLimit = -1;

    [SerializeField]
    List<Vector3> nextPoints = new List<Vector3>();

    [SerializeField]
    List<DrivingLane> nextLane = new List<DrivingLane>();

    [SerializeField]
    List<Vector3> endTrail = new List<Vector3>();

    [SerializeField]
    bool right = true;
    [SerializeField]
    bool lastPoint = false;

    List<GameObject> conectionParent = new List<GameObject>();

    void SendCarSpeedLimit(IMovable car)
    {
        if (speedLimit == -1) return;

        car.setSpeedLimit(speedLimit);
    }

    public void setSpeedLimit(float speedLimit)
    { 
        this.speedLimit = speedLimit;
    }

    public void setLane(DrivingLane lane)
    {
        this.lane = lane;
    }

    public void setRight(bool r)
    {
        right = r;
    }

    public void AddConexionParent(GameObject obj)
    {
        conectionParent.Add(obj);
    }

    public void AddTrailEnd(Vector3 p)
    {
        endTrail.Add(p);
    }

    public void AddConexion(Vector3 p)
    {
        nextPoints.Add(p);
    }
    public void AddConexionLane(DrivingLane p)
    {
        nextLane.Add(p);
    }

    public void SetLastPoint()
    {
        lastPoint = true;
    }

    public List<Vector3> getNextPoint()
    {
        return nextPoints;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Car") 
        {
            
            IMovable car = other.transform.GetComponent<IMovable>();


            if (car.getBehaviour() == behaviours.competitive)
            {
                car.AddRewardAgent(0.25f);

                if(type == PointType.Start) // Just to make visible the side where the car is running
                {
                    car.setTarget(nextPoints, endTrail, nextLane, type, right, conectionParent, lastPoint);
                }
            }
            else
            {
                Vector3 target = car.getTarget();

                if (Vector3.Distance(transform.position, target) < 0.01)
                {
                    if (type != PointType.Start)
                        SendCarSpeedLimit(car);

                    car.setTarget(nextPoints, endTrail, nextLane, type, right, conectionParent, lastPoint);

                    car.AddRewardAgent(0.25f);
                }
            }
        }
    }
}
