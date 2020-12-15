using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [RequireComponent(typeof(Building))]
    public class BuildingHealth : FactionEntityHealth
    {
        private Building building; //the main building component for which this component opeartes

        //hide/show different parts of the building depending on its current health allowing the building to reflect it
        [System.Serializable]
        public class State
        {
            [SerializeField]
            private IntRange healthRange = new IntRange(0, 100); //if the building's health is inside this range, then it will have this state

            public bool IsInRange (int value) { return value >= healthRange.min && value < healthRange.max; }

            [SerializeField]
            private GameObject[] showChildObjects = new GameObject[0];//child objects of the building to show (activate) when the building is in this state.

            [SerializeField]
            private GameObject[] hideChildObjects = new GameObject[0]; //child objects of the building to hide (deactivate) when the building is in this state.

            public void Toggle(bool enable)
            {
                foreach (GameObject obj in showChildObjects) //hide/show the show child objects
                    obj.SetActive(enable);

                foreach (GameObject obj in hideChildObjects) //hide/show the hide child objects
                    obj.SetActive(!enable);
            }
        }

        [SerializeField]
        private List<State> constructionStates = new List<State>(); //a stack datastructure that holds the building's states when it is under construction

        [SerializeField]
        private State constructionCompleteState = new State(); //this is state is activated when the building's construction is complete (it's like a transition state)

        [SerializeField]
        private List<State> builtStates = new List<State>(); //a stack datastructure that holds the building's states post construction

        private Stack<State> inactiveStates = new Stack<State>(); //a stack datastructure that holds all the inactive states
        private Stack<State> activeStates = new Stack<State>(); //a stack datastructure that holds all the previously activated states.
        private State lastState = null; //a pointer to the last state instance

        public override void Awake()
        {
            base.Awake();
            building = GetComponent<Building>(); //get the main building component

            building.IsBuilt = false; //by default, the building is not built

            activeStates.Clear();
            inactiveStates.Clear();
            foreach (State s in constructionStates)//therefore, we'll start by using the following inactive states:
                inactiveStates.Push(s);
        }

        //when the building's health is updated
        public override void OnHealthUpdated(int value, FactionEntity source)
        {
            base.OnHealthUpdated(value, source);
            CustomEvents.instance.OnBuildingHealthUpdated(building, value, source);

            CheckState(value > 0); //check the building's state
        }

        //when the building reaches its max health:
        public override void OnMaxHealthReached(int value, FactionEntity source)
        {
            base.OnMaxHealthReached(value, source);

            Unit[] workersList = building.WorkerMgr.GetAll(); //get all workers in the worker manager

            for (int i = 0; i < workersList.Length; i++)
                workersList[i].BuilderComp.Stop(); //stop constructing

            CompleteConstruction();
        }

        //when the building is destroyed:
        public override void DestroyFactionEntityLocal(bool upgrade)
        {
            base.DestroyFactionEntityLocal(upgrade);

            if (SelectionManager.instance.IsBuildingSelected(building)) //If this building is selected then deselect it:
                SelectionManager.instance.DeselectBuilding();

            if (building.IsFree() == false) //if this is not a free building
            {
                if (building.BorderComp) //if it has a border component:
                    building.BorderComp.Deactivate(); //deactivate the border component
                else
                {
                    if (building.CurrentCenter != null) //If the building is not a center then we'll check if it occupies a place in the defined buildings for its center:
                        building.CurrentCenter.UnegisterBuilding(building);
                }

                if (building.FactionMgr.DropOffBuildings.Contains(building)) //Remove the building from the resource drop off building lists if it's there
                    building.FactionMgr.DropOffBuildings.Remove(building);

                building.RemovePopulationSlots(); //remove population added by this building when destroyed

                building.FactionMgr.RemoveBuilding(building); //Remove the building from the spawned buildings list in the faction manager:

                //Remove bonuses from nearby resources:
                building.ToggleResourceBonus(false);

                //Check if it's the capital building, it's not getting upgraded and the faction defeated condition is set to capital destructionss
                if (building.FactionCapital == true && GameManager.Instance.GetDefeatCondition() == DefeatConditions.destroyCapital && upgrade == false)
                    GameManager.Instance.OnFactionDefeated(building.FactionID);
            }

            if (upgrade == false)
                CustomEvents.instance.OnBuildingDestroyed(building);
        }

        //-----------------------------------------------------------------------------------------
        //Building Construction:

        //method called to complete the construction of the building
        private void CompleteConstruction()
        {
            if (building.IsBuilt == true) //if the building has been already constructed
                return; //do not proceed

            building.IsBuilt = true; //mark as built.

            CustomEvents.instance.OnBuildingBuilt(building); //custom event

            //switch the building states for post construction mode:
            if (constructionCompleteState != null)
                ActivateState(constructionCompleteState);

            activeStates.Clear();
            inactiveStates.Clear();
            builtStates.Reverse();
            foreach (State s in builtStates)
                inactiveStates.Push(s);

            building.ToggleModel(true); //show the building's model

            if (SelectionManager.instance.IsBuildingSelected(building)) //Check if the building is currently selected:
                UIManager.instance.UpdateBuildingUI(building); //Update the building's selection UI

            building.OnBuilt(); //set the building's built settings
        }

        //-----------------------------------------------------------------------------------------
        //Building State:

        //This method allows to show/hide child ojbects of the building depending on the building's health (either in or post construction mode)
        public void CheckState(bool healthIncrease)
        {
            while(healthIncrease == !building.IsBuilt && inactiveStates.Count > 0 && inactiveStates.Peek().IsInRange(CurrHealth))
            {
                State poppedState = inactiveStates.Pop(); //pop the state
                ActivateState(poppedState); //update the building to this state
                activeStates.Push(poppedState); //push it to the to the active states stack
            }
            while (healthIncrease == building.IsBuilt && activeStates.Count > 0 && activeStates.Peek().IsInRange(CurrHealth) == false)
            {
                State poppedState = activeStates.Pop(); //pop the state
                inactiveStates.Push(poppedState); //push it back to the states stack

                //update to the next state in the active states stack
                ActivateState(activeStates.Count > 0 ? activeStates.Peek() : null);
            }
        }

        //a method that updates the building state:
        public virtual void ActivateState(State newState)
        {
            //if the new state is valid:
            if (newState != null)
                newState.Toggle(true);

            lastState = newState; //assign last state to new state
        }
    }
}
