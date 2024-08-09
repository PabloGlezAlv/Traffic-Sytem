using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CarMovement;
using static Point;

public interface IMovable
{
    behaviours getBehaviour();

    float GetMove();
    float GetSteer();

    void setTarget(List<Vector3> pos, List<Vector3> endLane, List<DrivingLane> lanes, PointType type, bool right, List<GameObject> parentDirs, bool lastPoint);

    void setTarget(Vector3 pos);

    void CalculateCarInput();

    void setSpeedLimit(float limit);

    Vector3 getTarget();
    void AddRewardAgent(float amount, bool kill = false);
}
