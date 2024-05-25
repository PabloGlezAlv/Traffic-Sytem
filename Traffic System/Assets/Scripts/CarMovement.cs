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

    [Header("CollisionSensors")]
    [SerializeField]
    float checkFrontCar = 3.0f;
    [SerializeField]
    float distanceFrontSensor = 0.35f;
    [SerializeField]
    float checkSidesCar = 3.0f;

    [SerializeField]
    Vector3 _centerOfMass;
    [Header("Target")]
    [SerializeField]
    Vector3 targetPosition;

    [Header("Car Wheels")]
    [SerializeField]
    public List<Wheel> wheels;


    private float driverSpeed = 0;

    float moveInput;
    float steerInput;

    private Rigidbody carRb;

    private Vector3 previousTarget = Vector3.zero;

    private float speedLimit = 30;
    private float speedValue = 0;

    [Header("Debug Parameters")]
    [SerializeField]
    DriveDirection direction = DriveDirection.Front;
    [SerializeField]
    DrivingLane carLane = DrivingLane.OneLane;
    [SerializeField]
    float currentSpeed = 0;

    bool safeRouteChange = false;

    private Vector3 previousPosition;

    private float frontRangeValue = 1;

    [SerializeField]
    private bool changeLane = false;

    private int overtaking = -1;

    private PointType lastCheckLine = PointType.Start; // Used to know if Car in rect or changing intersection

    //Variables to not overtake when car stop by traffic light
    [SerializeField]
    private bool waitingToGo = false;
    private float timerToGo = -1;

    Vector3 forward = new Vector3();

    bool rightSide = false;
    bool otherRight = false;

    RaycastHit hitR; //Front Right
    bool hitFR = false;
    RaycastHit hitL; //Front Left
    bool hitFL = false;
    RaycastHit hitSBR; //Side sensor in the back right
    bool hitSideBR = false;
    RaycastHit hitSFR; //Side sensor in the front right
    bool hitSideFR = false;
    RaycastHit hitSBL; //Side sensor in the back left
    bool hitSideBL = false;
    RaycastHit hitSFL; //Side sensor in the front left
    bool hitSideFL = false;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerOfMass;

        speedValue = speedLimit / maxAcceleration;

        driverSpeed = Random.Range(0.7f, 1);

        checkRayCast();


        Invoke("CalculateCurrentSpeed", 0.1f);
    }

    public bool GetRight()
    {
        return rightSide;
    }

    private void OnValidate()
    {
        forward = new Vector3(transform.forward.x, transform.forward.y, transform.forward.z);
    }
    public void setLane(DrivingLane lane)
    {
        carLane = lane;
    }

    public bool isCarStopped()
    {
        return waitingToGo;
    }

    public void setSpeedLimit(float speedLimit)
    {
        this.speedLimit = speedLimit;


        speedValue = speedLimit / maxAcceleration;

        frontRangeValue = speedLimit / 40;
    }

    void Update()
    {
        forward = new Vector3(transform.forward.x, 0, transform.forward.z);

        if (timerToGo >= 0)
        {
            timerToGo += Time.deltaTime;

            if (timerToGo > 15)
            {
                timerToGo = -1;
                waitingToGo = false;
            }
        }

        GetInputs();
        AnimateWheels();

        Movement();
    }

    private void CalculateCurrentSpeed()
    {
        Vector3 currentPosition = transform.position;

        // Calcular la distancia recorrida desde la �ltima actualizaci�n
        float distance = Vector3.Distance(previousPosition, currentPosition);

        // Calcular la velocidad (distancia recorrida por unidad de tiempo)
        currentSpeed = distance / 0.1f;

        // Actualizar la posici�n anterior
        previousPosition = currentPosition;

        Invoke("CalculateCurrentSpeed", 0.1f);
    }

    private void checkRayCast()
    {
        DirectionsRaycast();

        Invoke("checkRayCast", 0.1f);
    }

    private void DirectionsRaycast()
    {
        //FrontSensors
        hitFR = Physics.Raycast(transform.position + transform.right * distanceFrontSensor, forward * checkFrontCar, out hitR, checkFrontCar * frontRangeValue);
        hitFL = Physics.Raycast(transform.position - transform.right * distanceFrontSensor, forward * checkFrontCar, out hitL, checkFrontCar * frontRangeValue);


        if(hitFR || hitFL) //Check collision
        {
            if ((hitFR && hitR.transform.tag == "TrafficBarrier") || (hitFL && hitL.transform.tag == "TrafficBarrier")) //Traffic system stop
            {
                waitingToGo = true;
                timerToGo = 0;
            }
            else if((hitFR && hitR.transform.tag == "Car" && hitR.transform.gameObject.GetComponent<CarMovement>() == rightSide) || (hitFL && hitL.transform.tag == "Car" && hitL.transform.gameObject.GetComponent<CarMovement>() == rightSide)) //Collision other car
            {
                if ((!waitingToGo && hitFR && hitR.transform.tag == "Car"))
                {
                    if (hitR.transform.GetComponent<CarMovement>().isCarStopped()) //Front car but stop bcs traffic system
                    {
                        waitingToGo = true;
                        timerToGo = 0;

                        if (gameObject.name == "Car 2")
                            Debug.Log("Car in Traffic light");
                    }
                    else if (lastCheckLine == PointType.Start && !changeLane && carLane != DrivingLane.OneLane && overtaking == -1)
                    {
                        changeLane = true;
                        if (gameObject.name == "Car 2")
                            Debug.Log("Car");
                    }
                }
                else if ((!waitingToGo && hitFL && hitL.transform.tag == "Car"))
                {
                    if (hitL.transform.GetComponent<CarMovement>().isCarStopped()) //Front car but stop bcs traffic system
                    {
                        waitingToGo = true;
                        timerToGo = 0;
                        if (gameObject.name == "Car 2")
                            Debug.Log("Car in Traffic light");
                    }
                    else if (lastCheckLine == PointType.Start && !changeLane && carLane != DrivingLane.OneLane && overtaking == -1)
                    {
                        changeLane = true;
                        if (gameObject.name == "Car 2")
                            Debug.Log("Car");
                    }
                }
            }
        }

        switch (direction)
        {
            case DriveDirection.Left:
                hitSideBL = Physics.Raycast(transform.position - transform.right * distanceFrontSensor, -transform.right * checkSidesCar + transform.forward * checkSidesCar / 2, out hitSBL, checkSidesCar);
                hitSideFL = Physics.Raycast(transform.position - transform.right * distanceFrontSensor + forward * 1.3f, -transform.right * checkSidesCar + forward * checkSidesCar / 2, out hitSFL, checkSidesCar);
                break;

            case DriveDirection.Right: //Leave front sensor just in case another close car
                hitSideBR = Physics.Raycast(transform.position + transform.right * distanceFrontSensor, transform.right * checkSidesCar + transform.forward * checkSidesCar / 2, out hitSBR, checkSidesCar);
                hitSideFR = Physics.Raycast(transform.position + transform.right * distanceFrontSensor + forward * 1.3f, transform.right * checkSidesCar + forward * checkSidesCar / 2, out hitSFR, checkSidesCar);
                break;
            case DriveDirection.Front:
                
                break;
            default:
                break;
        }
    }

    void Movement()
    {
        Move();
        Steer();
        Brake();

        RaycastHit hit;
    }

    public Vector3 getTarget()
    {
        return targetPosition;
    }
    public void setTarget(List<Vector3> pos, List<Vector3> endLane, List<DrivingLane> lanes,PointType type, bool right)//Chek if endPoint to check if movement left rotation
    {
        previousTarget = targetPosition;
        int rng; 
        rng = Random.Range(0, pos.Count);

        rightSide = right;


        RaycastHit hit;
        if (type == PointType.Mid)
        {
            bool checkRight = Physics.Raycast(transform.position - transform.right * 2.5f + forward * 6, -forward, out hit, 17);
            bool checkLeft = Physics.Raycast(transform.position + transform.right * 2.5f + forward * 6, -forward, out hit, 17);

            if (changeLane && ((carLane == DrivingLane.Right && !checkRight) || (carLane == DrivingLane.Left && !checkLeft))) //If want to overtake check if car behind
            {
                rng = pos.Count - 1;
                changeLane = false;
                overtaking = 0;
            }
            else if (overtaking >= 0) //Overtaking add values
            {
                overtaking++;
                rng = 0;

                //Debug.Log("Check point overtake: " + overtaking);
                if (overtaking > 1 && (carLane == DrivingLane.Left && !checkLeft) || (carLane == DrivingLane.Right && !checkRight)) //Overtake done return
                {
                    //Debug.Log("Change lane End: " + carLane);
                    rng = pos.Count - 1;
                    overtaking = -1;
                    changeLane = false;
                }
            }
            else
            {
                rng = 0;
            }
        }
        else
        {
            lastCheckLine = type;
        }

        //Debug.Log(gameObject.name + " moving to: " + rng + " " + pos[rng]);

        //Set car parameters
        targetPosition = pos[rng];

        if(lanes.Count > 0) //This means next point is a conection
        {
            carLane = lanes[rng];
        }

        if (type == PointType.End) //Check direction to turn
        {
            safeRouteChange = pos.Count == 1; //Only one route, its safe

            Vector3 targetDirection = (endLane[rng] - transform.position).normalized;
            float angle = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);

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

            if(Vector3.Distance(transform.position, endLane[rng]) < 7)
            {
                speedValue /= 2;
            }
        }
        else if (type == PointType.Start)
        {
            safeRouteChange = false;

            direction = DriveDirection.Front;
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

                float angle = Vector3.SignedAngle(forward, targetDirection, Vector3.up);
                angle = Mathf.Clamp(angle, -maxSteerAngle, maxSteerAngle);

                float steerObjective = angle / maxSteerAngle;

                steerInput = steerObjective;
            }

            //-------SPEED------------


            if (safeRouteChange) // If is just a  curve no problem of other cars
            {
                if(hitFR || hitFL)
                {
                    moveInput = 0;
                }
                else
                {
                    moveInput = speedValue * driverSpeed;
                }
            }
            else
            {
                switch (direction)
                {
                    case DriveDirection.Left:
                        if (hitFR || hitFL || hitSideBL || hitSideFL)
                        {
                            moveInput = 0;
                        }
                        else
                        {
                            moveInput = speedValue * driverSpeed;
                        }
                        break;

                    case DriveDirection.Right: //Leave front sensor just in case another close car
                        if (hitFR || hitFL || hitSideBR || hitSideFR)
                        {
                            moveInput = 0;
                        }
                        else
                        {
                            moveInput = speedValue * driverSpeed;
                        }
                        break;
                    case DriveDirection.Front:
                        if (hitFR || hitFL)
                        {
                            moveInput = 0;
                        }
                        else
                        {
                            moveInput = speedValue * driverSpeed;
                        }
                        break;
                    default:
                        break;
                }
            }
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

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.blue;
        

        //Front Sensor
        Gizmos.DrawLine(transform.position + transform.right * distanceFrontSensor, transform.position + transform.right * distanceFrontSensor + forward * checkFrontCar * frontRangeValue);
        Gizmos.DrawLine(transform.position + -transform.right * distanceFrontSensor, transform.position + -transform.right * distanceFrontSensor + forward * checkFrontCar * frontRangeValue);

        //Right Sensor
        Gizmos.DrawLine(transform.position + transform.right * distanceFrontSensor, transform.position + transform.right * distanceFrontSensor + transform.right * checkSidesCar + forward * checkSidesCar / 2);
        Gizmos.DrawLine(transform.position + transform.right * distanceFrontSensor + forward * 1.3f, transform.position + transform.right * distanceFrontSensor + transform.right * checkSidesCar + forward * checkSidesCar / 2 + forward * 1.3f);

        //Left Sensor
        Gizmos.DrawLine(transform.position - transform.right * distanceFrontSensor, transform.position - transform.right * distanceFrontSensor - transform.right * checkSidesCar + forward * checkSidesCar / 2);
        Gizmos.DrawLine(transform.position - transform.right * distanceFrontSensor + forward * 1.3f, transform.position - transform.right * distanceFrontSensor - transform.right * checkSidesCar + forward * checkSidesCar / 2 + forward * 1.3f);

        Gizmos.color = UnityEngine.Color.magenta;
        //Right Sensor Overtake
        Gizmos.DrawLine(transform.position + transform.right * 2.5f + forward * 6, transform.position + transform.right * 2.4f - forward * 11);
        //Left Sensor Overtake
        Gizmos.DrawLine(transform.position - transform.right * 2.5f + forward * 6, transform.position - transform.right * 2.4f - forward * 11);

    }
}
