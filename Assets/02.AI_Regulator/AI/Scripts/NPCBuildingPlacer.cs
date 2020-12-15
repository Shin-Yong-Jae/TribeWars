using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/* NPC Building Placer script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCBuildingPlacer : NPCComponent
    {
        //everything from the building object & placement info will be held in his structure and will be used to place the building
        public struct PendingBuilding
        {
            public Building buildingPrefab; //prefab of the building that is being placed
            public Building buildingInstance; //the actual building that will be placed.
            public Vector3 buildAroundPos; //building will be placed around this position.
            public Building buildingCenter; //the building center that the building instance will belong to.
            public float buildAroundDistance; //how close does the building is to its center?
            public bool rotate; //can the building rotate to look at the object its rotating around?
        }
        private List<PendingBuilding> pendingBuildings = new List<PendingBuilding>(); //list that holds all pending building infos.

        //placement settings:
        public FloatRange placementDelayRange = new FloatRange(7.0f, 20.0f); //actual placement will be only considered after this time.
        float placementDelay;
        public float rotationSpeed = 50.0f; //how fast will the building rotate around its build around position
        public float moveTimer = 10.0f; //whenever this timer is through, building will be moved away from build around position but keeps rotating
        float timer;
        public float timerInc = 2.0f; //this will be added to the move timer each time the building moves.
        int incVal = 0;
        public float moveDistance = 1.0f; //this the distance that the building will move at.

        [SerializeField, Range(0.0f,1.0f)]
        private float heightCheckReload = 0.2f; //how often does this component update the height of the pending building in a second?
        private IEnumerator heightCheckCoroutine; //this coroutine is running as long as there's a building to be placed and it allows NPC factions to place buildings on different heights

        ResourceManager resourceMgr;

        void Start()
        {
            resourceMgr = GameManager.Instance.ResourceMgr;
        }

        //a method that other NPC components use to request to place a building.
        public void OnBuildingPlacementRequest(Building buildingPrefab, GameObject buildAround, float buildAroundRadius, Building buildingCenter, float buildAroundDistance, bool rotate)
        {
            //if the building center or the build around object hasn't been specified:
            if (buildAround == null || buildingCenter == null)
            {
                Debug.LogError("Build Around object or Building Center for " + buildingPrefab.GetName() + " hasn't been specified in the Building Placement Request!");
                return;
            }

            //take resources to place building.
            resourceMgr.TakeResources(buildingPrefab.GetResources(), factionMgr.FactionID);

            //pick the building's spawn pos:
            Vector3 buildAroundPos = buildAround.transform.position;
            //for the sample height method, the last parameter presents the navigation layer mask and 0 stands for the built-in walkable layer where buildings can be placed
            buildAroundPos.y = TerrainManager.instance.SampleHeight(buildAround.transform.position, buildAroundRadius, 0) + BuildingPlacement.instance.BuildingYOffset;
            Vector3 buildingSpawnPos = buildAroundPos;
            buildingSpawnPos.x += buildAroundDistance;

            //create new instance of building and add it to the pending buildings list:
            PendingBuilding newPendingBuilding = new PendingBuilding
            {
                buildingPrefab = buildingPrefab,
                buildingInstance = Instantiate(buildingPrefab.gameObject, buildingSpawnPos, buildingPrefab.transform.rotation).GetComponent<Building>(),
                buildAroundPos = buildAroundPos,
                buildingCenter = buildingCenter,
                buildAroundDistance = buildAroundDistance,
                rotate = rotate
            };
            //initialize the building instance for placement:
            newPendingBuilding.buildingInstance.InitPlacementInstance(factionMgr.FactionID, buildingCenter.BorderComp);

            //we need to hide the building initially, when its turn comes to be placed, appropriate settings will be applied.
            newPendingBuilding.buildingInstance.gameObject.SetActive(false);
            newPendingBuilding.buildingInstance.ToggleModel(false); //Hide the building's model:

            //Call the start building placement custom event:
            if (GameManager.Instance.Events)
                GameManager.Instance.Events.OnBuildingStartPlacement(newPendingBuilding.buildingInstance);

            //add the new pending building to the list:
            pendingBuildings.Add(newPendingBuilding);

            if (pendingBuildings.Count == 1) //if the queue was empty before adding the new pending building
            {
                StartPlacingNextBuilding(); //immediately start placing it.

                heightCheckCoroutine = HeightCheck(heightCheckReload); //Start the height check coroutine to keep the building always on top of the terrain
                StartCoroutine(heightCheckCoroutine);
            }
        }

        //place buildings from the pending building list: First In, First Out
        void StartPlacingNextBuilding()
        {
            //if there's no pending building:
            if (pendingBuildings.Count == 0)
            {
                StopCoroutine(heightCheckCoroutine); //stop checking for height
                return; //stop.
            }

            //simply activate the first pending building in the list:
            pendingBuildings[0].buildingInstance.gameObject.SetActive(true);

            //reset building rotation/movement timer:
            timer = -1; //this will move the building from its initial position in the beginning of the placement process.
            incVal = 0;
            placementDelay = placementDelayRange.getRandomValue(); //start the placement delay timer.
        }

        void FixedUpdate()
        {
            if (pendingBuildings.Count > 0) //if that are pending buildings to be placed:
            {
                if(pendingBuildings[0].buildingInstance == null) //invalid building instance:
                {
                    StopPlacingBuilding(); //discard this pending building slot
                    return; //do not continue
                }

                float centerDistance = Vector3.Distance(pendingBuildings[0].buildingInstance.transform.position, pendingBuildings[0].buildingCenter.transform.position);
                //if building center of the current pending building is destroyed while building is getting placed:
                //or if the building is too far away or too close from the center
                if (pendingBuildings[0].buildingCenter == null || centerDistance > pendingBuildings[0].buildingCenter.BorderComp.Size)
                {
                    StopPlacingBuilding(); //Stop placing building.
                    return;
                }

                //building movement timer:
                if (timer > 0)
                {
                    timer -= Time.deltaTime;
                }
                else
                {
                    //reset timer:
                    timer = moveTimer + (timerInc * incVal);
                    incVal++;

                    //move building away from build around pos.
                    Vector3 mvtDir = (pendingBuildings[0].buildingInstance.transform.position - pendingBuildings[0].buildAroundPos).normalized;
                    mvtDir.y = 0.0f;
                    if (mvtDir == Vector3.zero)
                    {
                        mvtDir = new Vector3(1.0f, 0.0f, 0.0f);
                    }
                    pendingBuildings[0].buildingInstance.transform.position += mvtDir * moveDistance;
                }

                //move the building around its build around position:
                Quaternion buildingRotation = pendingBuildings[0].buildingInstance.transform.rotation; //save building rotation
                //this will move the building around the build around pos which what we want but it will also affect the build rotation..
                pendingBuildings[0].buildingInstance.transform.RotateAround(pendingBuildings[0].buildAroundPos, Vector3.up, rotationSpeed * Time.deltaTime);

                if (pendingBuildings[0].rotate == true) //if the building should be rotated to face its center object
                    pendingBuildings[0].buildingInstance.transform.rotation = RTSHelper.GetLookRotation(pendingBuildings[0].buildingInstance.transform, pendingBuildings[0].buildAroundPos, true);
                else
                    pendingBuildings[0].buildingInstance.transform.rotation = buildingRotation; //set initial rotation

                //placement delay timer:
                if (placementDelay > 0)
                {
                    placementDelay -= Time.deltaTime;
                }
                else //if the placement delay is through, NPC faction is now allowed to place faction:
                {
                    //Check if the building is in a valid position or not:
                    pendingBuildings[0].buildingInstance.PlacerComp.CheckBuildingPos();

                    //can we place the building:
                    if (pendingBuildings[0].buildingInstance.PlacerComp.CanPlace == true)
                    {
                        PlaceBuilding();
                        return;
                    }
                }
            }
        }

        //active when this component is placing a building, it allows to keep the building on top of the navmesh
        private IEnumerator HeightCheck(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);

                if (pendingBuildings[0].buildingInstance != null) //make sure the pending building instance is valid:
                {
                    transform.position = new Vector3(
                            transform.position.x,
                            TerrainManager.instance.SampleHeight(transform.position, pendingBuildings[0].buildingInstance.GetRadius(), 0) + BuildingPlacement.instance.BuildingYOffset,
                            transform.position.z);
                }
            }
        }

        //method that places a building.
        void PlaceBuilding()
        {
            //place the first building in the pending buildings list:

            //destroy the building instance that was supposed to be placed:
            Destroy(pendingBuildings[0].buildingInstance.gameObject);

            //ask the building manager to create a new placed building:
            BuildingManager.CreatePlacedInstance(pendingBuildings[0].buildingPrefab, 
                pendingBuildings[0].buildingInstance.transform.position,
                pendingBuildings[0].buildingInstance.transform.rotation.eulerAngles.y,
                pendingBuildings[0].buildingCenter.BorderComp, factionMgr.FactionID);

            //remove the first item in pending buildings list:
            pendingBuildings.RemoveAt(0);

            StartPlacingNextBuilding(); //start placing next building
        }

        //a method that stops placing a building
        void StopPlacingBuilding()
        {
            if (pendingBuildings[0].buildingInstance != null) //valid building instance:
            {
                //Call the stop building placement custom event:
                if (GameManager.Instance.Events)
                    GameManager.Instance.Events.OnBuildingStopPlacement(pendingBuildings[0].buildingInstance);

                //Give back resources:
                resourceMgr.GiveBackResources(pendingBuildings[0].buildingInstance.GetResources(), factionMgr.FactionID);

                //destroy the building instance that was supposed to be placed:
                Destroy(pendingBuildings[0].buildingInstance.gameObject);
            }

            //remove the first item in pending buildings list:
            pendingBuildings.RemoveAt(0);

            StartPlacingNextBuilding(); //start placing next building.
        }
    }
}
