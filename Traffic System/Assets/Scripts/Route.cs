using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.MemoryProfiler;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using static CarMovement;
using static Point;
using static UnityEditor.FilePathAttribute;
using static UnityEditor.PlayerSettings;

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
    bool isRight = false;

    [SerializeField]
    GameObject startPoint;
    [SerializeField]
    GameObject midPoint;
    [SerializeField]
    GameObject endPoint;
    [SerializeField]
    GameObject blockZone;

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

    private struct PointInfo
    {
        public Vector3 pos;
        public DrivingLane lane;
        public PointType type;
        public PointInfo(Vector3 position, PointType isEndPoint, DrivingLane l)
        {
            pos = position;
            type = isEndPoint;
            lane = l;
        }
    }

    List<PointInfo> locations = new List<PointInfo>();

    List<Vector3> conectionLocations = new List<Vector3>();

    List<GameObject> movingPoints = new List<GameObject>(); //Points in the route

    List<GameObject> conectionPoints = new List<GameObject>(); //Points Created to conect the route with others

    List<GameObject> conectionParent = new List<GameObject>(); //Parent of each conection

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


        DrivingLane lane = DrivingLane.OneLane;
        for(int l = 0; l < numberLanes; l++) //Run all lanes
        {
            if (numberLanes != 1)
            {
                if (l == 0) lane = DrivingLane.Left;
                else if (l == 1) lane = DrivingLane.Right;
            }
            // Add route points
            CreateLanePoints(child, lane, l);

            //Add connections points
            CreateConectionPoints(l);
        }
    }

    private void CreateConectionPoints(int l)
    {
        conectionLocations.Add(locations[locations.Count - 1].pos); //First one is last point
        for (int i = 0; i < routeDirections.Count; i++) // Points with intersections
        {
            if (routeDirections[i].onlyLeft && l != 0) continue;
            if (routeDirections[i].onlyRight && l != numberLanes - 1) continue;

            Vector3 start = locations[locations.Count - 1].pos;
            Vector3 end;
            List<Vector3> endList = new List<Vector3>();


            try
            {
                endList = routeDirections[i].directionObject.GetComponentInParent<Route>().GetStartPosition();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error carga datos: " + gameObject.name);
            }


            if(endList.Count > 0 )
            {
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

    private void CreateLanePoints(Transform child, DrivingLane lane, int l)
    {
        Vector3 p1 = child.position;
        for (int i = 1; i < transform.childCount; i++) //Create each point in the lane
        {
            Vector3 p2 = transform.GetChild(i).position;

            locations.Add(new PointInfo(getPerpendicularPoint(p1, p2, lanesDistance * routeIndex[l]), PointType.Start, lane));

            for (int j = 1; j <= pointsDensity; j++)
            {
                locations.Add(new PointInfo(getPerpendicularPoint(Vector3.Lerp(p1, p2, (float)j / (pointsDensity + 1)), p2, lanesDistance * routeIndex[l]), PointType.Mid, lane));
            }

            locations.Add(new PointInfo(getPerpendicularPoint(p2, p1, -lanesDistance * routeIndex[l]), PointType.End, lane));

            p1 = p2;
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
        SpawnLinePoints();

        SpawnConectionPoints();
    }

    private void SpawnConectionPoints()
    {
        int pointIndex = 1; //First one is the start
        int startIndex = 0; //Postion of last point in route

        for (int l = 0; l < numberLanes; l++)
        {
            //Debug.Log("Linea: " + l);
            startIndex += pointsDensity + 1;
            for (int i = 0; i < routeDirections.Count; i++) // Each Direction
            {
                if (routeDirections[i].onlyLeft && l != 0) continue;
                if (routeDirections[i].onlyRight && l != numberLanes - 1) continue;

                //Createa parent for each conection
                GameObject emptyObject = new GameObject("ConectionParent " + i + "Lane" + l);
                emptyObject.AddComponent<ConectionData>();
                emptyObject.transform.position = conectionLocations[pointIndex];
                emptyObject.transform.SetParent(this.transform);
                conectionParent.Add(emptyObject);

                //Create first point
                conectionPoints.Add(Instantiate(midPoint, conectionLocations[pointIndex], transform.rotation, emptyObject.transform));

                //Add next point to first of line
                movingPoints[startIndex].GetComponent<Point>().AddConexion(conectionLocations[pointIndex]);
                //Add parent of conexion
                movingPoints[startIndex].GetComponent<Point>().AddConexionParent(conectionParent[conectionParent.Count -1]);
                pointIndex++;
                //Add to first point the end of conection
                movingPoints[startIndex].GetComponent<Point>().AddTrailEnd(routeDirections[i].directionObject.GetComponentInParent<Route>().GetStartPosition()[l]);

                conectionPoints[conectionPoints.Count - 1].transform.LookAt(movingPoints[startIndex].transform.position);

                //Last point before conection
                Vector3 lastBeforeCon = movingPoints[startIndex].transform.position;

                //Add the rest of points;
                for (int j = pointIndex; j < pointIndex + routeDirections[i].density - 1; j++) // Points each direction
                {
                    conectionPoints.Add(Instantiate(midPoint, conectionLocations[j], transform.rotation, emptyObject.transform));

                    conectionPoints[conectionPoints.Count - 1].transform.LookAt(conectionPoints[conectionPoints.Count - 2].transform.position);

                    conectionPoints[conectionPoints.Count - 2].GetComponent<Point>().AddConexion(conectionPoints[conectionPoints.Count - 1].transform.position);
                    conectionPoints[conectionPoints.Count - 2].GetComponent<Point>().AddTrailEnd(routeDirections[i].directionObject.GetComponentInParent<Route>().GetStartPosition()[l]);
                }

                Point lastPoint = conectionPoints[conectionPoints.Count - 1].GetComponent<Point>();
                //Last point conected with start of next line
                Vector3 firstAfterCon = routeDirections[i].directionObject.GetComponentInParent<Route>().GetStartPosition()[l];

                lastPoint.AddConexion(firstAfterCon);
                lastPoint.AddTrailEnd(firstAfterCon);

                lastPoint.GetComponent<Point>().setRight(isRight);

                //Send last point the parent and set end of route
                lastPoint.AddConexionParent(conectionParent[conectionParent.Count - 1]);
                lastPoint.SetLastPoint();

                pointIndex += routeDirections[i].density - 1;
                emptyObject.SetActive(false);

                

                if(blockZone != null) //Add walls in conecctions
                {
                    SpawConectionBorder(startIndex, l, i, emptyObject, lastBeforeCon, firstAfterCon, 1);
                    SpawConectionBorder(startIndex, l, i, emptyObject, lastBeforeCon, firstAfterCon, -1);
                }
            }


            pointIndex++; //Add the start of the next line
            startIndex++; //Add the start of the next line
        }
    }

    private void SpawConectionBorder(int startIndex, int l, int i, GameObject emptyObject, Vector3 lastBeforeCon, Vector3 firstAfterCon, int sign)
    {
        GameObject start = Instantiate(blockZone, lastBeforeCon, movingPoints[startIndex].transform.rotation * Quaternion.Euler(0, 180, 0), emptyObject.transform);
        start.name = "BlockZone " + l + i;
        start.transform.Translate(start.transform.right * sign * 2.3f, Space.World);

        GameObject end = Instantiate(blockZone, firstAfterCon, routeDirections[i].directionObject.transform.rotation, emptyObject.transform);
        start.name = "BlockZone " + l + i;
        end.transform.Translate(end.transform.right * sign * 2.3f, Space.World);

        float angle = Vector3.Angle(start.transform.forward, end.transform.forward);


        int numPoints = (int)Vector3.Distance(start.transform.position, end.transform.position) * 4;

        Vector3 forward = movingPoints[startIndex].transform.forward;

        Vector2 posicionObjeto = new Vector2(start.transform.position.x, start.transform.position.z);
        Vector2 posicionObjeto2 = new Vector2(end.transform.position.x, end.transform.position.z);
        Vector2 vectorPunto = posicionObjeto2 - posicionObjeto;

        // Producto cruzado en 2D
        float crossProduct = forward.x * vectorPunto.y - forward.z * vectorPunto.x;

        if (crossProduct > 0.03f)//Right
        {
            for (int j = 1; j < numPoints + 1; j++)
            {
                float t = j / (float)(numPoints + 1);
                Vector3 point = HermiteInterpolation(start.transform.position, end.transform.position, routeDirections[i].startTangent, routeDirections[i].endTangent, t);

                GameObject p = Instantiate(blockZone, point, Quaternion.Euler(0, start.transform.rotation.eulerAngles.y + angle / numPoints * j, 0), emptyObject.transform);

                p.layer = LayerMask.NameToLayer("ConectionRight");
            }
            start.layer = LayerMask.NameToLayer("ConectionRight");
            end.layer = LayerMask.NameToLayer("ConectionRight");
        }
        else if (crossProduct < -0.03f) // Left
        {
            for (int j = 1; j < numPoints + 1; j++)
            {
                float t = j / (float)(numPoints + 1);
                Vector3 point = HermiteInterpolation(start.transform.position, end.transform.position, routeDirections[i].startTangent, routeDirections[i].endTangent, t);

                GameObject p = Instantiate(blockZone, point, Quaternion.Euler(0, start.transform.rotation.eulerAngles.y - angle / numPoints * j, 0), emptyObject.transform);
                p.layer = LayerMask.NameToLayer("ConectionLeft");
            }
            start.layer = LayerMask.NameToLayer("ConectionLeft");
            end.layer = LayerMask.NameToLayer("ConectionLeft");
        }
        else // Front
        {
            for (int j = 1; j < numPoints + 1; j++)
            {
                float t = j / (float)(numPoints + 1);
                Vector3 point = HermiteInterpolation(start.transform.position, end.transform.position, routeDirections[i].startTangent, routeDirections[i].endTangent, t);

                GameObject p = Instantiate(blockZone, point, Quaternion.Euler(0, start.transform.rotation.eulerAngles.y, 0), emptyObject.transform);
                p.layer = LayerMask.NameToLayer("ConectionFront");
            }
            start.layer = LayerMask.NameToLayer("ConectionFront");
            end.layer = LayerMask.NameToLayer("ConectionFront");
        }
    }

    private void SpawnLinePoints()
    {
        int points = locations.Count / numberLanes;
        for (int l = 0; l < numberLanes; l++) //Route conecction
        {
            int endPointIndex = 0;
            int limit = ((points * (l + 1)));
            for (int i = points * l; i < limit; i++)
            {
                if (locations[i].type == PointType.End)
                {
                    movingPoints.Add(Instantiate(endPoint, locations[i].pos, transform.GetChild(endPointIndex).transform.rotation, transform));

                    movingPoints[movingPoints.Count - 1].GetComponent<Point>().setSpeedLimit(exitSpeed);

                    endPointIndex++;
                }
                else if (locations[i].type == PointType.Start)
                {
                    movingPoints.Add(Instantiate(startPoint, locations[i].pos, transform.GetChild(endPointIndex).transform.rotation, transform));

                    movingPoints[movingPoints.Count - 1].GetComponent<Point>().setSpeedLimit(enterSpeed);

                    endPointIndex++;
                    if (spawnerRoute && l ==1) spawner.addSpawnPoint(movingPoints[movingPoints.Count - 1]);

                    movingPoints[movingPoints.Count - 1].transform.LookAt(locations[limit - 1].pos);
                }
                else
                {
                    movingPoints.Add(Instantiate(midPoint, locations[i].pos, transform.rotation, transform));

                    float speed = enterSpeed;

                    if(exitSpeed < enterSpeed && points > 2 && limit - 2 <= i) //Close to the end if endspeeed lower reduce in the last 2 points
                    {
                        speed = Mathf.Lerp(exitSpeed, enterSpeed,  limit - i/ 3);
                    }
                    movingPoints[movingPoints.Count - 1].GetComponent<Point>().setSpeedLimit(enterSpeed);
                }

                if (i != points * l) //Add to the previous point the last one
                {
                    movingPoints[i - 1].GetComponent<Point>().AddConexion(movingPoints[i].transform.position);
                    movingPoints[i - 1].GetComponent<Point>().AddTrailEnd(locations[limit - 1].pos);
                    movingPoints[i - 1].GetComponent<Point>().AddConexionLane(locations[limit - 1].lane);

                    movingPoints[i].transform.LookAt(movingPoints[i - 1].transform.position);
                }

                //Tell the lane of the point
                movingPoints[i].GetComponent<Point>().setLane(locations[i].lane);
                movingPoints[i].GetComponent<Point>().setRight(isRight);
            }


            if (enterSpeed > exitSpeed && points > 6) //In case enough point decelerate in lst point
            {
                movingPoints[movingPoints.Count - 2].GetComponent<Point>().setSpeedLimit(Mathf.Lerp(exitSpeed, enterSpeed, 0.33f));
                movingPoints[movingPoints.Count - 3].GetComponent<Point>().setSpeedLimit(Mathf.Lerp(exitSpeed, enterSpeed, 0.66f));
            }
        }

        if(numberLanes > 1)
        {
            for (int l = 0; l < numberLanes; l++)
            {
                for (int i = points * l + 1; i < points * (l + 1) - numberLanes; i++)
                {
                    
                    if (l == 0)
                    {
                        int dest = points + i + 1; // THE 1 represent the point in the line of l = 1
                        

                        movingPoints[i].GetComponent<Point>().AddConexion(movingPoints[dest].transform.position);
                        movingPoints[i].GetComponent<Point>().AddConexionLane(DrivingLane.Right);
                    }
                    else if (l == 1)
                    {
                        int dest = i + 1 - points;

                        movingPoints[i].GetComponent<Point>().AddConexion(movingPoints[dest].transform.position);
                        movingPoints[i].GetComponent<Point>().AddConexionLane(DrivingLane.Left);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Position of the points
        Gizmos.color = Color.blue;
        for (int i = 0; i < transform.childCount; i++)
        {
            Gizmos.DrawSphere(transform.GetChild(i).position, 0.3f);
        }
        // Position of points in the line
        for (int i = 0; i < locations.Count; i++)
        {
            if (locations[i].type == PointType.Start)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(locations[i].pos, 0.3f);
            }
            else if (locations[i].type == PointType.End)
            {
                Gizmos.color = Color.black;
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

                if(numberLanes > 1 && i > points *l + 1 && i < points *(l+1) -1)
                {
                    Gizmos.color = Color.green;
                    if (l == 0)
                    {
                        int dest = points * 1 + i - points * l - 1; // THE 1 represent the point in the line of l = 1

                        Gizmos.DrawLine(locations[i].pos, locations[dest].pos);
                    }
                    else if(l == 1)
                    {
                        int dest = i - points * l - 1;
                        Gizmos.DrawLine(locations[i].pos, locations[dest].pos);
                    }

                    Gizmos.color = Color.red;
                }
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
