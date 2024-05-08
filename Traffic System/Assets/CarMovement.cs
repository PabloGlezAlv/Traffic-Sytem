using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class CarMovement : MonoBehaviour
{
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


    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerOfMass;
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
    public void setTarget(List<Vector3> pos)
    {
        previousTarget = targetPosition;
        targetPosition = pos[Random.Range(0, pos.Count)];
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
            Debug.Log(distance);
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
            moveInput = 1;
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
        if (Input.GetKey(KeyCode.Space) || moveInput == 0)
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
}
