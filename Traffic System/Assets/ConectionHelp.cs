using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Point;

public class ConectionHelp : MonoBehaviour
{
    [SerializeField]
    Vector3 parentPosition;

    bool getReward = true;

    void Start()
    {
        parentPosition = transform.parent.position;
    }

    private void resetReward()
    {
        getReward = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        CarLogicAI car = other.transform.GetComponent<CarLogicAI>();

        if (car != null && getReward && Vector3.Distance(car.getPreviousTarget(),parentPosition) < 0.01)
        {
            getReward = false;
            Invoke("resetReward", 15f);
            car.AddRewardAgent(0.25f);
        }
    }
}
