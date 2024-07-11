using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngineInternal;
using static CarMovement;
using static Point;
using static UnityEngine.GraphicsBuffer;

public class CarLogicAI : Agent, IMovable
{
    [SerializeField]
    WheelCollider wheel;
    [SerializeField]
    RayPerceptionSensorComponent3D normalSensor;
    [SerializeField]
    RayPerceptionSensorComponent3D topSensor;
    [SerializeField]
    float trainingReward = 0;
    [Header("Input")]
    [SerializeField]    //VARIABLES TO DEBUG THE INPUT
    Vector3 myPosition;
    [SerializeField]
    Vector3 targetPosition;
    [SerializeField]
    float myRotation;
    [SerializeField]
    float mySpeed;

    [Header("Output")]
    [SerializeField]
    float moveInput = 0;
    [SerializeField]
    float steerInput = 0;

    [Header("CollisionSensors")]
    [SerializeField]
    float checkFrontCar = 3.0f;
    [SerializeField]
    float distanceFrontSensor = 0.35f;
    [SerializeField]
    float checkSidesCar = 3.0f;

    CarMovement carMov;


    private float driverSpeed = 0;


    bool otherRight = false;

    private Vector3 previousTarget = Vector3.zero;

    private float speedLimit = 70;
    private float speedValue = 0;

    [Header("Debug Parameters")]
    [SerializeField]
    DriveDirection direction = DriveDirection.Front;
    [SerializeField]
    DrivingLane carLane = DrivingLane.OneLane;
    [SerializeField]
    float currentSpeed = 0;
    [SerializeField]
    int checkpointID = 0;
    [SerializeField]
    bool inConection = false;
    GameObject conectionParent;

    bool safeRouteChange = false;


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
    bool startSide;

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

    float maxAcceleration = 0;
    float maxSteerAngle = 0;

    Vector3 startPosition;
    Quaternion startRotation;
    Vector3 startGoal;

    bool wallColliding = false;

    Rigidbody rb;

    int deleteConectionWalls = 0;
    GameObject parentConection;

    Vector3 finalLinePoint;

    float lastCheckPoint = 0;

    bool otherCarStoped = false;

    public Vector3 getFinalPoint()
    {
        return finalLinePoint;
    }

    public int CheckpointID
    {
        get { return checkpointID; }
        set { checkpointID = value; }
    }
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startGoal = targetPosition;
        carMov = gameObject.GetComponent<CarMovement>();

        maxAcceleration = carMov.GetMaxAcceleration();
        maxSteerAngle = carMov.GetMaxSteer();

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Start()
    {
        carMov.StopCar();

        //checkRayCast();

        speedValue = speedLimit / maxAcceleration;

        driverSpeed = Random.Range(0.7f, 1);

        startSide = rightSide;
    }

    private void RestartCar()
    {
        rightSide = startSide;
        lastCheckPoint = 0;
        //Raycast
        hitFR = false;
        hitFL = false;
        hitSideBR = false;
        hitSideFR = false;
        hitSideBL = false;
        hitSideFL = false;
        //Overtake
        bool changeLane = false;
        int overtaking = -1;

        rb.position = startPosition;
        targetPosition = startGoal;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.rotation = startRotation;

        deleteConectionWalls = 0;
        checkpointID = 0;

        if(inConection)
        {
            ConectionData data= conectionParent.GetComponent<ConectionData>();
            data.AddCar(-1);
            if (data.GetDependantCars() <= 0)
                conectionParent.SetActive(false);
        }

        speedLimit = 50;
        speedValue = speedLimit / maxAcceleration;

        checkRayCast();

        gameObject.layer = LayerMask.NameToLayer("Car");
    }

    // ---------------------------------AI PARAMETERS-----------------------------
    public override void OnEpisodeBegin()
    {
        RestartCar();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (waitingToGo) return;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = (int)Input.GetAxis("Vertical");
        discreteActions[1] = (int)Input.GetAxis("Horizontal");

        if (discreteActions[0] == -1)
        {
            discreteActions[0] = 2;
        }
        if (discreteActions[1] == -1)
        {
            discreteActions[1] = 2;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Own car
        sensor.AddObservation(mySpeed);
        sensor.AddObservation(myPosition);
        sensor.AddObservation(myRotation);

        //target Position
        sensor.AddObservation(targetPosition);

        //Debug.Log("Speed: " + rb.velocity.magnitude);
        //Debug.Log("Pos: " + transform.position);
        //Debug.Log("Rot: " + GetNormalizedValue(transform.eulerAngles.y, 0, 360));
        //Debug.Log("GoalPos: " + goal.transform.position);
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (waitingToGo)
        {
            moveInput = 0;
            steerInput = 0;
            return;
        }


            moveInput = actions.DiscreteActions[0];
        switch (moveInput)
        {
            case 0: moveInput = 0; break;
            case 1: moveInput = speedValue * driverSpeed; break;
            case 2: moveInput = -speedValue * driverSpeed; break;
            case 3: moveInput = speedValue * driverSpeed * 0.66f; break;
            case 4: moveInput = -speedValue * driverSpeed * 0.66f; break;
            case 5: moveInput = speedValue * driverSpeed * 0.33f; break;
            case 6: moveInput = -speedValue * driverSpeed * 0.33f; break;
            default: break;
        }
        //---------------------------------------------
        steerInput = actions.DiscreteActions[1];
        switch (steerInput)
        {
            case 0: steerInput = 0; break;
            case 1: steerInput = 1; break;
            case 2: steerInput = -1; break;
            case 3: steerInput = 0.75f; break;
            case 4: steerInput = -0.75f; break;
            case 5: steerInput = 0.5f; break;
            case 6: steerInput = -0.5f; break;
            case 7: steerInput = 0.25f; break;
            case 8: steerInput = -0.25f; break;
            default: break;
        }


        if (waitingToGo) return;
        AddReward(-1.0f / MaxStep);
    }

    public void AddRewardAgent(float amount, bool kill = false)
    {
        
        AddReward(amount);

        if (kill)
        {
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        CarLogicAI car = collision.gameObject.GetComponentInParent<CarLogicAI>();

        if (car) //Check if collide with a car
        {
            // Check whose front is closer to the impact fisrt case my fault
            if (Vector3.Distance(transform.position + forward, collision.contacts[0].point) <
                Vector3.Distance(collision.transform.position + collision.transform.forward, collision.contacts[0].point))
            {
                car.AddRewardAgent(-1f, true);
            }
            else
            {
                car.AddRewardAgent(-0.25f, true);
            }
        }
    }

    // ---------------------------------------------------------------------------------

    public float GetMove()
    {
        return moveInput;
    }

    public float GetSteer()
    {
        return steerInput;
    }
    public bool GetRight()
    {
        return rightSide;
    }
    public Vector3 getTarget()
    {
        return targetPosition;
    }

    public void CalculateCarInput()
    {
        //Empty the AI Calculate the input now
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("TrafficBarrier"))
        {
            waitingToGo = true;
            moveInput = 0;
            carMov.SetCarStopped(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("TrafficBarrier"))
        {
            waitingToGo = false;
            moveInput = 0;
            carMov.SetCarStopped(false);
        }
    }

    private void DirectionsRaycast()
    {
        //FrontSensors
        hitFR = Physics.Raycast(transform.position + transform.right * distanceFrontSensor, forward * checkFrontCar, out hitR, checkFrontCar * frontRangeValue);
        hitFL = Physics.Raycast(transform.position - transform.right * distanceFrontSensor, forward * checkFrontCar, out hitL, checkFrontCar * frontRangeValue);


        if (hitFR || hitFL) //Check collision
        {
            if ((hitFR && hitR.transform.tag == "Car" && hitR.transform.gameObject.GetComponent<CarMovement>() == rightSide) || (hitFL && hitL.transform.tag == "Car" && hitL.transform.gameObject.GetComponent<CarMovement>() == rightSide)) //Collision other car
            {
                if ((!waitingToGo && hitFR && hitR.transform.tag == "Car"))
                {
                    if (hitR.transform.GetComponent<CarMovement>().isCarStopped()) //Front car but stop bcs traffic system
                    {
                        waitingToGo = true;
                        moveInput = 0f;
                        carMov.SetCarStopped(true);
                        timerToGo = 0;
                        otherCarStoped = true;

                        if (gameObject.name == "Car 2")
                            Debug.Log("Car in Traffic light");
                    }
                    if (lastCheckLine == PointType.Start && !changeLane && carLane != DrivingLane.OneLane && overtaking == -1)
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
                        moveInput = 0f;
                        carMov.SetCarStopped(true);
                        timerToGo = 0;
                        otherCarStoped= true;

                        if (gameObject.name == "Car 2")
                            Debug.Log("Car in Traffic light");
                    }
                    else
                    {
                        waitingToGo = false;
                        moveInput = 0f;
                        carMov.SetCarStopped(false);
                    }

                    if (lastCheckLine == PointType.Start && !changeLane && carLane != DrivingLane.OneLane && overtaking == -1)
                    {
                        changeLane = true;
                        if (gameObject.name == "Car 2")
                            Debug.Log("Car");
                    }
                }
            }
        }
        else if(otherCarStoped)
        {
            otherCarStoped = false;
            waitingToGo = false;
            moveInput = 0;
            carMov.SetCarStopped(false);
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

    public void setLane(DrivingLane lane)
    {
        carLane = lane;
    }

    public void setSpeedLimit(float speedLimit)
    {
        this.speedLimit = speedLimit;


        speedValue = speedLimit / maxAcceleration;
    }
    private void checkRayCast()
    {
        DirectionsRaycast();

        Invoke("checkRayCast", 0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        trainingReward = GetCumulativeReward();
        mySpeed = rb.velocity.magnitude;
        myRotation = transform.eulerAngles.y;
        myPosition = transform.position;


        if (!waitingToGo)
        {
            lastCheckPoint += Time.deltaTime;
            if (lastCheckPoint > 15)
            {

                AddRewardAgent(-0.01f, true);
            }
        }

        forward = new Vector3(transform.forward.x, 0, transform.forward.z);
    }
    public void setTarget(List<Vector3> pos, List<Vector3> endLane, List<DrivingLane> lanes, PointType type, bool right, List<GameObject> parentDirs, bool lastPoint)//Chek if endPoint to check if movement left rotation
    {
        lastCheckPoint = 0;
        previousTarget = targetPosition;
        int rng;
        rng = Random.Range(0, pos.Count);

        //Change triggers for sensor
        if (rightSide != right)
        {
            int laneRightLayer = LayerMask.NameToLayer("LaneRigth");
            int laneLeftLayer = LayerMask.NameToLayer("LaneLeft");

            if (right)
            {
                //Add Left
                normalSensor.RayLayerMask |= (1 << laneLeftLayer);

                //Remove Right
                normalSensor.RayLayerMask &= ~(1 << laneRightLayer);
            }
            else
            {
                //Add Rigth
                normalSensor.RayLayerMask |= (1 << laneRightLayer);

                //Remove Left
                normalSensor.RayLayerMask &= ~(1 << laneLeftLayer);
            }
        }

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

        if (type == PointType.End)
        {
            finalLinePoint = endLane[rng];

            Vector3 forward = (previousTarget - targetPosition).normalized;
            Vector2 startPos = new Vector2(targetPosition.x, targetPosition.z);
            Vector2 endPos = new Vector2(endLane[rng].x, endLane[rng].z);
            Vector2 dir = endPos - startPos;

            float crossProduct = forward.x * dir.y - forward.z * dir.x;

            if (crossProduct > 0.03f)
            {
                int laneRightLayer = LayerMask.NameToLayer("ConectionRight");
                normalSensor.RayLayerMask |= (1 << laneRightLayer);

                gameObject.layer = LayerMask.NameToLayer("CarConectionRight");
            }
            else if (crossProduct < -0.03f)
            {
                int laneLeftLayer = LayerMask.NameToLayer("ConectionLeft");
                normalSensor.RayLayerMask |= (1 << laneLeftLayer);

                gameObject.layer = LayerMask.NameToLayer("CarConectionLeft");
            }
            else
            {
                int laneMidLayer = LayerMask.NameToLayer("ConectionFront");
                normalSensor.RayLayerMask |= (1 << laneMidLayer);

                gameObject.layer = LayerMask.NameToLayer("CarConectionFront");
            }
        }
        else if (type == PointType.Start)
        {
            int laneRightLayer = LayerMask.NameToLayer("ConectionRight");
            int laneMidLayer = LayerMask.NameToLayer("ConectionFront");
            int laneLeftLayer = LayerMask.NameToLayer("ConectionLeft");

            normalSensor.RayLayerMask &= ~(1 << laneRightLayer);
            normalSensor.RayLayerMask &= ~(1 << laneMidLayer);
            normalSensor.RayLayerMask &= ~(1 << laneLeftLayer);

            gameObject.layer = LayerMask.NameToLayer("Car");
        }
            //Activate or deactivate conection objects
            if (deleteConectionWalls > 0)
            {
                deleteConectionWalls++;
                if (deleteConectionWalls > 2)
                {
                    ConectionData data = parentConection.GetComponent<ConectionData>();

                    data.AddCar(-1);
                    if (data.GetDependantCars() <= 0)
                        parentConection.SetActive(false);

                    inConection = false;
                    deleteConectionWalls = 0;
                }
            }
            else if (parentDirs.Count != 0)
            {
                if (!lastPoint)
                {
                    parentDirs[rng].GetComponent<ConectionData>().AddCar(1);
                    parentDirs[rng].SetActive(true);

                    inConection = true;

                    conectionParent = parentDirs[rng];
                }
                else //We do not delete in last point but in first of next conection
                {
                    deleteConectionWalls = 1;

                    parentConection = parentDirs[rng];
                }
            }


            if (lanes.Count > 0) //This means next point is a conection
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

                if (Vector3.Distance(transform.position, endLane[rng]) < 7)
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
        startGoal = pos;
        previousTarget = targetPosition;
        targetPosition = pos;
    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = UnityEngine.Color.blue;


        ////Front Sensor
        Gizmos.DrawLine(transform.position + transform.right * distanceFrontSensor, transform.position + transform.right * distanceFrontSensor + forward * checkFrontCar * frontRangeValue);
        Gizmos.DrawLine(transform.position + -transform.right * distanceFrontSensor, transform.position + -transform.right * distanceFrontSensor + forward * checkFrontCar * frontRangeValue);

        ////Right Sensor
        //Gizmos.DrawLine(transform.position + transform.right * distanceFrontSensor, transform.position + transform.right * distanceFrontSensor + transform.right * checkSidesCar + forward * checkSidesCar / 2);
        //Gizmos.DrawLine(transform.position + transform.right * distanceFrontSensor + forward * 1.3f, transform.position + transform.right * distanceFrontSensor + transform.right * checkSidesCar + forward * checkSidesCar / 2 + forward * 1.3f);

        ////Left Sensor
        //Gizmos.DrawLine(transform.position - transform.right * distanceFrontSensor, transform.position - transform.right * distanceFrontSensor - transform.right * checkSidesCar + forward * checkSidesCar / 2);
        //Gizmos.DrawLine(transform.position - transform.right * distanceFrontSensor + forward * 1.3f, transform.position - transform.right * distanceFrontSensor - transform.right * checkSidesCar + forward * checkSidesCar / 2 + forward * 1.3f);

        //Gizmos.color = UnityEngine.Color.magenta;
        ////Right Sensor Overtake
        //Gizmos.DrawLine(transform.position + transform.right * 2.5f + forward * 6, transform.position + transform.right * 2.4f - forward * 11);
        ////Left Sensor Overtake
        //Gizmos.DrawLine(transform.position - transform.right * 2.5f + forward * 6, transform.position - transform.right * 2.4f - forward * 11);

        //Gizmos.color = UnityEngine.Color.white;
        Gizmos.DrawLine(transform.position, targetPosition);
    }
}
