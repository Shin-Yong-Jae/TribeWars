using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

/* Border script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(Building))]
    public class Border : MonoBehaviour
    {
        public Building building { private set; get; } //the main building component for which this component opeartes

        public bool IsActive { private set; get; } //is the border active or not?

        [Header("Border Object:")]
        [SerializeField]
        private bool spawnObj = true; //spawn the border object?
        [SerializeField]
        private GameObject obj; //Use an object that is only visible on the terrain to avoid drawing borders outside the terrain.
        public GameObject Obj { set { obj = value; } get { return obj; } }
        [SerializeField]
        private float height = 20.0f; //The height of the border object here
        [Range(0.0f, 1.0f), SerializeField]
        private float colorTransparency = 1.0f; //transparency of the border's object color
        [SerializeField]
        private float size = 10.0f; //The size of the border around this building.
        public float Size { set { size = value; } get { return size; } }
        [SerializeField]
        private float sizeMultiplier = 2.0f; //To control the relation of the border obj's actual size and the border's map. Using different textures for the border objects will require using 

        //If the border belongs to the player, then this array will only represent the maximum amounts for each building
        //and if a building is not in this array, then the player is free to build as many as he wishes to build.
        //As for NPC Factions, this is handled by NPC components
        [System.Serializable]
        public class BuildingInBorder
        {
            [SerializeField]
            private Building prefab = null; //prefab of the building to be placed inside the border
            public string GetPrefabCode () { return prefab.GetCode(); }
            [SerializeField]
            private FactionTypeInfo factionType = null; //Leave empty if you want this building to be placed by all factions
            public string GetFactionCode () { return factionType.Code; }

            private int currAmount; //current amount of the building type inside this border
            public void UpdateCurrAmount (bool inc) { currAmount += (inc == true) ? 1 : -1; }
            [SerializeField]
            private int maxAmount = 10; //maximum allowed amount of this building type inside this border
            public bool IsMaxAmountReached () { return currAmount >= maxAmount; }
        }

        [Header("Border Buildings:"), SerializeField]
        private List<BuildingInBorder> buildingsInBorder = new List<BuildingInBorder>(); //a list of buildings that are defined to be placed inside this border

        private List<Building> buildingsInRange = new List<Building>(); //a list of the spawned buildings inside the territory defined by this border
        public List<Building> GetBuildingsInRange () { return new List<Building>(buildingsInRange); }
        private List<Resource> resourcesInRange = new List<Resource>(); //a list of the resources inside the territory defined by this border
        public List<Resource> GetResourcesInRange () {
            return new List<Resource>(resourcesInRange);
        }

        //called to activate the border
        public void Activate()
        {
            //if the border is already active
            if (IsActive == true)
                return; //do not proceed

            building = gameObject.GetComponent<Building>(); //get the building that is controlling this border component

            if (spawnObj == true) //if it's allowed to spawn the border object
            {
                obj = (GameObject)Instantiate(obj, new Vector3(transform.position.x, height, transform.position.z), Quaternion.identity); //create the border obj
                obj.transform.localScale = new Vector3(Size * sizeMultiplier, obj.transform.localScale.y, Size * sizeMultiplier); //set the correct size for the border obj
                obj.transform.SetParent(transform, true); //make sure it's a child object of the building main object

                Color FactionColor = GameManager.Instance.Factions[building.FactionID].FactionColor; //set its color to the faction that it belongs to
                obj.GetComponent<MeshRenderer>().material.color = new Color(FactionColor.r, FactionColor.g, FactionColor.b, colorTransparency); //set the color transparency

                obj.GetComponent<MeshRenderer>().sortingOrder = GameManager.Instance.LastBorderSortingOrder; //set the border object's sorting order according to the previosuly placed borders
                GameManager.Instance.LastBorderSortingOrder--;
            }

            GameManager.Instance.AllBorders.Add(this); //add this border to the all borders list

            CheckAllResources(); //check the resources around the border

            IsActive = true; //mark the border as active

            CustomEvents.instance.OnBorderActivated(this); //trigger custom event
        }

        //called to deactivate this border:
        public void Deactivate ()
        {
            if (IsActive == false) //if the border isn't even active:
                return; //do not proceed.

            foreach (Resource r in resourcesInRange) //free the resources inside this border
                r.FactionID = -1;

            resourcesInRange.Clear();

            GameManager.Instance.AllBorders.Remove(this); //Remove the border from the all borders list

            //Go through all the borders' centers to refresh the resources inside this border (as one of the freed resources above could now belong to another center):
            foreach (Border b in GameManager.Instance.AllBorders)
                b.CheckAllResources(); //borders that were placed first will have priority over borders that were placed later

            building.FactionMgr.BuildingCenters.Remove(building); //Remove the border's main building from the building centers list in the faction manager

            CustomEvents.instance.OnBorderDeactivated(this); //trigger custom event

            //Destroy the border object if it has been created
            if (spawnObj == true)
                Destroy(obj);
        }

        //called to check all resources inside the range of the border
        public void CheckAllResources()
        {
            foreach (Resource r in ResourceManager.instance.AllResources) //go through all the resources in the map
                CheckResource(r);
        }

        //called to check if one resource is inside the range of the border or not
        public void CheckResource(Resource r)
        {
            if (!resourcesInRange.Contains(r) && r.FactionID == -1 && Vector3.Distance(r.transform.position, transform.position) < Size) //if this resource is not already in this border, wasn't claimed by another faction and is close enough
            {
                resourcesInRange.Add(r); //add it to the resources in range list
                r.FactionID = building.FactionID; //mark it as belonging to this faction ID
            }
        }

        //register a new building in this border
        public void RegisterBuilding(Building newBuilding)
        {
            buildingsInRange.Add(newBuilding); //add the new building to the list
            foreach (BuildingInBorder bir in buildingsInBorder) //go through all buildings in border slots
                if (bir.GetPrefabCode() == newBuilding.GetCode()) //if the code matches
                    bir.UpdateCurrAmount(true); //increase the current amount
        }

        //unregister an old building from this border
        public void UnegisterBuilding(Building oldBuilding)
        {
            buildingsInRange.Remove(oldBuilding); //remove the building from the list
            foreach (BuildingInBorder bir in buildingsInBorder) //go through all buildings in border slots
                if (bir.GetPrefabCode() == oldBuilding.GetCode()) //if the code matches
                    bir.UpdateCurrAmount(false); //decrease the current amount
        }

        //check if a building is allowed inside this border or not (using the building's code
        public bool AllowBuildingInBorder(string code)
        {
            foreach (BuildingInBorder bir in buildingsInBorder) //go through all buildings in border slots
                if (bir.GetPrefabCode() == code) //if the code matches
                    return !bir.IsMaxAmountReached(); //allow if the current amount still hasn't reached the max amount

            return true; //if the building type doesn't have a defined slot in the buildings in border list, then it can be definitely accepted.
        }

        //check all buildings placed in the range of this border
        public void CheckBuildings()
        {
            int i = 0;
            while (i < buildingsInBorder.Count) //go through the buildings in border slots
                if (buildingsInBorder[i].GetFactionCode() != "" && (GameManager.Instance.Factions[building.FactionID].TypeInfo == null || GameManager.Instance.Factions[building.FactionID].TypeInfo.Code != buildingsInBorder[i].GetFactionCode())) //if the faction code is specified and doesn't match the building's faction
                    buildingsInBorder.RemoveAt(i); //remove this slot
                else
                    i++;
        }
    }
}