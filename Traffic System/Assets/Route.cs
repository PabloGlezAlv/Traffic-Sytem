using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor.MemoryProfiler;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using static UnityEditor.FilePathAttribute;

public class Route : MonoBehaviour
{
    [SerializeField]
    int numberLanes = 2;
    [SerializeField]
    float lanesDistance = 1.4f;
    [SerializeField]
    int pointsDensity = 7;
    [SerializeField]
    bool spawnerRoute = true;
    [SerializeField]
    CarSpawner spawner;
    [SerializeField]
    float enterSpeed = 60;
    [SerializeField]
    float exitSpeed = 20;


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
        public bool onlyLeft;
        public bool onlyRight;

        public RouteDirection(int dens, Vector3 start, Vector3 end, GameObject obj, bool left, bool right)
        {
            density = dens;
            startTangent = start;
            endTangent = end;
            directionObject = obj;
            onlyLeft = left;
            onlyRight = right;
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

    List<int> routeIndex = new List<int>()
    {
        1, -1, 2, -2, 3, -3, 4, -4, 5, -5
    };
    private void ClearInfo()
    {
        locations.Clear();
        conectionLocations.Clear();
    }


    public List<Vector3> GetStartPosition()
    {
        List<Vector3> positions = new List<Vector3>();

        Vector3 p1 = transform.GetChild(0).position;
        Vector3 p2 = transform.GetChild(1).position;

        for (int l = 0; l < numberLanes; l++)
        {
            positions.Add(getPerpendicularPoint(p1, p2, lanesDistance * routeIndex[l]));
        }
        return positions;
    }

    private void OnValidate()
    {
        ClearInfo();
        // Add start position
        Transform child = transform.GetChild(0);


        for(int l = 0; l < numberLanes; l++) //Run all lanes
        {
            // Add route points
            Vector3 p1 = child.position;
            for (int i = 1; i < transform.childCount; i++) //Create each point in the lane
            {
                Vector3 p2 = transform.GetChild(i).position;

                locations.Add(new PointType(getPerpendicularPoint(p1, p2, lanesDistance * routeIndex[l]), true));
                
                for (int j = 1; j <= pointsDensity; j++)
                {
                    locations.Add(new PointType(getPerpendicularPoint(Vector3.Lerp(p1, p2, (float)j / (pointsDensity + 1)), p2, lanesDistance * routeIndex[l]), false));
                }

                locations.Add(new PointType(getPerpendicularPoint(p2, p1, -lanesDistance * routeIndex[l]), true));

                p1 = p2;
            }

            //Add connections points
            conectionLocations.Add(locations[locations.Count - 1].pos); //First one is last point
            for (int i = 0; i < routeDirections.Count; i++) // Points with intersections
            {
                if (routeDirections[i].onlyLeft && l != 0) continue;
                if (routeDirections[i].onlyRight && l != numberLanes -1) continue;

                Vector3 start = locations[locations.Count - 1].pos;
                Vector3 end;
                List<Vector3> endList;
                
                endList = routeDirections[i].directionObject.GetComponentInParent<Route>().GetStartPosition();
                end = endList[l];

                int numPoints = routeDirections[i].density;
                for (int j = 1; j < numPoints + 1; j++)
                {
                    float t = j / (float)(numPoints + 1);
                    Vector3 point = HermiteInterpolation(start, end, routeDirections[i].startTangent, routeDirections[i].endTangent, t);
                    conectionLocations.Add(point);
                }
            }
        }
    }

    private Vector3 getPerpendicularPoint(Vector3 start, Vector3 end, float distance)
    {
        Vector2 start2D = new Vector2(start.x, start.z);
        Vector2 end2D = new Vector2(end.x, end.z);

        Vector2 direction = (end2D - start2D).normalized;

        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        Vector2 offset = perpendicular * distance;

        Vector2 perpendicularPoint2D = start2D + offset;

        Vector3 perpendicularPoint = new Vector3(perpendicularPoint2D.x, start.y, perpendicularPoint2D.y);

        // Retorna el punto perpendicular
        return perpendicularPoint;
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
        //-------------Route points--------------------------
        int points = locations.Count / numberLanes;
        for (int l = 0; l < numberLanes; l++) //Route conecction
        {
            int endPointIndex = 0;
            int limit = ((points * (l + 1)));
            for (int i = points * l; i < limit; i++)
            {
                if (locations[i].endPoint)
                {
                    movingPoints.Add(Instantiate(endPoint, locations[i].pos, transform.GetChild(endPointIndex).transform.rotation, transform));
                    if(endPointIndex%2 == 0) //Set speed for start 
                    {
                        movingPoints[movingPoints.Count - 1].GetComponent<Point>().setSpeedLimit(enterSpeed);
                    }
                    else
                    {
                        movingPoints[movingPoints.Count - 1].GetComponent<Point>().setSpeedLimit(exitSpeed);
                    }
                    endPointIndex++;
                    if (spawnerRoute && i == points * l) spawner.addSpawnPoint(movingPoints[movingPoints.Count - 1]);
                }
                else
                {
                    movingPoints.Add(Instantiate(midPoint, locations[i].pos, transform.rotation, transform));
                }

                if (i != points * l) //Add to the previous point the last one
                {
                    movingPoints[i - 1].GetComponent<Point>().AddConexion(movingPoints[i].transform.position);
                    movingPoints[i - 1].GetComponent<Point>().AddTrailEnd(locations[limit - 1].pos);
                }
            }
        }


        //-------------Conection points---------------------------
        int pointIndex = 1; //First one is the start
        int startIndex = 0; //Postion of last point in route
        for (int l = 0; l < numberLanes; l++)
        {
            //Debug.Log("Linea: " + l);
            startIndex += pointsDensity + 1;
            for (int i = 0; i < routeDirections.Count; i++) // Each Direction
            {
                
                conectionPoints.Add(Instantiate(midPoint, conectionLocations[pointIndex], transform.rotation, transform));
                
                //Add next point to first of line
                movingPoints[startIndex].GetComponent<Point>().AddConexion(conectionLocations[pointIndex]);
                pointIndex++;
                //Add to first point the end of conection
                movingPoints[startIndex].GetComponent<Point>().AddTrailEnd(routeDirections[i].directionObject.GetComponentInParent<Route>().GetStartPosition()[l]);

                //Add the rest of points;
                for (int j = pointIndex; j < pointIndex + routeDirections[i].density - 1; j++) // Points each direction
                {
                    conectionPoints.Add(Instantiate(midPoint, conectionLocations[j], transform.rotation, transform));
                    
                    conectionPoints[conectionPoints.Count - 2].GetComponent<Point>().AddConexion(conectionPoints[conectionPoints.Count - 1].transform.position);
                    conectionPoints[conectionPoints.Count - 2].GetComponent<Point>().AddTrailEnd(routeDirections[i].directionObject.GetComponentInParent<Route>().GetStartPosition()[l]);
                }

                //Last point conected with start of next line
                conectionPoints[conectionPoints.Count - 1].GetComponent<Point>().AddConexion(routeDirections[i].directionObject.GetComponentInParent<Route>().GetStartPosition()[l]);
                conectionPoints[conectionPoints.Count - 1].GetComponent<Point>().AddTrailEnd(routeDirections[i].directionObject.GetComponentInParent<Route>().GetStartPosition()[l]);

                pointIndex += routeDirections[i].density - 1;

            }
            pointIndex++; //Add the start of the next line
            startIndex++; //Add the start of the next line
        }
    }

    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        // Position of the points
        Gizmos.color = Color.white;
        for (int i = 0; i < transform.childCount; i++)
        {
            Gizmos.DrawSphere(transform.GetChild(i).position, 0.3f);
        }
        // Position of points in the line
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

        //Draw lines of the road
        Gizmos.color = Color.red;
        int points = locations.Count / numberLanes;
        for (int l = 0; l < numberLanes; l++)
        {
            for (int i = points * l; i < (points *(l+1)) - 1; i++)
            {
                Gizmos.DrawLine(locations[i].pos, locations[i + 1].pos);
            }
        }

        Gizmos.color = Color.yellow;
        int pointIndex = 1;
        for (int l = 0; l < numberLanes; l++)
        {
            int startIndex = pointIndex - 1;
            for (int i = 0; i < routeDirections.Count; i++) // Each Direction
            {
                if (routeDirections[i].onlyLeft && l != 0) continue;
                if (routeDirections[i].onlyRight && l != numberLanes - 1) continue;

                Gizmos.DrawSphere(conectionLocations[pointIndex], 0.15f);

                Gizmos.DrawLine(conectionLocations[startIndex], conectionLocations[pointIndex]);

                for (int j = pointIndex + 1; j < pointIndex + routeDirections[i].density; j++) // Points each direction
                {
                    Gizmos.DrawSphere(conectionLocations[j], 0.15f);

                    Gizmos.DrawLine(conectionLocations[j - 1], conectionLocations[j]);
                }

                Gizmos.DrawLine(conectionLocations[pointIndex + routeDirections[i].density - 1], routeDirections[i].directionObject.GetComponentInParent<Route>().GetStartPosition()[l]);

                pointIndex += routeDirections[i].density;
            }
            pointIndex++;
        }
    }
}
