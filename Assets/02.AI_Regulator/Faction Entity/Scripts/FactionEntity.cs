using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public enum FactionEntityTypes { unit, building };

    public abstract class FactionEntity : MonoBehaviour
    {
        public FactionEntityTypes Type { protected set; get; }

        [SerializeField]
        private string _name = "entity_name"; //the name of the faction entity that will be displayd when it is selected.
        public string GetName() { return _name; }
        [SerializeField]
        private string code = "entity_code"; //unique code for each faction entity that is used to identify it in the system.
        public string GetCode () { return code; }
        [SerializeField]
        private string category = "entity_category"; //the category that this faction entity belongs to.
        public string GetCategory() { return category; }
        [SerializeField]
        private string description = "entity_description"; //the description of the faction entity that will be displayed when it is selected.
        public string GetDescription() { return description; }

        [SerializeField]
        private Sprite icon = null; //the icon that will be displayed when the faction entity is selected.
        public Sprite GetIcon() { return icon; }

        [SerializeField]
        protected bool free = false; //does this entity belong to no faction?
        public bool IsFree() { return free; }
        public void SetFree (bool free) { this.free = free; }
        [SerializeField]
        protected int factionID = 0; //the faction ID that this entity belongs to.
        public int FactionID { set { factionID = value; } get { return factionID; } }
        public FactionManager FactionMgr { set; get; } //the faction manager that this entity belongs to.

        [System.Serializable]
        public struct ColoredRenderer
        {
            [SerializeField]
            private Renderer renderer;
            [SerializeField]
            private int materialID;

            //a method that updates the renderer's material color
            public void UpdateColor (Color color)
            {
                renderer.materials[materialID].color = color;
            }
        }
        [SerializeField]
        private ColoredRenderer[] coloredRenderers = new ColoredRenderer[0]; //The materials of the assigned Renderer components in this array will be colored by the faction entity's faction color

        //Audio:
        [SerializeField]
        protected AudioClip selectionAudio = null; //Audio played when the building is selected.
        public AudioClip GetSelectionAudio() { return selectionAudio; }

        //Faction entity components:
        [SerializeField]
        protected GameObject plane = null;

        [SerializeField]
        private GameObject model = null; //the faction entity's model goes here.
        public void ToggleModel(bool show) { model.SetActive(show); } //hide the faction entity's model

        [SerializeField]
        protected SelectionEntity selection = null; //The selection object of the faction entity goes here.
        public SelectionEntity GetSelection () { return selection; }

        //other components that can be attached to a faction entity:
        public Renderer PlaneRenderer { private set; get; } //this is the Renderer component of the plane's object.
        public TaskLauncher TaskLauncherComp { private set; get; }
        public APC APCComp { private set; get; }
        public MultipleAttackManager MultipleAttackMgr { private set; get; }
        public FactionEntityHealth EntityHealthComp { private set; get; }

        public virtual void Awake()
        {
            //get the components that are attached to the faction entity:
            PlaneRenderer = plane.GetComponent<Renderer>(); //get the faction entity's plane renderer here
            TaskLauncherComp = GetComponent<TaskLauncher>();
            APCComp = GetComponent<APC>();
            MultipleAttackMgr = GetComponent<MultipleAttackManager>();
            EntityHealthComp = GetComponent<FactionEntityHealth>();

            selection.FactionEntity = this;

            gameObject.layer = 2; //set the layer to IgnoreRaycast as we don't want any raycast to recongize this.

            if (plane == null) //if the selection plane is not available.
                Debug.LogError("[Faction Entity]: You must attach a plane object for the faction entity and assign it to 'Plane' in the inspector.");
            else
                plane.SetActive(false);

            if (selection != null) //Set the selection object if we're using a different collider for player selection
                selection.UpdateMainObject(gameObject);
            else
                Debug.LogError("[Faction Entity]: The Selection component is missing.");
        }

        public abstract void UpdateAttackComp(AttackEntity attackEntity);

        public virtual void Start()
        {
            
        }

        //initialize the faction entity
        public virtual void Init(int fID)
        {
            if (free == false) //if the entity belongs to a faction
            {
                factionID = fID; //set the faction ID.
                FactionMgr = GameManager.Instance.Factions[factionID].FactionMgr; //get the faction manager
                SetFactionColors(); //Set the faction color objects
            }
            else
                factionID = -1;

            if (TaskLauncherComp) //if the entity has a task launcher component
                TaskLauncherComp.OnTasksInit(); //initialize it
        }

        public virtual void Update()
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
        public void EnableSelectionPlane (Texture selectionTexture, Color freeColor)
        {
            plane.SetActive(true); //Activate the plane object where we will show the selection texture.

            plane.GetComponent<Renderer>().material.mainTexture = selectionTexture; //Show the selection texture and set its color.
            Color color = IsFree() ? freeColor : GameManager.Instance.Factions[factionID].FactionColor; //get the color
            plane.GetComponent<Renderer>().material.color = new Color(color.r, color.g, color.b, 0.5f); //update the plane's color
        }

        //disable the faction entity's selection plane:
        public void DisableSelectionPlane ()
        {
            plane.SetActive(false);
        }

        //method called to set a faction entity's faction colors:
        protected void SetFactionColors()
        {
            foreach (ColoredRenderer cr in coloredRenderers) //go through all renderers that can be colored
                cr.UpdateColor(GameManager.Instance.Factions[FactionID].FactionColor);
        }
         
        //get the radius of the faction entity here
        public abstract float GetRadius();
    }
}
