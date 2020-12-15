using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Minimap Icon Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    //all units/buildings/resources icons in the minimap will be handled by this component
    public class MinimapIconManager : MonoBehaviour
    {
        public static MinimapIconManager instance; //only one instance of this component is allowed per game.

        public GameObject IconPrefab; //the minimap's icon prefab
        public float MinimapIconHeight = 20.0f; //height of the minimap icon

        //a list where all created minimap icons are, this list will be used for object pooling
        public List<GameObject> InactiveMinimapIcons { set; get; }

        //this is the color that the free units/buildings will have.
        public Color FreeFactionIconColor = Color.white;

        void Awake()
        {
            //assign the instance:
            if (instance == null)
                instance = this;
            else if (instance != this) //if we have another already active instance of this component
                Destroy(this); //destroy this instance then

            if (IconPrefab == null)
            {
                //if there's no icon prefab then disable this component.
                Debug.LogError("No minimap icon prefab has been assigned in the Minimap Icon Manager.");
                enabled = false;
            }

            InactiveMinimapIcons = new List<GameObject>(); //initialize this list
        }

        //Assign a minimap icon:
        public void AssignIcon (SelectionEntity selection)
        {
            selection.UpdateMinimapIcon(GetNewMinimapIcon(selection.GetMainObject().transform, selection.GetMinimapIconSize())); //assign the minimap icon to the selection obj component
            selection.UpdateMinimapIconColor(); //update the new minimap icon color
        }
        

        //method to get a minimap icon either from the inactive list or create one
        GameObject GetNewMinimapIcon(Transform Parent, float Size)
        {
            GameObject NewMinimapIcon = null;

            if (InactiveMinimapIcons.Count > 0) //if there are any unused minimap icons
            {
                //get one and that's it
                NewMinimapIcon = InactiveMinimapIcons[0];
                InactiveMinimapIcons.RemoveAt(0);
            }
            else //if we don't have an unused one we need to create one
            {
                //create one from the prefab
                NewMinimapIcon = Instantiate(IconPrefab, Parent.position, Quaternion.identity);
            }

            //set its size;
            NewMinimapIcon.transform.localScale = new Vector3(Size, Size, Size);

            //set it as child of the parent object
            NewMinimapIcon.transform.SetParent(Parent, true);

            NewMinimapIcon.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f); //set its position
            NewMinimapIcon.transform.position = new Vector3(NewMinimapIcon.transform.position.x, MinimapIconHeight, NewMinimapIcon.transform.position.z); //set its position

            return NewMinimapIcon;
        }
    }
}