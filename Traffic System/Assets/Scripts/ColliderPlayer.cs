using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderPlayer : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        CarLogicAI carLogic = collision.gameObject.GetComponentInParent<CarLogicAI>();
        if (carLogic != null )
        {
             carLogic.AddRewardAgent(-0.01f, true);
        }
    }
}
