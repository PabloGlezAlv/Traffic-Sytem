using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class CarMovement : MonoBehaviour
{
    public enum DriveDirection
    {
        Front, Left, Right
    }
    public enum Axel
    {
        Front,
        Rear
    }

    [Serializable]
    public struct Wheel
    {
        public GameObject wheelModel;
        public WheelCollider wheelCollider;
        public Axel axel;
    }

    [Header("Car Parameter")]
    [SerializeField]
    bool playerController = false;

    [SerializeField]
    float maxAcceleration = 30.0f;
    [SerializeField]
    float accelerationSpeed = 5.0f;
    [SerializeField] 
    float brakeAcceleration = 50.0f;

    [SerializeField]
    float turnSensitivity = 1.0f;
    [SerializeField] 
    float maxSteerAngle = 30.0f;

    [Header("CollisionSensors")]
    [SerializeField]
    float checkFrontCar = 3.0f;
    [SerializeField]
    float distanceFrontSensor = 0.35f;

    [SerializeField]
    Vector3 _centerOfMass;
    [Header("Target")]
    [SerializeField]
    Vector3 targetPosition;

    [Header("Car Wheels")]
    [SerializeField]
    public List<Wheel> wheels;


    float moveInput;
    float steerInput;

    private Rigidbody carRb;

    private Vector3 previousTarget = Vector3.zero;

    private float speedLimit = 30;
    private float speedValue = 0;

    DriveDirection direction = DriveDirection.Front;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerOfMass;

        speedValue = speedLimit / maxAcceleration;
    }
    
    public void setSpeedLimit(float speedLimit)
    {
        this.speedLimit = speedLimit;


        speedValue = speedLimit / maxAcceleration;
    }

    void Update()
    {
        GetInputs();
        AnimateWheels();
    }

    void LateUpdate()
    {
        Move();
        Steer();
        Brake();
    }

    public Vector3 getTarget()
    {
        return targetPosition;
    }
    public void setTarget(List<Vector3> pos, List<Vector3> endLane, bool endPoint)//Chek if endPoint to check if movement left rotation
    {
        previousTarget = targetPosition;
        int rng = Random.Range(0, pos.Count);
        targetPosition = pos[rng];

        if(endPoint)
        {
            Vector3 targetDirection = (endLane[rng] - transform.position).normalized;
            float angle = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);

            Debug.Log("Start: " + previousTarget + "End: " + targetPosition);

            Debug.Log(angle);
            if (angle > 10)
            {
                direction = DriveDirection.Right;
            }
            else if (angle < 10)
            {
                direction = DriveDirection.Left;
            }
            else
            {
                direction = DriveDirection.Front;
            }
        }

    }
    public void setTarget(Vector3 pos)
    {
        previousTarget = targetPosition;
        targetPosition = pos;
    }

    public void MoveInput(float input)
    {
        moveInput = input;
    }

    public void SteerInput(float input)
    {
        steerInput = input;
    }

    void GetInputs()
    {
        if(playerController)
        {
            moveInput = Input.GetAxis("Vertical");
            steerInput = Input.GetAxis("Horizontal");
        }
        else
        {
            //----------ROTATION----------------------
            Vector3 lineDirection = targetPosition - previousTarget;
            Vector3 pointToLinePoint1 = transform.position - previousTarget;
            float projection = Vector3.Dot(pointToLinePoint1, lineDirection.normalized);
            Vector3 projectedPoint = previousTarget + projection * lineDirection.normalized;
            float distance = Vector3.Distance(transform.position, projectedPoint);
            //Debug.Log(distance);
            if(distance < 0.4)
            {
                steerInput = 0;
            }
            else
            {
                Vector3 targetDirection = (targetPosition - transform.position).normalized;

                float angle = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);
                angle = Mathf.Clamp(angle, -maxSteerAngle, maxSteerAngle);

                float steerObjective = angle / maxSteerAngle;

                steerInput = steerObjective;
            }

            //-------SPEED------------

            RaycastHit hitR;
            RaycastHit hitL;
            if (Physics.Raycast(transform.position + transform.right * distanceFrontSensor, transform.forward * checkFrontCar, out hitR, checkFrontCar) ||
                Physics.Raycast(transform.position + -transform.right * distanceFrontSensor, transform.forward * checkFrontCar, out hitL, checkFrontCar))
            {
                moveInput = 0;
            }
            else
            {
                moveInput = speedValue;
            };
        }
        
    }

    void Move()
    {
        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.motorTorque = moveInput * 600 * maxAcceleration * Time.deltaTime;
        }
    }

    void Steer()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.axel == Axel.Front)
            {
                var _steerAngle = steerInput * turnSensitivity * maxSteerAngle;
                wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, _steerAngle, 0.3f);
            }
        }
    }

    void Brake()
    {
        if (moveInput == 0)
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.brakeTorque = 300 * brakeAcceleration * Time.deltaTime;
            }
        }
        else
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.brakeTorque = 0;
            }
        }
    }

    void AnimateWheels()
    {
        foreach (var wheel in wheels)
        {
            Quaternion rot;
            Vector3 pos;
            wheel.wheelCollider.GetWorldPose(out pos, out rot);
            wheel.wheelModel.transform.position = pos;
            wheel.wheelModel.transform.rotation = rot;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.blue;
        Gizmos.DrawLine(transform.position + transform.right * distanceFrontSensor, transform.position + transform.right * distanceFrontSensor + transform.forward * checkFrontCar);
        Gizmos.DrawLine(transform.position + -transform.right * distanceFrontSensor, transform.position + -transform.right * distanceFrontSensor + transform.forward * checkFrontCar);
    }
}
