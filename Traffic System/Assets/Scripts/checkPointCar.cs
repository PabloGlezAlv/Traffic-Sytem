using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class checkPointCar : MonoBehaviour
{
    [SerializeField]
    int id = 0;

    private void OnTriggerEnter(Collider other)
    {
        CarLogicAI carLogic = other.gameObject.GetComponentInParent<CarLogicAI>();
        if (carLogic != null && carLogic.CheckpointID == id)
        {
            carLogic.AddRewardAgent(0.25f);
            carLogic.CheckpointID = id + 1;
        }
    }
}
