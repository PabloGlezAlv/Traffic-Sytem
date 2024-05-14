using System;
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

    void SendCarSpeedLimit(CarMovement car)
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
                car.setTarget(nextPoints, endTrail, nextLane, type);
            }
            
        }
    }
}
