using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckSideRoad : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        CarLogicAI carLogic = other.gameObject.GetComponentInParent<CarLogicAI>();
        if (carLogic != null)
        {
            carLogic.AddRewardAgent(-0.5f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        CarLogicAI carLogic = other.gameObject.GetComponentInParent<CarLogicAI>();
        if (carLogic != null)
        {
            carLogic.AddRewardAgent(-0.001f);
        }
    }
}
