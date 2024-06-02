using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Net.Http.Headers;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;
using static Point;
using static UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class CarMovement : MonoBehaviour
{
    public enum DriveDirection
    {
        Front, Left, Right
    }
    public enum DrivingLane
    {
        OneLane, Left, Right
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

    [SerializeField]
    Vector3 _centerOfMass;

    [Header("Car Wheels")]
    [SerializeField]
    List<Wheel> wheels;

    float moveInput;
    float steerInput;

    private bool waitingToGo = false;

    private Rigidbody carRb;

    IMovable carlogic;

    void Start()
    {
        carlogic = GetComponent<IMovable>();

        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerOfMass;

    }

    public float GetMaxSteer()
    {
        return maxSteerAngle;
    }
    public float GetMaxAcceleration()
    {
        return maxAcceleration;
    }

    public void SetCarStopped(bool boolean)
    {
        waitingToGo = boolean;
    }

    public bool isCarStopped()
    {
        return waitingToGo;
    }

    void Update()
    {
        GetInputs();
        AnimateWheels();

        Movement();
    }


    void Movement()
    {
        Move();
        Steer();
        Brake();
    }

    public void StopCar()
    {
        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.motorTorque = 0; 
        }

        foreach (var wheel in wheels)
        {
            if (wheel.axel == Axel.Front)
            {
                wheel.wheelCollider.steerAngle = 0;
            }
        }
        AnimateWheels();
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
            carlogic.CalculateCarInput();

            moveInput = carlogic.GetMove();
            steerInput = carlogic.GetSteer();

        }

    }

    void Move()
    {
        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.motorTorque = moveInput * 600 * maxAcceleration * Time.deltaTime; //DELTA TIME BREAK IT
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


}
