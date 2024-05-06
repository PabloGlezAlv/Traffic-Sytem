using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using static UnityEditor.FilePathAttribute;

public class Route : MonoBehaviour
{
    [SerializeField]
    int numberLanes = 1;
    [SerializeField]
    int pointsDensity = 7;
    [SerializeField]
    bool spawnerRoute = true;
    [SerializeField]
    CarSpawner spawner;


    [SerializeField]
    GameObject midPoint;
    [SerializeField]
    GameObject endPoint;

    [SerializeField]
    List<RouteDirection> routeDirections;

    [Serializable]
    public struct RouteDirection
    {
        public int density;
        public Vector3 startTangent;
        public Vector3 endTangent;
        public GameObject directionObject;

        public RouteDirection(int dens, Vector3 start, Vector3 end, GameObject obj)
        {
            density = dens;
            startTangent = start;
            endTangent = end;
            directionObject = obj;
        }
    }

    private struct PointType
    {
        public Vector3 pos;
        public bool endPoint;
        public PointType(Vector3 position, bool isEndPoint)
        {
            pos = position;
            endPoint = isEndPoint;
        }
    }

    List<PointType> locations = new List<PointType>();

    List<Vector3> conectionLocations = new List<Vector3>();

    List<GameObject> movingPoints = new List<GameObject>(); //Points in the route

    List<GameObject> conectionPoints = new List<GameObject>(); //Points Created to conect the route with others

    private void ClearInfo()
    {
        locations.Clear();
        conectionLocations.Clear();
    }

    private void OnValidate()
    {
        ClearInfo();
        // Add start position
        Transform child = transform.GetChild(0);
        locations.Add(new PointType(child.position, true));

        // Add route points
        Vector3 p1 = child.position;
        for (int i = 1; i < transform.childCount; i++)
        {
            Vector3 p2 = transform.GetChild(i).position;

            for (int j = 1; j <= pointsDensity; j++)
            {
                locations.Add(new PointType(Vector3.Lerp(p1, p2, (float)j / (pointsDensity + 1)), false));
            }
            p1 = p2;

            locations.Add(new PointType(p2, true));
        }

        //Add connections points
        conectionLocations.Add(locations[locations.Count - 1].pos); //First one is last point
        for (int i = 0; i < routeDirections.Count; i++) // Points with intersections
        {
            Vector3 start = locations[locations.Count - 1].pos;
            Vector3 end = routeDirections[i].directionObject.transform.position;

            int numPoints = routeDirections[i].density;
            for (int j = 1; j < numPoints + 1; j++)
            {
                float t = j / (float)(numPoints + 1);
                Vector3 point = HermiteInterpolation(start, end, routeDirections[i].startTangent, routeDirections[i].endTangent, t);
                conectionLocations.Add(point);
            }
        }
    }
    private Vector3 HermiteInterpolation(Vector3 startPoint, Vector3 endPoint, Vector3 startTangent, Vector3 endTangent, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        float blend1 = 2 * t3 - 3 * t2 + 1;
        float blend2 = -2 * t3 + 3 * t2;
        float blend3 = t3 - 2 * t2 + t;
        float blend4 = t3 - t2;

        return blend1 * startPoint + blend2 * endPoint + blend3 * startTangent + blend4 * endTangent;
    }

    void Awake()
    {
        int endPointIndex = 0;
        for (int i = 0; i < locations.Count; i++) // Points with intersections
        {
            if (locations[i].endPoint)
            {
                movingPoints.Add(Instantiate(endPoint, locations[i].pos, transform.GetChild(endPointIndex).transform.rotation, transform));
                endPointIndex++;
            }
            else
            {
                movingPoints.Add(Instantiate(midPoint, locations[i].pos, transform.rotation, transform));
            }

            if (i != 0) //Add to the previous point the last one
            {
                movingPoints[i - 1].GetComponent<Point>().AddConexion(movingPoints[i].transform.position);

                
            }
        }
        // First point is a Spawner

        if(spawnerRoute) spawner.addSpawnPoint(movingPoints[0]);

        //Connect route with next ones

        int pointIndex = 1; //First one is the start
        for (int i = 0; i < routeDirections.Count; i++) // Each Direction
        {
            conectionPoints.Add(Instantiate(midPoint, conectionLocations[pointIndex], transform.rotation, transform));

            movingPoints[movingPoints.Count - 1].GetComponent<Point>().AddConexion(conectionLocations[pointIndex]); //Firs element always connected with end of route

            for (int j = pointIndex + 1; j < pointIndex + routeDirections[i].density; j++) // Points each direction
            {
                conectionPoints.Add(Instantiate(midPoint, conectionLocations[j], transform.rotation, transform));

                conectionPoints[conectionPoints.Count - 2].GetComponent<Point>().AddConexion(conectionLocations[j]);
            }

            //Add last conexion to next Route
            conectionPoints[conectionPoints.Count - 1].GetComponent<Point>().AddConexion(routeDirections[i].directionObject.transform.position);

            pointIndex += routeDirections[i].density;
        }
    }

    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < locations.Count; i++)
        {
            if (locations[i].endPoint)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(locations[i].pos, 0.3f);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(locations[i].pos, 0.15f);
            }
        }

        Gizmos.color = Color.red;
        for (int i = 0; i < locations.Count - 1; i++)
        {
            Gizmos.DrawLine(locations[i].pos, locations[i + 1].pos);
        }

        Gizmos.color = Color.yellow;
        int pointIndex = 1;
        for (int i = 0; i < routeDirections.Count; i++) // Each Direction
        {
            Gizmos.DrawSphere(conectionLocations[pointIndex], 0.15f);

            Gizmos.DrawLine(conectionLocations[0], conectionLocations[pointIndex]); 

            for (int j = pointIndex + 1; j < pointIndex + routeDirections[i].density; j++) // Points each direction
            {
                Gizmos.DrawSphere(conectionLocations[j], 0.15f);

                Gizmos.DrawLine(conectionLocations[j - 1], conectionLocations[j]);
            }

            Gizmos.DrawLine(conectionLocations[pointIndex + routeDirections[i].density - 1], routeDirections[i].directionObject.transform.position);

            pointIndex += routeDirections[i].density;
        }
    }
}
