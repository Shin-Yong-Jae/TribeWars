using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Building Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager instance = null;

        void Awake ()
        {
            //only one building manager component in the map
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(this);
        }

        //creates an instance of a building that is instantly placed:
        public static void CreatePlacedInstance(Building buildingPrefab, Vector3 placementPosition, float yEulerAngle, Border buildingCenter, int factionID, bool placedByDefault = false)
        {
            if (GameManager.MultiplayerGame == false)
            { //if it's a single player game.
                CreatePlacedInstanceLocal(buildingPrefab, placementPosition, yEulerAngle, buildingCenter, factionID, placedByDefault); //place the building
            }
            else
            { //in case it's a multiplayer game:

                //ask the server to spawn the building for all clients:
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.create,
                    targetMode = (byte)InputMode.building,
                    value = (placedByDefault == true) ? 1 : 0,
                    initialPosition = placementPosition,
                    targetPosition = new Vector3(0.0f,yEulerAngle, 0.0f)
                };
                InputManager.SendInput(newInput, buildingPrefab.gameObject, buildingCenter == null ? null : buildingCenter.gameObject); //send input to input manager
            }
        }

        //creates an instance of a building that is instantly placed:
        public static Building CreatePlacedInstanceLocal(Building buildingPrefab, Vector3 placementPosition, float yEulerAngle, Border buildingCenter, int factionID, bool placedByDefault = false)
        {
            Vector3 buildingEulerAngles = buildingPrefab.transform.rotation.eulerAngles;
            buildingEulerAngles.y = yEulerAngle; //set the rotation of the building

            Building buildingInstance = Instantiate(buildingPrefab.gameObject, placementPosition, Quaternion.Euler(buildingEulerAngles)).GetComponent<Building>(); //create instance
            buildingInstance.CurrentCenter = buildingCenter; //set building cenetr.
            buildingInstance.Init(factionID);

            if (placedByDefault == false) //if it's this placed by default
                buildingInstance.PlacerComp.PlaceBuilding();
            else
            {
                buildingInstance.FactionID = factionID;
                buildingInstance.PlacedByDefault = true;
            }

            return buildingInstance;
        }

        //filter a building list depending on a certain code
        public static List<Building> FilterBuildingList(List<Building> buildingList, string code)
        {
            //result list:
            List<Building> filteredBuildingList = new List<Building>();
            //go through the input building list:
            foreach (Building b in buildingList)
            {
                if (b.GetCode() == code) //if it has the code we need
                    filteredBuildingList.Add(b); //add it
            }

            return filteredBuildingList;
        }

        //get the closest building of a certain type out of a list to a given position
        public static Building GetClosestBuilding (Vector3 pos, List<Building> buildings, List<string> codes = null)
        {
            Building resultBuilding = null;
            float lastDistance = 0;

            //go through the buildings to search
            foreach(Building b in buildings)
            {
                //if the building has a valid code (or there's no code to be checked) and is the closest so far.
                if((codes == null || codes.Contains(b.GetCode())) && (resultBuilding == null || Vector3.Distance(b.transform.position, pos) < lastDistance))
                {
                    resultBuilding = b;
                    lastDistance = Vector3.Distance(b.transform.position, pos);
                }
            }

            return resultBuilding;
        }
    }
}
