using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MoveTarget : Agent
{
    [SerializeField]
    Transform target;

    float distance;

    public override void OnEpisodeBegin()
    {
        transform.position = Vector3.zero;

        distance = Vector3.Distance(transform.position, target.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(target.position);
    }


    // Size of actions set in the inspector Continuous multiple values, Discrete Branch Set Different values
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        float moveSpeed = 1f;
        transform.position += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;
    }

    //To test we set values
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment <float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Car") //Achive target Car tag just to test
        {
            SetReward(+2);
            EndEpisode();
        }
        else // Wall collision
        {
            SetReward(-2);
            EndEpisode();
        }
    }
}
