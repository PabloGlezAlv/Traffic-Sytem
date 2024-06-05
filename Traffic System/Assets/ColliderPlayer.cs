using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderPlayer : MonoBehaviour
{
    [SerializeField]
    CarLogicAI carLogic;


    private void OnCollisionEnter(Collision collision)
    {
        carLogic.AddRewardAgent(-10);
    }

    private void OnCollisionStay(Collision collision)
    {
        carLogic.AddRewardAgent(-1);
    }
}
