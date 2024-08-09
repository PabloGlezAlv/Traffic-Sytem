using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckSideRoad : MonoBehaviour
{
    [SerializeField]
    bool sideRight = true;
    private void OnTriggerEnter(Collider other)
    {
        CarLogicAI carLogic = other.gameObject.GetComponentInParent<CarLogicAI>();
        if (carLogic != null && carLogic.GetRight() != sideRight)
        {
            carLogic.AddRewardAgent(-0.5f);
        }

        CarLogicAICompetive carLogicComp = other.gameObject.GetComponentInParent<CarLogicAICompetive>();
        if (carLogicComp != null && carLogicComp.GetRight() != sideRight)
        {
            carLogicComp.AddRewardAgent(-0.5f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        CarLogicAI carLogic = other.gameObject.GetComponentInParent<CarLogicAI>();
        if (carLogic != null && carLogic.GetRight() != sideRight)
        {
            carLogic.AddRewardAgent(-0.001f);
        }

        CarLogicAICompetive carLogicComp = other.gameObject.GetComponentInParent<CarLogicAICompetive>();
        if (carLogicComp != null && carLogicComp.GetRight() != sideRight)
        {
            carLogicComp.AddRewardAgent(-0.001f);
        }
    }
}
