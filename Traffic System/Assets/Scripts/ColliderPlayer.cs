using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderPlayer : MonoBehaviour
{
    [SerializeField]
    CarLogicAI carLogic;

    private void OnCollisionEnter(Collision collision)
    {
        carLogic.AddRewardAgent(-0.01f);
        carLogic.killCar();
    }
}
