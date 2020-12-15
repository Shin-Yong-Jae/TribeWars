using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

/* Resource Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class ResourceManager : MonoBehaviour {

        public static ResourceManager instance; //we'll have the one instance of this component here so it's easier to access from other components.

        public GameObject ResourcesParent; //All resources must be placed as children of the same object.
        [System.Serializable]
        //This array appears in the inspector, it's where you can create the resources types:
        public class ResourcesVars
        {
            public ResourceTypeInfo TypeInfo;

            [HideInInspector]
            public int Amount; //The current amount of this resource.

            //UI:
            //show UI:
            public bool ShowUI = true; //show this resource in the dash board? 

            public Image UIImage; //Resource UI image
            public Text UIText; //Resource UI text to display the resource amount.

            //NPC Resource Tasks:
            [HideInInspector]
            public int TargetAmount = 0; //the target amount that the faction wants to reach.
            [HideInInspector]
            public int LastCenterID = 0; //Whenever a resource is missing, we start searching for it from a city center. This variable holds the last ID of the city center that we started the search from.
        }
        public ResourcesVars[] ResourcesInfo;

        public bool AutoCollect = true; //Collect resources automatically when true. if false, the unit must drop off the collected resources each time at a building that allow that.

        //Selection color the resources:
        public Color ResourceSelectionColor;

        //This array doesn't appear in the inspector, its values are set by the game manager depending on the number of teams:
        [System.Serializable]
        public class FactionResourcesVars
        {
            public ResourcesVars[] ResourcesTypes; //For each team, we'll associate all the resources types.

            // >= 1.0f, when faction needs to spend amount X of a resource, it must have X * resourceExploitRatio from that resource
            private float resourceNeedRatio = 1.0f;

            public float GetResourceNeedRatio() //get the value of the resource need ratio
            {
                return resourceNeedRatio;
            }

            public void SetResourceNeedRatio(float value) //modify the resource need ratio
            {
                //the resource need ratio must be always >= 1.0
                if (value < 1.0f)
                    return;
                resourceNeedRatio = value;
            }
        }
        [HideInInspector]
        public FactionResourcesVars[] FactionResourcesInfo;

        //Resources class:
        [System.Serializable]
        public class Resources
        {
            public string Name;
            public int Amount;
        }

        //All resources:
        public List<Resource> AllResources {private set; get;}

		//in order to set the faction IDs of resources after they spawn, we need to register the amount of resources we have in the scene in ..
		[HideInInspector]
		public int ResourcesAmount;
		//then when the spawned resources amount reaches the value above, all centers will look to set the faction IDs for resources.

		[HideInInspector]
		public GameManager gameMgr;

        private void Awake()
        {
            //assign the instance:
            if (instance == null)
                instance = this;
            else if (instance != this) //if we have another already active instance of this component
                Destroy(this); //destroy this instance then

            AllResources = new List<Resource>(); //go through all the resources that are children of the Resources Parent game object and init them by adding them to the all resources list
            AllResources.AddRange(ResourcesParent.transform.GetComponentsInChildren<Resource>(true));
            if (AllResources.Count > 0)
                foreach (Resource Element in AllResources)
                {
                    Element.FactionID = -1;
                    Element.WorkerMgr = Element.gameObject.GetComponent<WorkerManager>();
                }
        }

        //a method that initializes resources for factions.
        public void InitFactionResources ()
        {
            //make sure that the game manager is set:
            if(gameMgr == null)
            {
                Debug.LogError("[Resource Manager]: The Game Manager hasn't been set.");
                return; 
            }

            if (gameMgr.Factions.Count > 0) //if there are actual factions to begin with:
            {
                //Create as many resource info slots as the amount of the spawned factions.
                FactionResourcesInfo = new FactionResourcesVars[gameMgr.Factions.Count];

                //Loop through all the factions:
                for (int i = 0; i < gameMgr.Factions.Count; i++)
                {
                    FactionResourcesInfo[i] = new ResourceManager.FactionResourcesVars(); //init the faction resource var
                    //set the resource need ratio value to 1.0f if this is a player faction
                    if (gameMgr.Factions[i].playerControlled == true)
                        FactionResourcesInfo[i].SetResourceNeedRatio(1.0f);
                    //Associate each faction with all available resources:
                    FactionResourcesInfo[i].ResourcesTypes = new ResourcesVars[ResourcesInfo.Length];

                    //Loop through all the available resources and define them for each team.
                    for (int j = 0; j < FactionResourcesInfo[i].ResourcesTypes.Length; j++)
                    {
                        FactionResourcesInfo[i].ResourcesTypes[j] = new ResourcesVars()
                        {
                            TypeInfo = ResourcesInfo[j].TypeInfo, //resource typue.
                            Amount = ResourcesInfo[j].TypeInfo.StartingAmount //Starting amount of the resource for each team.
                        };
                    }
                }
            }

            UpdateResourcesUI(); //right after setting up the resource settings above, refresh the resource UI.
        }

		//a method that adds amount to a faction's resources.
		public void AddResource(int FactionID, string Name, int Amount)
		{
			int ResourceID = GetResourceID (Name);
			if(ResourceID >= 0) //Checking if the resource ID is valid.
			{
				//Add the resource amount.
				FactionResourcesInfo[FactionID].ResourcesTypes[ResourceID].Amount += Amount;
				if(FactionID == GameManager.PlayerFactionID) UpdateResourcesUI(); //Update the resource UI if the team ID corressponds to the team controlled by the player.
			}
			else
			{
				return;
			}
		}

        //a method that gets the resource amount by providing the faction ID and name of the resource.
		public int GetResourceAmount (int FactionID, string Name)
		{
			int ResourceID = GetResourceID (Name);
			if(ResourceID >= 0) //Checking if the resource ID is valid.
			{
				//Get the resource amount
				return FactionResourcesInfo[FactionID].ResourcesTypes[ResourceID].Amount;
			}
			else
			{
				return -1;
			}
		}

        //a method that gets the ID of a resource.
		public int GetResourceID(string Name)
		{
			//Search for the resource ID using its name.
			for(int i = 0; i < ResourcesInfo.Length; i++)
			{
				if(ResourcesInfo[i].TypeInfo.Name == Name)
				{
					return i;
				}
			}
			return -1;
		}

        //a method that updates he resources UI.
		public void UpdateResourcesUI()
		{
			//Update the resources UI:
			for(int i = 0; i < ResourcesInfo.Length; i++)
			{
				if (ResourcesInfo [i].ShowUI == true) {
					if (ResourcesInfo [i].UIImage)
						ResourcesInfo [i].UIImage.sprite = ResourcesInfo [i].TypeInfo.Icon;
					if (ResourcesInfo [i].UIText && GameManager.PlayerFactionID >= 0) {
						ResourcesInfo [i].UIText.text = FactionResourcesInfo [GameManager.PlayerFactionID].ResourcesTypes [i].Amount.ToString ();
					}
				}
			}
		}

		//Check resources:
		//This checks if the faction have enough resources to launch a task:
		public bool CheckResources (ResourceManager.Resources[] RequiredResources, int FactionID)
		{
            //if this is the local player and god mode is enabled
            if (FactionID == GameManager.PlayerFactionID && GodMode.Enabled == true)
            {
                //proceed no matter how much resources there is
                return true;
            }

            if (RequiredResources.Length > 0)
			{
				for(int i = 0; i < RequiredResources.Length; i++) //Loop through all the requried resources:
				{
					//Check if the team resources are lower than one of the demanded amounts:
					if(GetResourceAmount(FactionID,RequiredResources[i].Name) < RequiredResources[i].Amount*FactionResourcesInfo[FactionID].GetResourceNeedRatio())
					{
						return false; //If yes, return false.
					}
				}
				return true; //If not, return true.
			}
			else //This means that no resource are required.
			{
				return true;
			}
		}

		//this method gives back the resource of a task to the faction:
		public void GiveBackResources (ResourceManager.Resources[] RequiredResources, int FactionID)
		{
			if(RequiredResources.Length > 0)
			{
				for(int i = 0; i < RequiredResources.Length; i++) //Loop through all the requried resources:
				{
					//Give back the resources:
					AddResource(FactionID, RequiredResources[i].Name, RequiredResources[i].Amount);
				}
			}
		}

		//this method takes the resources of a task from a faction
		public void TakeResources (ResourceManager.Resources[] RequiredResources, int FactionID)
		{
            //if this is the local player and god mode is enabled 
            if (FactionID == GameManager.PlayerFactionID && GodMode.Enabled == true)
            {
                //take no resources
                return;
            }

            if (RequiredResources.Length > 0) //If the building requires resources:
			{
				for(int i = 0; i < RequiredResources.Length; i++) //Loop through all the requried resources:
				{
					//Remove the demanded resources amounts:
					AddResource(FactionID, RequiredResources[i].Name, -RequiredResources[i].Amount);
				}
			}
		}

        //a method that spawns a resource instance:
        public static void CreateResource(Resource resourcePrefab, Vector3 spawnPosition)
        {
            if (resourcePrefab == null) //invalid prefab
                return;

            if (GameManager.MultiplayerGame == false) //single player game:
                CreateResourceLocal(resourcePrefab, spawnPosition);
            else
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.create,
                    targetMode = (byte)InputMode.resource,

                    initialPosition = spawnPosition
                };
                InputManager.SendInput(newInput, resourcePrefab.gameObject, null);
            }
        }

        //a method that creates a resource instance locally:
        public static Resource CreateResourceLocal (Resource resourcePrefab, Vector3 spawnPosition)
        {
            if (resourcePrefab == null) //invalid prefab
                return null;

            Resource newResource = Instantiate(resourcePrefab.gameObject, spawnPosition, resourcePrefab.transform.rotation).GetComponent<Resource>(); //spawn the new resource
            newResource.Init(); //initiate resource settings

            //we need to determine whether the resource has spawned in an area controlled by a faction or not
            int i = 0;
            while (i < GameManager.Instance.AllBorders.Count && newResource.FactionID == -1) //borders that were placed first will have priority over borders that were placed later
            {
                GameManager.Instance.AllBorders[i].CheckResource(newResource);
                i++;
            }

            return newResource;
        }

		//loading resources in multiplayer and singleplayer games:
		void Start ()
		{
            ResourcesAmount = AllResources.Count;
		}

        //register the resource in this map:
        public void RegisterResource (Resource NewResource)
		{
			//add it to the list:
			AllResources.Add (NewResource);

			//if all resources are spawned:
			if (AllResources.Count == ResourcesAmount) {
				//ask spawned building centers (borders) to check for resources in order to set their faction ID:

				//go through all factions:
				if (gameMgr.Factions.Count > 0) {
					for (int i = 0; i < gameMgr.Factions.Count; i++) {
						if (gameMgr.Factions [i].FactionMgr.BuildingCenters.Count > 0) { //if the current faction actually has building centers
							foreach (Building Center in gameMgr.Factions [i].FactionMgr.BuildingCenters) {
								Center.CurrentCenter.CheckAllResources (); //reload the border resources here.
							}
						}
					}
				}
			}
		}

        //static help methods regarding resources:

        //filter a resource list depending on a certain name
        public static List<Resource> FilterResourceList(List<Resource> resourceList, string name)
        {
            //result list:
            List<Resource> filteredResourceList = new List<Resource>();
            //go through the input resource list:
            foreach(Resource r in resourceList)
            {
                if (r.GetResourceName() == name) //if it has the name we need
                    filteredResourceList.Add(r); //add it
            }

            return filteredResourceList;
        }
	}
}