using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RTSEngine
{
    public class Building : FactionEntity
    {
        //when using task panel categories, this is the category ID where the task button of this building will appear when selecting builder units.
        [SerializeField]
        private int taskPanelCategory = 0;
        public int GetTaskPanelCategory() { return taskPanelCategory; }

        [System.Serializable]
        public class RequiredBuilding
        {
            [SerializeField]
            private List<Building> buildingList = new List<Building>(); //If one the buildings in this list is spawned and built then the requirement is fulfilled.
            public List<Building> GetBuildingList() { return buildingList; }
            [SerializeField]
            private string name = "";  //will be displayed in the UI task tooltip
            public string GetName() { return name; }
        }
        [SerializeField]
        private List<RequiredBuilding> requiredBuildings = new List<RequiredBuilding>();
        public List<RequiredBuilding> GetRequiredBuildings() { return requiredBuildings; }

        [SerializeField]
        private ResourceManager.Resources[] resources = new ResourceManager.Resources[0]; //required resources to place this building
        public ResourceManager.Resources[] GetResources() { return resources; }

        [SerializeField]
        private float radius = 5; //the building's radius will be used to determine when units stop when attacking the building.
        public override float GetRadius() { return radius; }

        public bool FactionCapital { set; get; } //If true, then the building is the capital of this faction.

        [SerializeField]
        private int addPopulation = 0; //Allows to add/remove population slots for the faction that this belongs to.
        public void RemovePopulationSlots() { GameManager.Instance.Factions[factionID].UpdateMaxPopulation(-addPopulation); }

        //Resource collection bonus: A building can affect the resources that are inside the same border by increasing the amount of collection per second
        [System.Serializable]
        public class BonusResource
        {
            [SerializeField]
            private ResourceTypeInfo resourceType = null; //The resource type info asset file goes here.
            public string GetResourceTypeName () { return resourceType.Name; }
            [SerializeField]
            private float durationReduction = 1.0f; //self-explantory
            public float GetBonus () { return -durationReduction; }
        }
        [SerializeField]
        private BonusResource[] bonusResources = new BonusResource[0];

        public Border CurrentCenter { set; get; } //the building's current center

        //placement settings (these must be attributes of the main building component as they will be required by other external components):
        [SerializeField]
        private bool placedByDefault = false; //Is the building placed by default on the map.
        public bool PlacedByDefault { set { placedByDefault = value; } get { return placedByDefault; } }

        public bool IsBuilt { set; get; } //Is the building built?
        public bool Placed { set; get; } //Has the building been placed on the map?

        //building components:
        [SerializeField]
        private Transform spawnPosition = null; //If the building allows to create unit, they will spawned in this position.
        public Vector3 GetSpawnPosition (LayerMask navMeshLayerMask) {
            return new Vector3(spawnPosition.position.x, TerrainManager.instance.SampleHeight(spawnPosition.position, radius, navMeshLayerMask), spawnPosition.position.z); //return the building's assigned spawn position
        }
        [SerializeField]
        private Transform gotoPosition = null; //The position that the new unit goes to from the spawn position.
        public Transform GotoPosition { set { gotoPosition = value; } get { return gotoPosition; } }
        public Transform RallyPoint { set; get; } //this can be set to the above GotoPosition transform or a building or a resource transform

        //Building components:
        public BuildingPlacer PlacerComp { private set; get; }
        public BuildingHealth HealthComp { private set; get; }
        public NavMeshObstacle NavObstacle { private set; get; } //this is the navigation obstacle component assigned to the building.
        public Collider BoundaryCollider { private set; get; } //this is the collider that define the building's zone on the map where other buildings are not allowed to be placed.
        public Border BorderComp { private set; get; }
        public BuildingDropOff DropOffComp { private set; get; }
        public WorkerManager WorkerMgr { private set; get; }
        public Portal PortalComp { private set; get; }
        public ResourceGenerator GeneratorComp { private set; get; }
        public BuildingAttack AttackComp { set; get; }

        public override void Awake ()
        {
            base.Awake();

            Type = FactionEntityTypes.building;

            //get the building-specifc components:
            PlacerComp = GetComponent<BuildingPlacer>();
            HealthComp = GetComponent<BuildingHealth>();
            NavObstacle = GetComponent<NavMeshObstacle>();
            BoundaryCollider = GetComponent<Collider>();
            BorderComp = GetComponent<Border>();
            DropOffComp = GetComponent<BuildingDropOff>();
            WorkerMgr = GetComponent<WorkerManager>();
            PortalComp = GetComponent<Portal>();
            GeneratorComp = GetComponent<ResourceGenerator>();
            AttackComp = GetComponent<BuildingAttack>();
        }

        public override void UpdateAttackComp(AttackEntity attackEntity) { AttackComp = (BuildingAttack)attackEntity; }

        public override void Start()
        {
            base.Start();
            if (BoundaryCollider == null) //if the building collider is not set.
                Debug.LogError("[Building]: The building parent object must have a collider to represent the building's boundaries.");
            else
                BoundaryCollider.isTrigger = true; //the building's main collider must always have "isTrigger" is true.

            RallyPoint = gotoPosition; //by default the rally point is set to the goto position
            if (gotoPosition != null) //Hide the goto position
                gotoPosition.gameObject.SetActive(false);

            if (Placed == false) //Disable the player selection collider object if the building has not been placed yet.
                selection.gameObject.SetActive(false);

            if (placedByDefault == false) //if the building is not placed by default.
                PlaneRenderer.material.color = Color.green; //start by setting the selection texture color to green which implies that it's allowed to place building at its position.

            if (placedByDefault) //if the building is supposed to be placed by default -> meaning that it is already in the scene
            {
                //place building and add max health to it.
                Init(factionID); //initiliaze the building
                PlacerComp.PlaceBuilding();
                HealthComp.AddHealthLocal(HealthComp.MaxHealth, null);
            }
            else if (free == true) //if this building is not supposed to be placed by default but it belongs to a free faction
            {
                Init(-1); //init it
                PlacerComp.PlaceBuilding(); //and place it
            }
        }

        //initialize placement instance:
        public void InitPlacementInstance(int factionID, Border buildingCenter = null)
        {
            Init(factionID); //initialize the building

            CurrentCenter = buildingCenter; //set the building center

            PlacerComp.Init(); //initiliaze the building placer component

            if(FactionID == GameManager.PlayerFactionID) //if this building belongs to the local player
                plane.SetActive(true); //Enable the building's plane.

            if (NavObstacle) //disable the nav mesh obstacle comp, if it exists
                NavObstacle.enabled = false;
        }

        //a method called when the building is fully constructed
        public void OnBuilt()
        {
            if (BorderComp) //if the building includes the border component
                if (BorderComp.IsActive == false) //if the border is not active yet.
                {
                    FactionMgr.BuildingCenters.Add(this); //add the building to the building centers list (the list that includes all buildings wtih a border comp)

                    BorderComp.Activate(); //activate the border
                    CurrentCenter = BorderComp; //make the building its own center.
                }

            if (DropOffComp != null) //if the building has a drop off component
                DropOffComp.Init(); //initiliaze it

            if (free == true) //if this is a free building then stop here
                return;

            GameManager.Instance.Factions[factionID].UpdateMaxPopulation(addPopulation); //update the faction population slots.

            ToggleResourceBonus(true); //apply the resource bonus

            if (gotoPosition != null) //If the building has a goto position
                if (SelectionManager.instance.IsBuildingSelected(this)) //Check if the building is currently selected
                    gotoPosition.gameObject.SetActive(true); //then show the goto pos
                else
                    gotoPosition.gameObject.SetActive(false); //hide the goto pos
        }

        //a method that updates resource bonuses for all resources inside the center where this building instance is
        public void ToggleResourceBonus(bool enable)
        {
            if (bonusResources.Length == 0 || CurrentCenter == null) //if there are no bonus resources or the building doesn't have a center
                return; //do not continue

            foreach (Resource r in CurrentCenter.GetResourcesInRange()) //go through the resources inside the current center
            {
                for (int i = 0; i < bonusResources.Length; i++) //for each bonus resource type
                {
                    if (r != null && r.GetResourceName() == bonusResources[i].GetResourceTypeName()) //if the resource is valid and the bonus resource matches this resource type
                    {
                        //add/remove the bonus depending on the value of "enable"
                        r.UpdateCollectOneUnitDuration(enable ? bonusResources[i].GetBonus() : -bonusResources[i].GetBonus());
                    }
                }
            }
        }

        //a method called to send a unit to the building's rally point
        public void SendUnitToRallyPoint(Unit unit)
        {
            if (unit == null || RallyPoint == null) //if the input unit is invalid or the rally point is invalid
                return; //do not proceed.

            Building buildingRallyPoint = RallyPoint.gameObject.GetComponent<Building>();
            Resource resourceRallyPoint = RallyPoint.gameObject.GetComponent<Resource>();

            //if the rallypoint is a building that needs construction and the unit has a builder component
            if (buildingRallyPoint && unit.BuilderComp && buildingRallyPoint.WorkerMgr.currWorkers < buildingRallyPoint.WorkerMgr.GetAvailableSlots())
                unit.BuilderComp.SetTarget(buildingRallyPoint); //send unit to construct the building
            
            //if the rallypoint is a resource that can still use collectors and the unit has a collector component
            else if (resourceRallyPoint && unit.CollectorComp && resourceRallyPoint.WorkerMgr.currWorkers < resourceRallyPoint.WorkerMgr.GetAvailableSlots())
                unit.CollectorComp.SetTarget(resourceRallyPoint);
            //if the rallypoint is just a position on the map
            else
                MovementManager.instance.Move(unit, RallyPoint.position, 0.0f, null, InputMode.movement, false); //move the unit there
        }

        //Draw the building's radius in blue
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
