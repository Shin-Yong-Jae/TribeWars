using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/* Movement Manager created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class MovementManager : MonoBehaviour
    {
        public static MovementManager instance;

        //Movement & Avoidance:
        [SerializeField]
        private bool enableAvoidance = true; //should units avoid entering in collision when moving?
        public bool IsAvoidanceEnabled() { return enableAvoidance; }

        //the normal movement formation for non attack units and attack units can be different.
        //attack units use their range type movement formation when moving to attack
        [System.Serializable]
        public struct MovementFormation
        {
            public Formations nonAttackUnits;
            public Formations attackUnits;
            public int unitsPerRow;
        }
        [SerializeField]
        private MovementFormation movementFormation = new MovementFormation();
        public Formations GetFormation(Unit unit, bool attackMovement)
        {
            return (attackMovement == true) ? unit.AttackComp.RangeType.GetFormation() : ((unit.AttackComp != null) ? movementFormation.attackUnits : movementFormation.nonAttackUnits);
        }

        [SerializeField]
        private LayerMask groundUnitLayerMask = new LayerMask(); //a layer mask that contains ground unit-related layers.
        [SerializeField]
        private LayerMask airUnitLayerMask = new LayerMask(); //a layer mask that contains air unit-related layers.

        //The stopping distance when a unit moves to an empty space of the map:
        [SerializeField]
        private float stoppingDistance = 0.3f;
        public float GetStoppingDistance() { return stoppingDistance; }

        //Mvt target effect:
        [SerializeField]
        private EffectObj movementTargetEffect = null;

        //a movement task consists of the unit and its target position and movement info
        public struct MovementTask
        {
            public Unit unit;
            public Vector3 targetPosition;
            public Vector3 lookAtPosition;
            public GameObject targetObject;
            public InputMode targetMode;
            public bool attack;
            public FactionEntity targetEntity;
            public byte retries;
        }

        //queue to move units to their destinations
        private LinkedList<MovementTask> movementQueue = new LinkedList<MovementTask>();

        [SerializeField]
        private byte pathCalculationMaxRetries = 3; //sometimes the NavMesh fails to generate a correct path even if it's obvious, because a unit might be surrounded by other units for example.
        //this value determines how many times the movement task will be re-added at the end of the queue to re-calculate a valid path for the unit

        [SerializeField, Range(0.0f, 1.0f)]
        private float unitHeightCheckReload = 0.2f; //how often will units update their heights (position at the y position) during movement
        public float GetHeightCheckReload () { return unitHeightCheckReload; }

        //a method that adds a movement task to the movement queue
        public void EnqueueMovementTask(MovementTask newMovementTask, bool recalculatingPath = false)
        {
            if(recalculatingPath == false) //make sure this is not to recalculate the path
                newMovementTask.unit.MovementComp.EnablePendingMovement(newMovementTask); //enable the pending movement on the unit movement component

            movementQueue.AddLast(newMovementTask); //add the movement task to the movement queue
        }

        //a method that removes a movement task from the movement queue
        public void RemoveMovementTask (MovementTask movementTask)
        {
            movementQueue.Remove(movementTask);
        }

        void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(this);
        }

        private void Update()
        {
            if (movementQueue.Count > 0) //if there are movement tasks in the movement queue
            {
                OnMovementRequest(movementQueue.First.Value); //free the the oldest one in the queue & move the unit to its target position
                movementQueue.RemoveFirst();
            }
        }

        //the Move method is called when another component requests the movement of a single unit
        public void Move(Unit unit, Vector3 destination, float offsetRadius, GameObject targetObject, InputMode targetMode, bool playerCommand)
        {
            if (GameManager.MultiplayerGame == false) //single player game, directly prepare the unit's movement
                PrepareMove(unit, destination, offsetRadius, targetObject, targetMode, playerCommand);
            else if (GameManager.Instance.IsLocalPlayer(unit.gameObject) == true) //multiplayer game and this is the local player
            {
                //send input action to the input manager
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)targetMode,
                    initialPosition = unit.transform.position,
                    targetPosition = destination,
                    value = Mathf.FloorToInt(offsetRadius)
                };

                //sent input
                InputManager.SendInput(newInput, unit.gameObject, targetObject);
            }
        }

        //the PrepareMove method prepares the movement of a single unit
        public void PrepareMove(Unit unit, Vector3 destination, float offsetRadius, GameObject targetObject, InputMode targetMode, bool playerCommand, bool attackMovement = false, AttackModes attackMode = AttackModes.none, FactionEntity targetEntity = null)
        {
            if (unit.MovementComp.CanMove() == false) //if the unit can't move
            {
                if (playerCommand == true && unit.FactionID == GameManager.PlayerFactionID) //player notification message
                    UIManager.instance.ShowPlayerMessage("You can't move this unit!", UIManager.MessageTypes.Error);

                unit.MovementComp.OnInvalidPath(true, false);
                return; //nothing to do here
            }

            if (attackMovement == true) //if this is an attack movement
            {
                //change the target?
                bool canChangeTarget = (attackMode == AttackModes.full || attackMode == AttackModes.change) ? true : false;
                //assigned attack only?
                bool assignedAttackOnly = (attackMode == AttackModes.full || attackMode == AttackModes.assigned) ? true : false;

                if (CanProceedAttackMovement(unit, playerCommand, assignedAttackOnly, canChangeTarget, targetEntity) == false) //do not proceed if the attack is not possible
                    return;

                offsetRadius += unit.AttackComp.RangeType.GetStoppingDistance(targetEntity == null ? false : targetEntity.Type == FactionEntityTypes.unit); //update sort key

                if (Vector3.Distance(unit.transform.position, targetEntity.transform.position) < offsetRadius) //if the attack unit is already inside its target range
                {
                    //attack directly
                    EnqueueMovementTask(new MovementTask
                    {
                        unit = unit,
                        targetPosition = unit.transform.position,
                        lookAtPosition = targetEntity.transform.position,
                        targetObject = targetEntity.gameObject,
                        targetMode = InputMode.attack,
                        attack = true,
                        targetEntity = targetEntity
                    }); //add a new movement task to the movement queue to attack directly
                    return; //move to next unit
                }
            }

            unit.MovementComp.TriggerTargetPositionCollider(false); //disable the target position collider so it won't intefer in the target position collider

            if (playerCommand && attackMovement == false) //if this is a player command and it's not an attack movement
                EffectObjPool.SpawnEffectObj(movementTargetEffect, destination, movementTargetEffect.transform.rotation);

            //first check if the actual destination is a valid target position, if it can't be then search for a target position
            if (offsetRadius > 0.0f || IsPositionClear(ref destination, unit.MovementComp.GetAgentRadius(), unit.MovementComp.GetAgentAreaMask(), unit.MovementComp.CanFly()) == false)
            {
                List<Vector3> targetPositions = GenerateTargetPositions(Formations.circular, //it doesn't really matter what formation to use for one unit
                    destination, 1,
                    unit.MovementComp.GetAgentRadius(),
                    unit.MovementComp.GetAgentAreaMask(),
                    unit.MovementComp.CanFly(),
                    ref offsetRadius, Vector3.zero); //get the target positions for the current units list

                if (targetPositions.Count == 0) //no target positions found?
                {
                    if (playerCommand == true && unit.FactionID == GameManager.PlayerFactionID) //if this is a player command then inform the player
                        UIManager.instance.ShowPlayerMessage("The unit will not be moved!", UIManager.MessageTypes.Error); //let the player know that the unit won't move
                    unit.CancelJob(Unit.jobType.all); //cancel all the unit's jobs.
                    return;
                }

                targetPositions.Sort((x, y) => Vector3.Distance(x, unit.transform.position).CompareTo(Vector3.Distance(y, unit.transform.position))); //sort the target positions list depending on the distance to the actual unit to move

                destination = targetPositions[0]; //get the nearest target position
            }

            EnqueueMovementTask(new MovementTask
            {
                unit = unit,
                targetPosition = destination,
                lookAtPosition = destination,
                targetObject = targetObject,
                targetMode = targetMode,
                attack = attackMovement,
                targetEntity = targetEntity
            }); //add a new movement task to the movement queue.

            if (playerCommand) //if this is a player command
                AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, attackMovement == true ? unit.AttackComp.GetOrderAudio() : unit.MovementComp.GetMvtOrderAudio(), false); //play the movement order audio
        }

        //the Move method is called when another component requests the movement of a list of units
        public void Move(List<Unit> units, Vector3 destination, float offsetRadius, GameObject targetObject, InputMode targetMode, bool playerCommand)
        {
            if(units.Count == 1) //if there's only one unit in the list
            {
                Move(units[0], destination, offsetRadius, targetObject, targetMode, playerCommand); //use the one-unit movement
                return;
            }

            if (GameManager.MultiplayerGame == false) //single player game, directly prepare the unit's list movement
                PrepareMove(units, destination, offsetRadius, targetObject, targetMode, playerCommand);
            else if (GameManager.Instance.IsLocalPlayer(units[0].gameObject) == true) //multiplayer game and this is the local player
            {
                //send input action to the input manager
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unitGroup,
                    targetMode = (byte)targetMode,
                    targetPosition = destination,
                    value = Mathf.FloorToInt(offsetRadius),
                    groupSourceID = InputManager.UnitListToString(units)
                };

                //sent input
                InputManager.SendInput(newInput, null, targetObject);
            }
        }

        //the PrepareMove method prepares the movement of a list of units by sorting them based on their radius, distance to target and generating target positions
        public void PrepareMove(List<Unit> units, Vector3 destination, float offsetRadius, GameObject targetObject, InputMode targetMode, bool playerCommand, bool attackMovement = false, AttackModes attackMode = AttackModes.none, FactionEntity targetEntity = null)
        {
            //create a new chained sorted list where units will be sorted based on their radius or stopping distance towards the target unit/building (in case of an attack)
            ChainedSortedList<float, Unit> radiusSortedUnits = SortUnits(units, attackMovement ? UnitSortTypes.attackRange : UnitSortTypes.radius, offsetRadius, attackMode, targetEntity, playerCommand);

            float currOffsetRadius = offsetRadius; //set the current offset radius (we might need the same original offsetRadius value later)

            AudioClip movementOrderAudio = null; //we'll just pick one of the movement order audios from the units list

            if (playerCommand && attackMovement == false) //if this is a player command and it's not an attack movement
                EffectObjPool.SpawnEffectObj(movementTargetEffect, destination, movementTargetEffect.transform.rotation);

            foreach (KeyValuePair<float, List<Unit>> currUnits in radiusSortedUnits) //for each different list of units that share the same radius
            {
                if (attackMovement == true) //if this is an attack movement
                    currOffsetRadius += currUnits.Key; //increment the offset radius by the attack range (which is represented by the key in case)

                //sort units in ascending order of the distance to the target position
                currUnits.Value.Sort((x, y) => Vector3.Distance(x.transform.position, destination).CompareTo(Vector3.Distance(y.transform.position, destination)));

                UnitMovement movementRef = currUnits.Value[0].MovementComp; //reference movement component

                Vector3 unitsDirection = Vector3.zero; //direction of units regarding the target position
                foreach (Unit unit in currUnits.Value)
                    unitsDirection += (destination - unit.transform.position).normalized;

                unitsDirection /= currUnits.Value.Count;

                Formations currentFormation = GetFormation(currUnits.Value[0], attackMovement); //current movement formation for this units group

                List<Vector3> targetPositions = GenerateTargetPositions(currentFormation,
                    destination, currUnits.Value.Count,
                    movementRef.GetAgentRadius(),
                    movementRef.GetAgentAreaMask(),
                    movementRef.CanFly(),
                    ref currOffsetRadius, unitsDirection); //get the target positions for the current units list

                for (int i = 0; i < currUnits.Value.Count; i++)//go through the sorted units list
                {
                    if (targetPositions.Count == 0) //no more target positions available?
                    {
                        if (playerCommand == true && currUnits.Value[i].FactionID == GameManager.PlayerFactionID) //if this is a player command then inform the player
                            UIManager.instance.ShowPlayerMessage("Some unit(s) will not be moved!", UIManager.MessageTypes.Error); //let the player know that some of the units won't be moved
                        continue;
                    }

                    switch (currentFormation)
                    {
                        case Formations.circular:
                            if (attackMovement == true)//only pick the closest target position in case this is an attack movement type and we're using the circular formation
                                targetPositions.Sort((x, y) => Vector3.Distance(x, currUnits.Value[i].transform.position).CompareTo(Vector3.Distance(y, currUnits.Value[i].transform.position))); //sort the target positions list depending on the distance to the actual unit to move
                            break;
                        case Formations.rectangular: //if this is a rectangular formation, then the closest unit always gets to the farthest position
                            targetPositions.Sort((x, y) => Vector3.Distance(y, currUnits.Value[i].transform.position).CompareTo(Vector3.Distance(x, currUnits.Value[i].transform.position))); //sort the target positions list depending on the distance to the actual unit to move
                            break;
                    }

                    if (movementOrderAudio == null) //if the movement order audio hasn't been set yet
                        movementOrderAudio = attackMovement == true ? currUnits.Value[i].AttackComp.GetOrderAudio() : currUnits.Value[i].MovementComp.GetMvtOrderAudio(); //set it.

                    EnqueueMovementTask(new MovementTask
                    {
                        unit = currUnits.Value[i],
                        targetPosition = targetPositions[0],
                        lookAtPosition = targetPositions[0] + unitsDirection,
                        targetObject = targetObject,
                        targetMode = targetMode,
                        attack = attackMovement,
                        targetEntity = targetEntity
                    }); //add a new movement task to the movement queue

                    targetPositions.RemoveAt(0);
                }
            }

            if (playerCommand) //if this is a player command
                AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, movementOrderAudio, false); //play the movement order audio
        }

        public enum UnitSortTypes { radius, attackRange }
        public enum AttackModes { none, change, full, assigned }

        //a method that sorts a list of units depending on the sort type and return a chained sorted list of the sorted units
        private ChainedSortedList<float, Unit> SortUnits(List<Unit> units, UnitSortTypes sortType, float offsetRadius, AttackModes attackMode = AttackModes.none, FactionEntity targetEntity = null, bool playerCommand = false)
        {
            ChainedSortedList<float, Unit> sortedUnits = new ChainedSortedList<float, Unit>(); //this will hold the sorted list of the units.

            //attack only:
            //change the target?
            bool canChangeTarget = (attackMode == AttackModes.full || attackMode == AttackModes.change) ? true : false;
            //assigned attack only?
            bool assignedAttackOnly = (attackMode == AttackModes.full || attackMode == AttackModes.assigned) ? true : false;
            //is the target faction entity a unit?
            bool isTargetUnit = targetEntity == null ? false : targetEntity.Type == FactionEntityTypes.unit;

            foreach (Unit unit in units) //go through the list of units.
            {
                if (unit.MovementComp.CanMove() == false) //if the unit can't move
                {
                    if (playerCommand && unit.FactionID == GameManager.PlayerFactionID) //player notification message
                        UIManager.instance.ShowPlayerMessage("Some unit(s) can't be moved!", UIManager.MessageTypes.Error);

                    continue;
                }

                float sortKey = unit.MovementComp.GetAgentRadius(); //sort key is set to agent radius by default

                if (sortType != UnitSortTypes.radius) //if we're not sorting by radius <=> we're not sorting for a simple movement but for an attack movement and therefore:
                {
                    if (CanProceedAttackMovement(unit, playerCommand, assignedAttackOnly, canChangeTarget, targetEntity) == false) //do not proceed if the attack is not possible
                        continue;

                    sortKey = unit.AttackComp.RangeType.GetStoppingDistance(isTargetUnit); //update sort key

                    if (Vector3.Distance(unit.transform.position, targetEntity.transform.position) < sortKey + offsetRadius) //if the attack unit is already inside its target range
                    {
                        //attack directly
                        EnqueueMovementTask(new MovementTask
                        {
                            unit = unit,
                            targetPosition = unit.transform.position,
                            lookAtPosition = targetEntity.transform.position,
                            targetObject = targetEntity.gameObject,
                            targetMode = InputMode.attack,
                            attack = true,
                            targetEntity = targetEntity
                        }); //add a new movement task to the movement queue to attack directly
                        continue; //move to next unit
                    }
                }

                unit.MovementComp.TriggerTargetPositionCollider(false); //disable the target position collider first so that it won't intefer in the movement calculations
                sortedUnits.Add(sortKey, unit); //add the new alongside its radius as the key.
            }

            return sortedUnits;
        }

        public enum Formations { circular, rectangular };

        //this constant determines the maximum allowed iterations in the while loop of the GenerateTargetPositions method in regards to the "amount" parameter (amount of requested positions)
        //the while loop can run a maximum of: amount + targetPositionGenerationMaxIterations
        private const byte targetPositionGenerationMaxIterations = 255;

        //by following a certain formation, this method generates a list of free positions to be occupied by the unit in range of a target position.
        public List<Vector3> GenerateTargetPositions(Formations formation, Vector3 originPosition, int amount, float unitRadius, LayerMask agentAreaMask, bool isFlyUnit, ref float offsetRadius, Vector3 unitsDirection)
        {
            if (isFlyUnit == true) //if this is a flying unit
                originPosition.y = TerrainManager.instance.GetFlyingHeight(); //set the position to the flying height.

            if (formation != Formations.circular) //if this is not a circular formation
                originPosition -= new Vector3((offsetRadius) * unitsDirection.x, 0.0f, offsetRadius * unitsDirection.z); //offset from the start

            List<Vector3> targetPositions = new List<Vector3>(); //this will hold the target positions.

            int rowSize = amount > movementFormation.unitsPerRow ? movementFormation.unitsPerRow : amount; //the amount of units per row (in case this is a rectangular formation)
            if (rowSize < 1) //units per row can't be lower than 1
                rowSize = 1;

            int mainCounter = targetPositionGenerationMaxIterations + amount; //counter for the while loop
            int subCounter = 0; //counter for the loops inside the switch

            while (targetPositions.Count < amount && mainCounter > 0) //as long as the required amount of target positions isn't set and we haven't surpassed the max # of allowed iterations
            {
                mainCounter--;

                switch (formation)
                {
                    case Formations.circular: //in case we have a circular formation

                        float perimeter = 2 * Mathf.PI * offsetRadius; //calculate the perimeter of the circle in which unoccupied positions will be searched
                        int expectedPositionCount = Mathf.FloorToInt(perimeter / (unitRadius * 2f)); //calculate the expected amount of free positions for the unit with unitRadius in the circle

                        float angleIncValue = 360f / expectedPositionCount; //the increment value of the angle inside the current circle with the above perimeter
                        float currentAngle = 0.0f;

                        subCounter = 0;
                        while (subCounter < expectedPositionCount) //as long as we haven't inspected all the expected free positions inside this cirlce
                        {
                            currentAngle += angleIncValue; //set the angle value

                            //calculate the position on the circle
                            float x = originPosition.x + offsetRadius * Mathf.Sin(Mathf.Deg2Rad * currentAngle);
                            float z = originPosition.z + offsetRadius * Mathf.Cos(Mathf.Deg2Rad * currentAngle);

                            //calculate the target position using the x,z and sampled y values
                            Vector3 possiblePosition = new Vector3(x, TerrainManager.instance.SampleHeight(new Vector3(x, originPosition.y, z), unitRadius, agentAreaMask), z);
                            if (IsPositionClear(ref possiblePosition, unitRadius, agentAreaMask, isFlyUnit))
                                targetPositions.Add(possiblePosition);

                            subCounter++;
                        }

                        offsetRadius += unitRadius;

                        break;

                    case Formations.rectangular:

                        Vector3 rowDirection = new Vector3(unitsDirection.z, unitsDirection.y, -unitsDirection.x).normalized;

                        Vector3 pos = originPosition;
                        pos -= new Vector3((unitRadius * rowSize) * rowDirection.x, 0.0f, (unitRadius * rowSize) * rowDirection.z);

                        subCounter = 0;
                        while (subCounter < rowSize)
                        {
                            pos = new Vector3(pos.x, TerrainManager.instance.SampleHeight(pos, unitRadius, agentAreaMask), pos.z);
                            if (IsPositionClear(ref pos, unitRadius, agentAreaMask, isFlyUnit))
                            {
                                targetPositions.Add(pos);
                                originPosition.y = pos.y;
                            }

                            pos += new Vector3((unitRadius * 2.0f) * rowDirection.x, 0.0f, (unitRadius * 2.0f) * rowDirection.z);

                            subCounter++;
                        }

                        originPosition -= new Vector3((unitRadius * 2.0f) * unitsDirection.x, 0.0f, (unitRadius * 2.0f) * unitsDirection.z);
                        break;
                }
            }

            if (targetPositions.Count < amount) //if we still haven't found all the target positions and we have exceeded the max amount of allowed iterations
                Debug.LogError("[MovementManager] Maximum amount of allowed iterations in the GenerateTargetPositions method has been exceeded but not all target positions have been found");

            return targetPositions;
        }

        //a method that determines whether a target position is clear (unoccupied) or not.
        private bool IsPositionClear(ref Vector3 targetPosition, float agentRadius, LayerMask agentAreaMask, bool isFlyUnit)
        {
            //this will search for the "Unit Target Position" colliders which present the intended destinations of units so that two units do not end up in the same position
            Collider[] targetPositionCollidersInRange = Physics.OverlapSphere(targetPosition, agentRadius, isFlyUnit ? airUnitLayerMask : groundUnitLayerMask); //we perform this "expensive" method on a very small area (agent radius).
            //now we'll sample the navigation mesh in the agent radius sized range of target position to get the free position inside that range.
            NavMeshHit hit;
            if (targetPositionCollidersInRange.Length == 0 && NavMesh.SamplePosition(targetPosition, out hit, agentRadius, agentAreaMask)) //so if there are no target position collider in the target position range and the nav mesh sample position returns an unoccupied position
            {
                targetPosition = hit.position; //assign position and return true.
                return true;
            }
            return false;
        }

        //a method that moves a single unit to its target position
        private void OnMovementRequest(MovementTask movementTask)
        {
            if (movementTask.unit == null || movementTask.unit.HealthComp.IsDead() == true) //invalid/dead unit? (do not continue)
                return;

            //attempt to calculate a valid path for the movement:
            //if the calculate path is invalid/incomplete
            if (movementTask.unit.MovementComp.CalculatePath(movementTask.targetPosition) == false)
            {
                if (movementTask.retries < pathCalculationMaxRetries) //we still have more retries to calculate the path again
                {
                    movementTask.retries++;
                    EnqueueMovementTask(movementTask, true); //add movement task back at the end of the queue to compute the path again
                }
                else
                    movementTask.unit.MovementComp.OnInvalidPath(false, true);
            }
            else //if a valid path is found
            {
                movementTask.unit.MovementComp.DisablePendingMovement(false); //movement is no longer pending

                if (movementTask.attack == true && movementTask.targetEntity == null) //if the unit is supposed to attack a target and the target entity is invalid
                    return; //do not engage/move unit

                if (movementTask.attack == true) //attack movement
                    movementTask.unit.AttackComp.SetTargetLocal(movementTask.targetEntity);

                movementTask.unit.MovementComp.OnPathComplete(
                    movementTask.targetPosition,
                    movementTask.targetObject,
                    movementTask.lookAtPosition,
                    stoppingDistance,
                    movementTask.targetMode); //move the unit
            }
        }

        //a method that launches an attack movement for a single unit
        public void LaunchAttack(Unit unit, FactionEntity targetEntity, AttackModes attackMode, bool playerCommand)
        {
            if (GameManager.MultiplayerGame == false) //single player game, directly prepare the unit's attack movement
                PrepareMove(unit, targetEntity.transform.position, targetEntity.GetRadius(), targetEntity.gameObject, InputMode.attack, playerCommand, true, attackMode, targetEntity);
            else if (GameManager.Instance.IsLocalPlayer(unit.gameObject) == true) //multiplayer game and this is the local player
            {
                //send input action to the input manager
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)InputMode.attack,
                    initialPosition = unit.transform.position,
                    value = (int)attackMode,
                };

                //sent input
                InputManager.SendInput(newInput, unit.gameObject, targetEntity.gameObject);
            }
        }

        //a method that launches an attack movement for a single unit locally
        public void LaunchAttackLocal(Unit unit, FactionEntity targetEntity, AttackModes attackMode, bool playerCommand)
        {
            PrepareMove(unit, targetEntity.transform.position, targetEntity.GetRadius(), targetEntity.gameObject, InputMode.attack, playerCommand, true, attackMode, targetEntity);
        }

        //a method that launches the attack movement of a list of units
        public void LaunchAttack(List<Unit> units, FactionEntity targetEntity, AttackModes attackMode, bool playerCommand)
        {
            if(units.Count == 1) //one unit only?
            {
                //use the one-unit attack movement
                PrepareMove(units[0], targetEntity.transform.position, targetEntity.GetRadius(), targetEntity.gameObject, InputMode.attack, playerCommand, true, attackMode, targetEntity);
                return;
            }

            if (GameManager.MultiplayerGame == false) //single player game, directly prepare the unit's attack movement
                PrepareMove(units, targetEntity.transform.position, targetEntity.GetRadius(), targetEntity.gameObject, InputMode.attack, playerCommand, true, attackMode, targetEntity);
            else if (GameManager.Instance.IsLocalPlayer(units[0].gameObject) == true) //multiplayer game and this is the local player
            {
                //send input action to the input manager
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unitGroup,
                    targetMode = (byte)InputMode.attack,
                    value = (int)attackMode,
                    groupSourceID = InputManager.UnitListToString(units)
                };

                //sent input
                InputManager.SendInput(newInput, null, targetEntity.gameObject);
            }
        }

        //a method that launches an attack movement for a a list of units locally
        public void LaunchAttackLocal(List<Unit> units, FactionEntity targetEntity, AttackModes attackMode, bool playerCommand)
        {
            PrepareMove(units, targetEntity.transform.position, targetEntity.GetRadius(), targetEntity.gameObject, InputMode.attack, playerCommand, true, attackMode, targetEntity);
        }

        //a method that checks whether a unit can proceed with an attack movement or not
        public bool CanProceedAttackMovement (Unit unit, bool playerCommand, bool assignedAttackOnly, bool canChangeTarget, FactionEntity targetEntity)
        {
            if (unit.AttackComp == null) //if the unit doesn't have an attack component
            {
                if (playerCommand == true && unit.FactionID == GameManager.PlayerFactionID) //player notification message
                    UIManager.instance.ShowPlayerMessage("This unit can't launch an attack!", UIManager.MessageTypes.Error);

                return false; //do not proceed.
            }

            //if the attack type is assigned and the target can't engage when a target is assigned...
            //... or the unit can't change its current target...
            //... or the unit can't engage with this particular target
            if ((assignedAttackOnly == true && unit.AttackComp.CanEngageOnAssign() == false)
                || (unit.AttackComp.Target != null && canChangeTarget == false))
            {
                if (playerCommand == true && unit.FactionID == GameManager.PlayerFactionID) //player notification message
                    UIManager.instance.ShowPlayerMessage("You can't set/change this unit's target!", UIManager.MessageTypes.Error);
                return false;
            }
            else if (unit.AttackComp.CanEngageTarget(targetEntity) == false)
            {
                if (playerCommand == true && unit.FactionID == GameManager.PlayerFactionID) //player notification message
                    UIManager.instance.ShowPlayerMessage("Can't engage target!", UIManager.MessageTypes.Error);
                return false;
            }

                return true;
        }

        //a method that picks a random movable position starting from on origin point and inside a certain range
        public Vector3 GetRandomMovablePosition(Vector3 origin, float range, Unit unit, LayerMask areaMask)
        {
            Vector3 randomDirection = Random.insideUnitSphere * range; //pick a random direction to go to
            randomDirection += origin;
            randomDirection.y = TerrainManager.instance.SampleHeight(randomDirection, range, areaMask);

            Vector3 targetPosition = unit.transform.position;
            //get the closet movable point to the random chosen direction
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, range, areaMask))
            {
                targetPosition = hit.position;
                IsPositionClear(ref targetPosition, unit.MovementComp.GetAgentRadius(), unit.MovementComp.GetAgentAreaMask(), unit.MovementComp.CanFly());
            }

            return targetPosition;
        }

    }

}