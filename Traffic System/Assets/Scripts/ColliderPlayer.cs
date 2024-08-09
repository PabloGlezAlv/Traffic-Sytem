using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderPlayer : MonoBehaviour
{
    private Vector3 _finalTarget;
    private bool isConexion = false;
    public Vector3 FinalTarget
    {
        get { return _finalTarget; }
        set { _finalTarget = value; isConexion = true; }
    }

    private void OnCollisionEnter(Collision collision)
    {
        CarLogicAI carLogic = collision.gameObject.GetComponent<CarLogicAI>();

        if(!isConexion)
        {
            if (carLogic != null)
            {
                carLogic.AddRewardAgent(-0.01f, true);
            }
        }
        else
        {
            if (carLogic != null && _finalTarget == carLogic.getFinalPoint())
            {
                carLogic.AddRewardAgent(-0.01f, true);
            }
        }


        CarLogicAICompetive carLogicComp = collision.gameObject.GetComponent<CarLogicAICompetive>();

        if (!isConexion)
        {
            if (carLogicComp != null)
            {
                carLogicComp.AddRewardAgent(-0.01f, true);
            }
        }
        else
        {
            if (carLogicComp != null && _finalTarget == carLogicComp.getFinalPoint())
            {
                carLogicComp.AddRewardAgent(-0.01f, true);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CarLogicAI carLogic = other.gameObject.GetComponent<CarLogicAI>();
        if (!isConexion)
        {
            if (carLogic != null)
            {
                carLogic.AddRewardAgent(-0.01f, true);
            }
        }
        else
        {
            if(carLogic != null && _finalTarget == carLogic.getFinalPoint())
            {
                carLogic.AddRewardAgent(-0.01f, true);
            }
        }

        CarLogicAICompetive carLogicComp = other.gameObject.GetComponent<CarLogicAICompetive>();
        if (!isConexion)
        {
            if (carLogicComp != null)
            {
                carLogicComp.AddRewardAgent(-0.01f, true);
            }
        }
        else
        {
            if (carLogicComp != null && _finalTarget == carLogicComp.getFinalPoint())
            {
                carLogicComp.AddRewardAgent(-0.01f, true);
            }
        }
    }
}
