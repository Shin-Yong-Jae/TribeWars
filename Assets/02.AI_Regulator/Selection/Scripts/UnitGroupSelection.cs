using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Unit Group Selection script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(SelectionManager))]
    public class UnitGroupSelection : MonoBehaviour
    {
        //each GroupSelectionSlot instance presents a slot where a group of units can be stored.
        [System.Serializable]
        public struct GroupSelectionSlot
        {
            public KeyCode key;
            [HideInInspector]
            public List<Unit> units;
        }
        [SerializeField]
        private GroupSelectionSlot[] groupSelectionSlots = new GroupSelectionSlot[0]; //the array that holds all the group selection slots

        //when this key is pressed along with one of the group selection slots' key then the group selection slot will be set.
        [SerializeField]
        private KeyCode assignGroupKey = KeyCode.LeftShift;

        [SerializeField]
        private bool showUIMessages = true; //when enabled, each group assign/selection will show a UI message to the player

        [SerializeField]
        private AudioClip assignGroupAudio = null; //played when a selection group slot is assigned
        [SerializeField]
        private AudioClip selectGroupAudio = null; //played when a selection group slot is activated
        [SerializeField]
        private AudioClip groupEmptyAudio = null; //played when the player attempts to activate the selection of a group slot but it happens to be empty

        SelectionManager selectionMgr;

        private void Awake()
        {
            selectionMgr = GetComponent<SelectionManager>();
        }

        private void Update()
        {
            foreach(GroupSelectionSlot slot in groupSelectionSlots) //go through all the group selection slots
            {
                if(Input.GetKeyDown(slot.key)) //if the player presses both the slot specific key
                {
                    if(Input.GetKey(assignGroupKey)) //if the player presses the group slot assign key at the same time -> assign group
                    {
                        if(selectionMgr.SelectedUnits.Count > 0) //make sure that there's at least one unit selected
                        {
                            //assign this new group to them
                            slot.units.Clear();
                            slot.units.AddRange(selectionMgr.SelectedUnits);

                            //play audio:
                            AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, assignGroupAudio, false);

                            //inform player about assigning a new selection group:
                            if(showUIMessages)
                                UIManager.instance.ShowPlayerMessage("Unit selection group has been set.", UIManager.MessageTypes.Info);
                        }
                    }
                    else //the assign group key hasn't been assigned -> select units in this slot if there are any
                    {
                        bool found = false; //determines whether there are actually units in the list
                        //it might be that the previously assigned units to this slot are all dead and therefore all slots are referencing null

                        int i = 0; //we'll be also clearing empty slots
                        while(i < slot.units.Count)
                        {
                            if (slot.units[i] == null) //if this element is invalid
                                slot.units.RemoveAt(i); //remove it
                            else
                            {
                                if (found == false) //first time encountering a valid
                                    selectionMgr.DeselectUnits(); //deselect the currently selected units.

                                selectionMgr.SelectUnit(slot.units[i], true); //add unit to selection
                                found = true;
                            }

                            i++;
                        }

                        if(found == true) //making sure that there are valid units in the list that have been selected:
                        {
                            //play audio:
                            AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, selectGroupAudio, false);

                            //inform player about selecting:
                            if (showUIMessages)
                                UIManager.instance.ShowPlayerMessage("Unit selection group has been selected.", UIManager.MessageTypes.Info);
                        }
                        else //the list is either empty or all elements are invalid
                        {
                            //play audio:
                            AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, groupEmptyAudio, false);

                            //inform player about the empty group:
                            if (showUIMessages)
                                UIManager.instance.ShowPlayerMessage("Unit selection group is empty.", UIManager.MessageTypes.Error);
                        }
                    }
                }
            }
        }
    }
}
