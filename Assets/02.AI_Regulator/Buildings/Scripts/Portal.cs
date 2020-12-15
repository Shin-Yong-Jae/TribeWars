using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Portal script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(Building))]
	public class Portal : MonoBehaviour {

        public Building building { private set; get; } //the main building component for which this component opeartes

        [SerializeField]
        private Transform spawnPosition = null; //the position where units come out of this portal
        public Vector3 GetSpawnPosition () { return spawnPosition.position; }
        [SerializeField]
        private Transform gotoPosition = null; //if there's a goto pos, then the unit will move to this position when they spawn.

        [SerializeField]
		private Portal targetPortal = null; //the target's portal that this portal teleports to.

        [SerializeField]
        private bool allowAllUnits = true; //does this portal allow all units? if this is true, then the two next attributes can be ignored
        [SerializeField]
        private bool allowInListOnly = false; //if the above option is disabled, then when this is true, only unit types with the codes in the list below will be allowed
        //...however, if set to false, then all unit types but the ones specified in the list below will be allowed.
        [SerializeField]
        private List<string> codesList = new List<string>(); //a list of the allowed unit codes that are not allowed to use the portal

        //audio clips:
        [SerializeField]
        private AudioClip teleportAudio = null; //audio clip played when a unit enters this portal

		//double clicking on the portal changes the camera view to the target portal
		private float doubleClickTimer;
		private bool clickedOnce = false;

        void Awake ()
        {
            building = GetComponent<Building>(); //get the main building's component
        }

		void Start () {
            if (spawnPosition == null)
                Debug.LogError("[Portal]: You must assign a spawn position (transform) for the portal to spawn units at");

            //initial settings for the double click
            clickedOnce = false;
			doubleClickTimer = 0.0f;
		}

		void Update ()
		{
			//double click timer:
			if (clickedOnce == true) {
				if (doubleClickTimer > 0)
                    doubleClickTimer -= Time.deltaTime;
                if (doubleClickTimer <= 0)
                    clickedOnce = false;
            }
		}

        //a method that teleports a unit from a portal to another
		public void Teleport (Unit unit)
		{
            if(targetPortal == null || targetPortal.spawnPosition == null) //if this portal doesn't have a target portal
            {
                UIManager.instance.ShowPlayerMessage("This portal doesn't have a target portal!", UIManager.MessageTypes.Error); //let the player know that this portal doesn't have a target.
                return; //do not continue
            }

            unit.gameObject.SetActive(false); //deactivate the unit's object
            unit.transform.position = targetPortal.spawnPosition.position; //move the unit to the target portal's spawn position
            unit.gameObject.SetActive(true); //activate the unit's object again

            CustomEvents.instance.OnUnitTeleport(this, targetPortal, unit); //trigger custom events

            //if the target portal has a goto position, move the unit there
            if(targetPortal.gotoPosition)
                MovementManager.instance.Move(unit, targetPortal.gotoPosition.position, 0.0f, null, InputMode.movement, false);
        }

        //a method that is called when a mouse click on this portal is detected
        public void OnMouseClick ()
		{
            if (clickedOnce == false)
            { //if the player hasn't clicked on this portal shortly before this click
                doubleClickTimer = 0.5f; //launch the double click timer
                clickedOnce = true; //change to true to mark that the second click (to finish the double click) is awaited
            }
            else if (targetPortal != null)
            { //if this is the second click (double click)
                CustomEvents.instance.OnPortalDoubleClick(this, targetPortal, null); //trigger the custom event

                AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, teleportAudio, false); //play the teleport audio clip
                CameraMovement.instance.LookAt(targetPortal.transform.position); //change the main camera's view to look at the target portal
                CameraMovement.instance.SetMiniMapCursorPos(targetPortal.transform.position); //change the minimap's cursor position to be over the target portal
            }
		}

        //can a certain faction use this portal? 
        public bool CanUsePortal (int factionID)
        {
            //a unit can use the portal only if it has the same faction ID as the portal or if the portal is a free building
            return building.FactionID == factionID || building.IsFree() == true;
        }

		//What units are allowed through this portal?
		public bool IsUnitAllowed (Unit unit)
		{
			if (allowAllUnits) //if all units are allowed, then yes
				return true;
            //depending on the portal's settings, see if the unit is allowed through the portal or not
            return (allowInListOnly == true && codesList.Contains(unit.GetCode())) || (allowInListOnly == false && !codesList.Contains(unit.GetCode()));
		}
	}
}