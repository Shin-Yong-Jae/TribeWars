using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

/* Resource Generator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(Building))]
    public class ResourceGenerator : MonoBehaviour {

        public Building building { private set; get; } //the main building component for which this component opeartes

        [System.Serializable]
		public class Generator
		{
            [SerializeField]
            private ResourceTypeInfo resourceType = null; //the resource type to collect
            public string GetResourceName() { return resourceType.Name; }
            [SerializeField]
            private Sprite taskIcon = null; //when the maximum amount is reached, a task appears on the task panel to collect the gathered resource when the generator is selected. This is the task's icon.
            public Sprite GetTaskIcon() { return taskIcon; }
            [SerializeField]
            private bool autoCollect = false; //if true, resources produced by this generator will be collected automatically. If false, then the player will have to collect them manually

            [SerializeField]
            private bool isActive = true; //is this generator active or not?

            [SerializeField]
            protected float collectOneUnitTime = 1.5f; //time required to collect one unit of this resource type.

            private float timer; //each generator is assigned a timer
            private void ReloadTimer () { timer = collectOneUnitTime; } //reloads the production timer

            [SerializeField]
			private int maxAmount = 50; //the maximum amount of this resource type that this generator can store.
			private int currAmount = 0; //the current amount of produced resource in this generator.
            public int GetCurrAmount() { return currAmount; }
            public bool IsMaxAmountReached () { return currAmount >= maxAmount; } //did this generator reach its max amount?

            //Initialize the generator's settings:
            public void Init ()
            {
                currAmount = 0;
                ReloadTimer();
            }

            //Update the production in this generator:
            public void OnProductionUpdate (Building building)
            {
                if (isActive == false) //if this resource is not active, then do not proceed
                    return;

                if (IsMaxAmountReached() == false && isActive == true) //as long as the maximum amount is not yet reached and this generator is active
                {
                    if (timer > 0) //timer
                        timer -= Time.deltaTime;
                    else //timer is done
                    {
                        if (autoCollect == true) //if resources are auto collected
                            ResourceManager.instance.AddResource(building.FactionID, resourceType.Name, 1); //add one unit directly to the faction
                        else
                        {
                            currAmount++; //increment current amount
                            if (IsMaxAmountReached() == true) //if the maximum allowed amount is reached
                                if (GameManager.Instance.IsLocalPlayer(building.gameObject) && SelectionManager.instance.IsBuildingSelected(building)) //if this is the player's faction and the resource generator is selected
                                    UIManager.instance.UpdateBuildingUI(building);
                                else //if this is a NPC faction
                                {
                                    OnResourceCollected(building.FactionID);
                                }
                        }

                        ReloadTimer(); //reload timer
                    }
                }
            }

            //a method to collect resources and reset production settings
            public void OnResourceCollected (int factionID)
            {
                ResourceManager.instance.AddResource(factionID, resourceType.Name, currAmount); //add resources
                currAmount = 0; //reset the current amount
            }
		}
        [SerializeField]
        private Generator[] generators = new Generator[0]; //an array of the generators available in this component
        public int GetGeneratorsLength () { return generators.Length; }
        public Generator GetGenerator (int id) { return generators[id];  }

        [SerializeField]
        private int taskPanelCategory = 0; //task panel category at which the collection button will be shown in case Auto Collect is turned off.
        public int GetTaskPanelCategory () { return taskPanelCategory; }
        [SerializeField]
        private AudioClip collectionAudio = null; //played when the player collects the resources produced by this generator.

        void Awake()
        {
            building = GetComponent<Building>(); //get the main building's component
        }
        
        void Start ()
		{
            foreach (Generator g in generators) //go through all the available generators
                g.Init(); //init the generator's settings

			if (GameManager.MultiplayerGame == true && GameManager.Instance.IsLocalPlayer(gameObject)) //if it's a multiplayer game and this does not belong to the local player's faction.
                enabled = false; //disable this component
        }
        
		void Update ()
		{
			if (building.IsBuilt == true) //in order for the building to generate resources, it must be built.
                foreach(Generator r in generators) //go through the generators
                    r.OnProductionUpdate(building); //update the production of the resources
        }

        //a method to collect resources.
        public void CollectResources (int generatorID)
        {
            UIManager.instance.HideTooltip(); //hide the tooltip because the task will be gone

            generators[generatorID].OnResourceCollected(building.FactionID); //collect the resources and reset the generator's settings

            if (GameManager.Instance.IsLocalPlayer(gameObject)) //if this is the local player:
            {
                if (SelectionManager.instance.IsBuildingSelected(building)) //if the resource generator is selected
                {
                    UIManager.instance.UpdateInProgressTasksUI(); //update the in progress task UI
                    UIManager.instance.UpdateTaskPanel(); //update the task panel as well
                }
                AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, collectionAudio, false); //plau the collection audio
            }
        }
	}
}