using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RTSEngine
{
    public abstract class SelectionEntity : MonoBehaviour
    {
        public FactionEntity FactionEntity { set; get; } //the faction entity for whome this component controls selection

        public abstract void UpdateMainObject(GameObject mainObject);
        public abstract GameObject GetMainObject();

        [SerializeField]
        protected bool canSelect = true; //if this is set to false then the unit/building/resource will not be selectable
        [SerializeField]
        protected bool selectOwnerOnly = false; //if this is set to true then only the local player can select the object associated to this.

        //use this method to change the selection status of this entity
        public void ToggleSelection(bool enable, bool ownerOnly)
        {
            canSelect = enable;
            selectOwnerOnly = ownerOnly;
        }

        [SerializeField]
        private float minimapIconSize = 0.5f; //the size of the selection main object icon in the minimap
        public float GetMinimapIconSize () { return minimapIconSize; }

        protected GameObject minimapIcon; //reference to the minimap's icon of this selection entity
        public void UpdateMinimapIcon (GameObject newIcon) { minimapIcon = newIcon; }
        public void UpdateMinimapIconColor () //a method that updates the minimap color
        {
            if (minimapIcon == null)//invalid icon assigned? 
                return;

            if (IsFree()) //if this component manages the selection of a free entity
                minimapIcon.GetComponent<MeshRenderer>().material.color = MinimapIconManager.instance.FreeFactionIconColor; //assign the free faction color
            else
                minimapIcon.GetComponent<MeshRenderer>().material.color = GetMinimapIconColor(); //if not, get the correct faction color
        }
        public abstract Color GetMinimapIconColor();
        public void ToggleMinimapIcon(bool show) { minimapIcon.SetActive(show); }
        public void DisableMinimapIcon() //a method that disables the minimap icon
        {
            minimapIcon.SetActive(false); //hide it
            minimapIcon.transform.SetParent(null, true); //no longer child object of the main selection object
            MinimapIconManager.instance.InactiveMinimapIcons.Add(minimapIcon); //add it to the unused minimap icons list so that it can be used later
            minimapIcon = null;
        }

        public virtual void Start()
        {
            gameObject.layer = 0; //setting it to the default layer because raycasting ignores building and resource layers.

            //in order for collision detection to work, we must assign the following settings to the collider and rigidbody.
            GetComponent<Collider>().isTrigger = true;
            GetComponent<Collider>().enabled = true;

            if (GetComponent<Rigidbody>() == null)
            {
                gameObject.AddComponent<Rigidbody>();
            }
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;
        }

        public abstract bool CanSelect();

        //a method called by the Selection Manager to attempt to select the object associated with this selection entity
        public void Select()
        {
            if (canSelect == false) //if the main component hasn't been assigned or this can't be selected
                return;

            if (!EventSystem.current.IsPointerOverGameObject() && BuildingPlacement.IsBuilding == false) //as long as the player is not clicking on a UI object and is not placing a building
                OnSelected();
        }

        protected abstract void OnSelected();

        public abstract bool IsSelected();

        public abstract void Deselect();

        public abstract bool IsFree(); //is the entity managed by this selection component a faction entity or a free one?

        //called when the mouse is over this selection entity's collider
        void OnMouseEnter()
        {
            //if the hover health bar feature is enabled
            if (UIManager.instance.EnableHoverHealthBar == true && !EventSystem.current.IsPointerOverGameObject() && BuildingPlacement.IsBuilding == false) //as long as the player is not clicking on a UI object and is not placing a building
                OnHoverHealthBarRequest();
        }

        protected abstract void OnHoverHealthBarRequest();

        //if the mouse leaves this collider
        void OnMouseExit()
        {
            if (UIManager.instance.EnableHoverHealthBar == true) //if the hover health bar feature is enabled
                UIManager.instance.TriggerHoverHealthBar(false, this, 0.0f);
        }
    }
}
