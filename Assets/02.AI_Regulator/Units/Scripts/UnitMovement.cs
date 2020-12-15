using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

/* Unit Movement created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(Unit))]
    public class UnitMovement : MonoBehaviour
    {
        private Unit unit; //the main unit's component

        [SerializeField]
        private bool canMove = true; //can this unit move when ordered by the player?
        public bool CanMove() { return canMove; }

        [SerializeField]
        private bool canFly = false; //when true, the unit will be able to fly over the terrain/map.
        public bool CanFly() { return canFly; }

        private bool isMoving = false; //Is the player currently moving?
        public bool IsMoving() { return isMoving || pendingMovement; }

        private bool pendingMovement = false; //when the unit is awaiting for the movement manager to assign its movement
        private MovementManager.MovementTask pendingMovementTask; //in case pending movement is enabled, this stores the pending movement task

        //the next two fields allow to avoid having a unit stuck.
        private float mvtCheckTimer; //timer to check whether the unit is moving towards its current target or not.
        private Vector3 lastPosition; //saves the last player's position to compare it later and see if the unit has actually moved.

        private NavMeshAgent navAgent; //Navigation Agent component attached to the unit's object.

        public LayerMask GetAgentAreaMask() { return navAgent.areaMask; } //return the area mask set in the nav agent component, this presents on which areas the unit can and can't move.
        public float GetAgentRadius() { return navAgent.radius; } //get the navigation agent radius which presents the size that the unit is supposed to occupy on the navmesh

        private NavMeshPath navPath; //we'll be using the navigation agent to compute the path and store it here then move the unit manually
        private Queue<Vector3> navPathCornerQueue; //after computing a valid and complete path, the path's corners will be added to this queue.

        private Vector3 finalDestination; //the target destination the unit is moving towards.
        private float stoppingDistance; //the current stopping distance of the unit

        private Vector3 currentDestination; //the current corner that the unit is moving towards in the computed path
        private Vector3 currentDirection; //the current direction of the unit when moving from one corner to another in the path.
        private IEnumerator heightCheckCoroutine; //so that we don't sample the height every frame (since it's very expensive), we do it a couple of times per second in a coroutine

        public bool DestinationReached { set; get; } //when the target his destination, this is set to true.

        private NavMeshObstacle navObstacle; //Navigation Obstacle component that's attached to the unit's object (in case avoidance is enabled).

        //Speed:
        [SerializeField]
        private float speed = 10.0f; //The unit's movement speed.
        private float maxSpeed; //the unit's current max speed.
        public float CurrentSpeed { private set; get; } //the current unit's speed when it's moving
        public float GetMaxSpeed() { return maxSpeed; }
        public void SetMaxSpeed(float newSpeed) { maxSpeed = newSpeed; }

        //how fast will the unit reach the max speed
        [SerializeField]
        private float acceleration = 10.0f;

        //Movement targets:
        private APC targetAPC; //if the unit is moving towards a APC, it will be held here.
        private Portal targetPortal; //if the unit is moving towards a portal, it will be held here.

        //Rotation:
        [SerializeField]
        private bool canMoveRotate = true; //can the unit rotate and move at the same time? 
        bool facingNextDestination = false; //when the unit faces its next target for the first time and is ready to move towards, this is enabled until the next destination in the path is assigned
        [SerializeField]
        private float minMoveAngle = 40.0f; //the close this value to 0.0f, the closer must the unit face its next destination in its path to move

        [SerializeField]
        private bool canIdleRotate = true; //can the unit rotate when not moving?
        [SerializeField]
        private float rotationDamping = 2.0f; //How fast does the rotation update?
        private Quaternion rotationTarget; //What is the unit currently looking at?
        private Vector3 lookAtTarget; //Where should the unit look at as soon as it stops moving?
        private Transform lookAtTransform; //the object that this unit should be look at when not moving, if it exists.

        //Target Position Collider:
        [SerializeField]
        private Collider targetPositionCollider = null; //a collider component that represents the position that the unit occupies when idle/will occupy in the future when moving.
        //the following are the methods that can access the target positions collider attributes:
        public void DestroyTargetPositionCollider () { Destroy(targetPositionCollider.gameObject); }
        public void TriggerTargetPositionCollider (bool enable)
        {
            targetPositionCollider.enabled = enable;
        }
        public void UpdateTargetPositionCollider (Vector3 newPosition) { targetPositionCollider.transform.position = newPosition; }

        //Audio:
        [SerializeField]
        private AudioClip mvtOrderAudio = null; //Audio played when the unit is ordered to move.
        public AudioClip GetMvtOrderAudio() { return mvtOrderAudio; }
        [SerializeField]
        private AudioClip mvtAudio = null; //Audio clip played when the unit is moving.
        [SerializeField]
        private AudioClip invalidMvtPathAudio = null; //When the movement path is invalid, this audio is played.

        private void Awake()
        {
            unit = GetComponent<Unit>();

            //get the nav agent and nav obstacle components:
            navAgent = GetComponent<NavMeshAgent>();
            navObstacle = GetComponent<NavMeshObstacle>();
            navPath = new NavMeshPath(); //initiate the path:

            Assert.IsNotNull(navAgent, "[UnitMovement] NavMeshAgent must be attached to the unit in order to make it movable.");
            navAgent.enabled = false; //disable it by default
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance; //always set to none as avoidance will be handled by the nav obstacle

            if (MovementManager.instance.IsAvoidanceEnabled() == true) //if avoidance is enabled
            {
                Assert.IsNotNull(navObstacle, "[UnitMovement] Movement avoidance is enabled but the unit doesn't have a NavMeshObstacle component attached to it.");
                navObstacle.enabled = false; //dsiable by default
                navObstacle.carving = true; //enable carving
                navObstacle.carveOnlyStationary = true; //carve only stationary 
            }
            else if (navObstacle != null) //if the nav obstacle is attached but avoidance is disabled or this is a flying unit
                Destroy(navObstacle); //destroy the nav obstacle

            //apply the speed modifier to both the speed and acceleration values
            speed *= GameManager.Instance.GetSpeedModifier();
            acceleration *= GameManager.Instance.GetSpeedModifier();
        }

        private void Start()
        {
            targetPositionCollider.transform.SetParent(null, true); //release the target position collider

            isMoving = false; //the unit is initially not moving.
            pendingMovement = false; //and not in pending movement

            maxSpeed = speed; //set default value for maxSpeed.
            CurrentSpeed = 0.0f; //default value for the current speed
        }

        void FixedUpdate()
        {
            if (unit.HealthComp.IsDead() == true) //if the unit is already dead
                return; //do not update movement

            if (isMoving == false || (canMoveRotate == false && facingNextDestination == false))
            {
                //deceleration (when either the unit is not moving or rotating to face next destination)
                if (CurrentSpeed > 0.0f)
                    CurrentSpeed -= acceleration * Time.deltaTime;

                if (isMoving == false && canIdleRotate == true && rotationTarget != Quaternion.identity) //can the unit rotate when idle (and the unit is not moving) + there's a valid rotation target
                {
                    if (lookAtTransform != null) //if there's a target object to look at
                        rotationTarget = RTSHelper.GetLookRotation(transform, lookAtTransform.position); //keep updating the rotation target as the target object might keep changing position

                    transform.rotation = Quaternion.Slerp(transform.rotation, rotationTarget, Time.deltaTime * rotationDamping); //smoothly update the unit's rotation
                }
            }

            if (isMoving == true) //if the unit is currently moving
            {
                if (navPath == null) //if the unit's path is invalid
                    Stop(); //stop the unit movement. 

                else //valid path
                {
                    //only if either the unit can move and rotate at the same time or it can't move and rotate and it's still hasn't faced its next destination in the path
                    if (canMoveRotate == false || facingNextDestination == true) 
                    {
                        if (mvtCheckTimer > 0) //movement check timer -> making sure the unit is not stuck at its current position
                            mvtCheckTimer -= Time.deltaTime;
                        if (mvtCheckTimer < 0) //the movement check duration is hardcoded to 2 seconds, while this is only a temporary solution for the units getting stuck issue, a more optimal solution will be soon presented
                        {
                            if (Vector3.Distance(transform.position, lastPosition) <= 0.1f) //if the time passed and we still in the same position (unit is stuck) then stop the movement
                            {
                                Stop();
                                unit.CancelJob(Unit.jobType.all); //cancel all unit jobs.
                            }
                            ReloadMvtCheck();
                        }
                    }

                    MoveAlongPath(); //move the unit along its path

                    if (DestinationReached == false) //check if the unit has reached its target position or not
                        DestinationReached = Vector3.Distance(transform.position, finalDestination) <= stoppingDistance;
                }

                if (DestinationReached == true)
                {
                    APC nextAPC = targetAPC;
                    Portal nextPortal = targetPortal;

                    Stop(); //stop the unit mvt

                    if (nextAPC != null) //if the unit is looking to get inside a APC
                        nextAPC.Add(unit); //get in the APC
                    else if (nextPortal != null) //if the unit is moving to get inside a portal
                        nextPortal.Teleport(unit); //go through the portal
                }
            }
        }

        //a method that attempts to calculate a path using the navigation agent component when given a target position
        public bool CalculatePath(Vector3 targetPosition)
        {
            //disable the nav obstacle component and enable the nav agent component so a path can be computed.
            navObstacle.enabled = false;
            navAgent.enabled = true;
            navAgent.CalculatePath(targetPosition, navPath); //calculate the path here

            if (navPath != null && navPath.status == NavMeshPathStatus.PathComplete) //if the generated path is valid and is fully complete
            {
                GeneratePathCornerQueue(); //generate the new corners queue
                return true;
            }
            return false;
        }

        //converts a valid path into a corners queue
        public void GeneratePathCornerQueue()
        {
            navPathCornerQueue = new Queue<Vector3>();
            foreach (Vector3 corner in navPath.corners)
            {
                navPathCornerQueue.Enqueue(corner);
            }

            finalDestination = navPath.corners[navPath.corners.Length - 1]; //the final destination is set to the last corner in the path.

            GetNextCorner(); //set the first corner's info
        }

        //a method that updates the current destination and direction to the next corner in the computed path
        private void GetNextCorner()
        {
            if (navPathCornerQueue.Count > 0) //if there are more corners left in the path
            {
                currentDestination = navPathCornerQueue.Dequeue();
                currentDirection = (currentDestination - transform.position).normalized;
                facingNextDestination = false;
            }
        }

        //moving the unit along its computed path
        private void MoveAlongPath()
        {
            float currentDistance = (transform.position - currentDestination).sqrMagnitude; //compute the distance between the current unit's position and the next corner in the path
            //update the rotation as long as the unit is moving to look at the next corner in the path queue.
            transform.rotation = Quaternion.Slerp(transform.rotation, RTSHelper.GetLookRotation(transform, currentDestination), Time.deltaTime * rotationDamping);

            //if the unit can't move before it faces a certain angle towards its next destination in the path
            if(canMoveRotate == false && facingNextDestination == false)
            {
                //keep checking if the angle between the unit and its next destination
                Vector3 lookAt = currentDestination - transform.position;
                lookAt.y = 0.0f;

                //as long as the angle is still over the min allowed movement angle, then do not proceed to keep moving
                if (Vector3.Angle(transform.forward, lookAt) > minMoveAngle)
                    return;
                else
                    facingNextDestination = true;
            }

            //if this is the last corner or the player's distance to the next corner reaches a min value, move to the next corner, if not keep moving the player towards the current corner.
            if (currentDistance > stoppingDistance || navPathCornerQueue.Count == 0)
            {
                //acceleration:
                CurrentSpeed = CurrentSpeed >= maxSpeed ? maxSpeed : CurrentSpeed + acceleration * Time.deltaTime;

                //move the unit on the x and z axis using the assigned speed
                transform.position += new Vector3(currentDirection.x * CurrentSpeed * Time.deltaTime, 0.0f, currentDirection.z * CurrentSpeed * Time.deltaTime);
            }
            else
                GetNextCorner();
        }

        //a method that reloads the movement check attributes:
        private void ReloadMvtCheck()
        {
            mvtCheckTimer = 2.0f; //launch the timer
            lastPosition = transform.position; //set this is as the last registered position.
        }

        //a method that stops the unit's movement.
        public void Stop()
        {
            DisablePendingMovement(true); 

            if (unit.HealthComp.IsDead() == true || isMoving == false) //if the unit is already dead or not moving, do not proceed.
                return;

            SetMaxSpeed(speed); //set the movement speed to the default one in case it was changed by the Attack on Escape component.

            StopCoroutine(heightCheckCoroutine);

            isMoving = false; //marked as not moving

            unit.SetAnimState(UnitAnimState.idle); //get into idle state

            AudioManager.StopAudio(gameObject); //stop the movement audio from playing

            //unit doesn't have a target APC or Portal to move to anymore
            targetAPC = null;
            targetPortal = null;

            rotationTarget = RTSHelper.GetLookRotation(transform, lookAtTarget); //update the rotation target using the registered lookAt position.

            UpdateTargetPositionCollider(transform.position); //set the target position's collider to the current unit position since the movement has stopped.
        }

        //called from the movement manager to enable pending movement
        public void EnablePendingMovement (MovementManager.MovementTask movementTask)
        {
            Stop(); //stop previous movement
            DestinationReached = false; //mark destination as not-reached
            TriggerTargetPositionCollider(true); //enable the target position collider here.
            UpdateTargetPositionCollider(movementTask.targetPosition); //set the target position collider at the target position

            //enable pending movement and store the movement task
            pendingMovement = true;
            pendingMovementTask = movementTask;
        }

        //called to disable pending movement
        public void DisablePendingMovement (bool removeTask)
        {
            if (removeTask == true && pendingMovement == true) //if pending movement was enabled
                MovementManager.instance.RemoveMovementTask(pendingMovementTask); //remove the pending movement task from the movement manager queue

            pendingMovement = false; //movement is not pending anymore
        }

        //called when after all retries, the movement manager still can't find a valid path for the unit's movement
        public void OnInvalidPath(bool playAudio, bool toIdle)
        {
            if (toIdle == true) //if this invalid path calculation moves the unit into idle state
            {
                Stop(); //if the unit was moving, stop it.
                unit.CancelJob(Unit.jobType.all); //stop all unit's current jobs.
            }
             
            if (playAudio && GameManager.PlayerFactionID == unit.FactionID) //if the local player owns this unit and we can play the invalid path audio
                AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, invalidMvtPathAudio, false);
        }

        //Called when a valid and complete path is calculated to start the unit's movement
        public void OnPathComplete(Vector3 targetPosition, GameObject targetObject, Vector3 lookAtTarget, float stoppingDistance, InputMode targetMode)
        {
            targetPortal = null;
            targetAPC = null;

            if (InputMode.portal == targetMode) //if the unit is orderd to move towards a portal
                targetPortal = targetObject.GetComponent<Portal>(); //set target portal
            else if (InputMode.APC == targetMode) //if the unit is orderd to move towards a APC
                targetAPC = targetObject.GetComponent<APC>();

            //movement settings:
            ReloadMvtCheck();
            this.lookAtTarget = lookAtTarget;
            lookAtTransform = (targetObject != null) ? targetObject.transform : null;
            this.stoppingDistance = stoppingDistance; //set the movement stopping distance.

            navAgent.enabled = false; //disable the nav agent component.
            navObstacle.enabled = true; //enable the nav mesh obstacle.

            isMoving = true; //player is now marked as moving
            DestinationReached = false; //destination is not reached by default

            if (unit.GetCurrAnimatorState() == UnitAnimState.moving) //if the unit was already moving, then lock changing the animator state briefly
                unit.LockAnimState = true;

            if (targetObject && targetObject.activeInHierarchy == true) //if the unit is moving towards an active object
            {
                List<Unit.jobType> jobsToCancel = new List<Unit.jobType>(); //holds the jobs that will be later cancelled
                jobsToCancel.AddRange(new Unit.jobType[] { Unit.jobType.attack, Unit.jobType.healing, Unit.jobType.converting, Unit.jobType.building, Unit.jobType.collecting });

                FactionEntity targetFactionEntity = targetObject.GetComponent<FactionEntity>();
                Resource targetResource = targetObject.GetComponent<Resource>();

                if (targetFactionEntity)
                {
                    if (unit.AttackComp && unit.AttackComp.Target == targetFactionEntity) //if the unit is set to attack the target object
                        jobsToCancel.Remove(Unit.jobType.attack);

                    Building targetBuilding = targetObject.GetComponent<Building>();
                    Unit targetUnit = targetObject.GetComponent<Unit>();

                    if (targetBuilding && targetBuilding.FactionID == unit.FactionID) //if the target object is a building that belongs to the unit's faction
                    {
                        if (unit.CollectorComp != null && unit.CollectorComp.IsDroppingOff() == true) //is the unit dropping off resources?
                            jobsToCancel.Remove(Unit.jobType.collecting);
                        if (unit.BuilderComp != null && unit.BuilderComp.GetTarget() == targetBuilding) //is the unit going to construct this building?
                            jobsToCancel.Remove(Unit.jobType.building);
                    }
                    else if (targetUnit) //if the target object is a unit
                    {
                        if (targetUnit.FactionID == unit.FactionID && targetAPC == null) //same faction and unit is not going towards a APC -> healing
                        {
                            jobsToCancel.Remove(Unit.jobType.healing);
                            navAgent.stoppingDistance = unit.HealerComp.GetStoppingDistance(); //set the stopping distance to be the max healing distance
                        }
                        else if (unit.ConverterComp && unit.ConverterComp.GetTarget() == targetUnit) //different faction but unit is going for conversion
                        {
                            jobsToCancel.Remove(Unit.jobType.converting);
                            navAgent.stoppingDistance = unit.ConverterComp.GetStoppingDistance(); //set the stopping distance to be the max converting distance
                        }
                    }
                }
                else if (targetResource && targetResource == unit.CollectorComp.GetTarget()) //if the target object is a resource and the unit is going to collect it
                    jobsToCancel.Remove(Unit.jobType.collecting);

                unit.CancelJob(jobsToCancel.ToArray());
            }
            else //no target object
                unit.CancelJob(Unit.jobType.all); //cancel all unit's jobs.

            unit.LockAnimState = false; //unlock animation state and play the movement anim
            unit.SetAnimState(UnitAnimState.moving);

            heightCheckCoroutine = HeightCheck(MovementManager.instance.GetHeightCheckReload()); //Start the height check coroutine

            StartCoroutine(heightCheckCoroutine);

            if(targetMode == InputMode.unitEscape && unit.EscapeComp != null) //if the unit is supposed to perform an attack escape & it has a valid escape component
            {
                SetMaxSpeed(unit.EscapeComp.GetSpeed());
            }

            //if the current speed is below zero, reset it
            if (CurrentSpeed < 0.0f)
                CurrentSpeed = 0.0f;

            AudioManager.PlayAudio(gameObject, mvtAudio, true);
        }

        //active when the unit is moving, it samples the height of the terrain where the unit is and updates it
        private IEnumerator HeightCheck(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);
                transform.position = new Vector3(
                        transform.position.x,
                        TerrainManager.instance.SampleHeight(transform.position, GetAgentRadius(), GetAgentAreaMask()),
                        transform.position.z);
            }
        }
    }
}
