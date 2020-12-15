using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

/* Resource script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class Resource : MonoBehaviour
    {

        [Header("General"), SerializeField]
        private ResourceTypeInfo resourceType = null; //the resoruce type that defines this resource object
        public string GetResourceName() { return resourceType.Name; }

        [SerializeField]
        private float radius = 2.0f; //the radius of this resource that defines where units can interact with it
        public float GetRadius() { return radius; }

        public int ID { private set; get; } //the resource ID of this resource type in the resource manager.

        [SerializeField]
        private bool infiniteAmount = false; //when true, the resource will never get empty
        public bool IsInfinite() { return infiniteAmount; }
        [SerializeField]
        private int amount = 1000; //amount that's available to be collected
        public int GetAmount() { return amount; }
        public bool IsEmpty() { return amount <= 0 && infiniteAmount == false; }

        [SerializeField]
        private bool destroyOnEmpty = true; //destroy the resource object once the amount hits zero ?

        public int FactionID { set; get; } //the ID of the faction that includes this resource object inside its territory

        [SerializeField]
        private GameObject plane = null; //appears when the resource is selected.
        [SerializeField]
        private ResourceSelection selection = null; //Must be an object that only include this script, a trigger collider and a kinematic rigidbody.

        [SerializeField]
        private GameObject initialModel = null; //the initial model of the resource that is visible before a unit starts exploiting this resource
        [SerializeField]
        private GameObject secondaryModel = null; //when assigned, this model is enabled as soon as a unit starts exploiting this resource. the initial model will be then hidden.
        [SerializeField]
        private ResourceSelection secondarySelection = null; //if assigned, it replaces the selection field when the secondaryModel gameobject is enabled

        private bool collected = false;

        [Header("Collection & Drop Off"), SerializeField]
        private bool canCollectOutsideBorder = false; //can the player collect this resource outside the borders?
        public bool CanCollectOutsideBorder() { return canCollectOutsideBorder; }

        [SerializeField]
        private float collectOneUnitDuration = 1.5f; //how much time is needed to collect 1 from the resource amount.
        public float GetCollectOneUnitDuration() { return collectOneUnitDuration; }
        public void UpdateCollectOneUnitDuration(float value) { collectOneUnitDuration += value; }

        [Header("UI")]
        [SerializeField]
        private bool showCollectors = true; //show the current collectors in the UI.
        public bool ShowCollectors() { return showCollectors; }
        [SerializeField]
        private bool showAmount = true; //show the amount in the UI.
        public bool ShowAmount() { return showAmount; }

        public Treasure TreasureComp { private set; get; } //the treasure component attached to this resource (if there's any)
        public WorkerManager WorkerMgr { set; get; } //this component manages resource collectors

        private bool initiated = false;

        private void Awake()
        {
            TreasureComp = GetComponent<Treasure>();
            WorkerMgr = GetComponent<WorkerManager>();

            if (initiated == false) //check if the resource hasn't been initiated yet (because other components can init the resource when creating it before it gets to be initiated here).
                Init();
        }

        public void Init()
        {
            initiated = true; //mark as initiated.

            FactionID = -1; //set faction ID to -1 by default.

            plane.gameObject.SetActive(false); //hide the resource's plane initially

            ID = ResourceManager.instance.GetResourceID(resourceType.Name); //get the resource ID from the resource manager

            if (selection != null) //Set the selection object if we're using a different collider for player selection
            {
                selection.UpdateMainObject(gameObject); //set the player selection object for this resource
                MinimapIconManager.instance.AssignIcon(selection); //ask the minimap icon manager to create the a minimap icon for this resource
            }
            else
                Debug.LogError("[Resource]: The Selection component is missing.");

            if (secondaryModel != null) //if there's a secondary model, enable it and disable the initial one
            {
                secondaryModel.SetActive(false);

                if (secondarySelection != null) //if there's secondary selection
                {
                    secondarySelection.UpdateMainObject(gameObject); //set the player selection object for this resource
                    secondarySelection.gameObject.SetActive(false); //and hide it
                }
            }

            collectOneUnitDuration /= GameManager.Instance.GetSpeedModifier(); //set collection time regarding speed modifier

            initialModel.SetActive(true);

            ResourceManager.instance.RegisterResource(this); //register the resource in the resource manager
        }

        private void Update()
        {
            if (isFlashActive == false)
                return;

            //selection plane flash timer:
            if (flashTimer > 0)
                flashTimer -= Time.deltaTime;
            else
                DisableSelectionFlash();
        }

        float flashTimer;
        bool isFlashActive = false; //is the selection flash currently flashing?

        //a method to enable the selection flash
        public void EnableSelectionFlash(float duration, float repeatTime, Color color)
        {
            plane.GetComponent<Renderer>().material.color = color; //set the flashing color first
            InvokeRepeating("SelectionFlash", 0.0f, repeatTime);
            flashTimer = duration;
            isFlashActive = true;
        }

        //a method to disable the selection flash
        public void DisableSelectionFlash()
        {
            CancelInvoke("SelectionFlash");
            plane.SetActive(false); //hide the selection plane.
            isFlashActive = false;
        }

        //flashing faction entity selection
        public void SelectionFlash()
        {
            plane.SetActive(!plane.activeInHierarchy);
        }

        //enabling the faction entity's plane when selected:
        public void EnableSelectionPlane(Texture selectionTexture, Color color)
        {
            plane.SetActive(true); //Activate the plane object where we will show the selection texture.

            plane.GetComponent<Renderer>().material.mainTexture = selectionTexture; //Show the selection texture and set its color.
            plane.GetComponent<Renderer>().material.color = new Color(color.r, color.g, color.b, 0.5f); //update the plane's color
        }

        //disable the faction entity's selection plane:
        public void DisableSelectionPlane()
        {
            plane.SetActive(false);
        }

        //add/remove to/from the resource
        public void AddAmount(int value, Unit source)
        {
            if (GameManager.MultiplayerGame == false) //if this is a single player game -> go ahead directly
                AddAmountLocal(value, source);
            else if (GameManager.Instance.IsLocalPlayer(source.gameObject)) //multiplayer game and the resource collector belongs to the local faction
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.resource,
                    targetMode = (byte)InputMode.health,
                    value = value
                };
                InputManager.SendInput(newInput, gameObject, (source == null) ? null : source.gameObject);
            }
        }

        //add/remove to/from the resource locally
        public void AddAmountLocal(int value, Unit source)
        {
            if (collected == false) //if the resource hasn't been collected before now
            {
                collected = true;

                if (secondaryModel != null) //if there's a secondary model, enable it and disable the initial one
                {
                    initialModel.SetActive(false);
                    secondaryModel.SetActive(true);

                    if(secondarySelection != null) //if there's a secondary selection
                    {
                        selection.DisableMinimapIcon(); //disable the first minimap icon
                        selection.gameObject.SetActive(false); //disable the selection object

                        //enable the secondary one:
                        secondarySelection.gameObject.SetActive(true);

                        selection = secondarySelection;
                        MinimapIconManager.instance.AssignIcon(selection); //ask the minimap icon manager to create the a minimap icon for this resource and link it to the secondary selection
                    }
                }

            }

            if (infiniteAmount == false) //only change the amount if the resource doesn't have infinite amount
            {
                amount += Mathf.FloorToInt(value);
                if (SelectionManager.instance.IsResourceSelected(this)) //if resource object is selected
                    SelectionManager.instance.UIMgr.UpdateResourceUI(this); //update UI
            }

            if (ResourceManager.instance.AutoCollect == true) //if resources are automatically collected
                ResourceManager.instance.AddResource(source.FactionID, GetResourceName(), -value); //then add the resource to the faction.
            else //resources need to dropped off at a building
                source.CollectorComp.UpdateDropOffResources(ID, -value);

            if (amount <= 0 && infiniteAmount == false) //if the resource is empty
                DestroyResource(source);
        }

        //a method called when the resource object is to be destroyed
        public void DestroyResource(Unit source)
        {
            if (GameManager.MultiplayerGame == false) //if this is a single player game -> go ahead directly
                DestroyResourceLocal(source);
            else if (GameManager.Instance.IsLocalPlayer(source.gameObject)) //multiplayer game and the resource collector belongs to the local faction
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.destroy,
                    targetMode = (byte)InputMode.resource,
                };
                InputManager.SendInput(newInput, gameObject, (source == null) ? null : source.gameObject);
            }
        }

        //a method called when the resource object is to be locally destroyed
        public void DestroyResourceLocal(Unit source)
        {
            amount = 0; //empty the resource
            CustomEvents.instance.OnResourceEmpty(this); //trigger custom event

            source.CollectorComp.Stop(); //stop the unit from collecting.

            if (destroyOnEmpty == false) //if the resource is not supposed to be destroyed
                return;

            if (TreasureComp) //if this has a treasure component
                TreasureComp.Trigger(source.FactionID); //trigger the treasure for the collector's faction

            GameManager.Instance.ResourceMgr.AllResources.Remove(this); //remove resource from all resources list

            selection.DisableMinimapIcon(); //remove resource's minimap icon from the minimap

            Destroy(gameObject);
        }

        //Draw the resource's radius in blue
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}