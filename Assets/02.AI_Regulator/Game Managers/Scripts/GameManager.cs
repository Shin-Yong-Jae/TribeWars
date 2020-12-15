using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

/* Game Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

//THIS COMPONENT IS SUBJECT TO REFACTORING/REWRITE IN FUTURE UPDATES.

namespace RTSEngine
{
    public enum GameStates { Running, Paused, Over, Frozen }
    public enum DefeatConditions { destroyCapital, eliminateAll }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance = null;
        public string MainMenuScene = "Menu"; //Main menu scene name, this is the scene that will be loaded when the player decides to leave the game.

        [SerializeField]
        private DefeatConditions defeatCondition = DefeatConditions.destroyCapital; //this presents the condition that defines when a faction is defeated: either when the capital is destroyed or all units/buildings are destroyed
        public DefeatConditions GetDefeatCondition() { return defeatCondition; }

        [SerializeField]
        private float speedModifier = 1.0f; //speed modifier for unit movement, construction and collection time!
        public float GetSpeedModifier () { return speedModifier >= 1.0f ? speedModifier : 1.0f;  } //must be at least 1.0f

        [HideInInspector]
        public static GameStates GameState; //game state

        [System.Serializable]
        //The array that holds all the current teams information.
        public class FactionInfo
        {
            public string Name; //Faction's name.

            public FactionTypeInfo TypeInfo; //Type of this faction (the type determines which extra buildings/units can this faction use).

            public Color FactionColor; //Faction's color.
            public bool playerControlled = false; //Is the team controlled by the player, make sure that only one team is controlled by the player.

            public int maxPopulation; //Maximum number of units that can be present at the same time (which can be increased in the game by constructing certain buildings)

            //update the maximum population
            public void UpdateMaxPopulation(int value, bool add = true)
            {
                if (add)
                    maxPopulation += value;
                else
                    maxPopulation = value;

                //custom event trigger:
                CustomEvents.instance.OnMaxPopulationUpdated(this, value);

                if (playerControlled == true)
                    UIManager.instance.UpdatePopulationUI(currentPopulation, maxPopulation);
            }
            //get the maximum population
            public int GetMaxPopulation()
            {
                return maxPopulation;
            }

            int currentPopulation; //Current number of spawned units.

            //update the current population
            public void UpdateCurrentPopulation(int value)
            {
                currentPopulation += value;
                //custom event trigger:
                CustomEvents.instance.OnCurrentPopulationUpdated(this, value);

                if (playerControlled == true)
                    UIManager.instance.UpdatePopulationUI(currentPopulation, maxPopulation);
            }

            //get the current population
            public int GetCurrentPopulation()
            {
                return currentPopulation;
            }

            //get the amount of free slots:
            public int GetFreePopulation()
            {
                return maxPopulation - currentPopulation;
            }

            public Building CapitalBuilding; //The capital building that MUST be placed in the map before startng the game.
            public Vector3 CapitalPos; //The capital building's position is stored in this variable because when it's a new multiplayer game, the capital buildings are re-spawned in order to be synced in all players screens.
            [HideInInspector]
            public FactionManager FactionMgr; //The faction manager is a component that stores the faction data. Each faction is required to have one.

            public NPCManager npcMgr; //Drag and drop the NPC manager's prefab here.
            private NPCManager npcMgrIns; //the active instance of the NPC manager prefab.

            //get the NPC Manager instance:
            public NPCManager GetNPCMgrIns() { return npcMgrIns; }

            //init the npc manager:
            public void InitNPCMgr()
            {
                //make sure there's a npc manager prefab set:
                if (npcMgr == null)
                {
                    Debug.LogError("[Game Manager]: NPC Manager hasn't been set for NPC faction.");
                    return;
                }

                npcMgrIns = Instantiate(npcMgr.gameObject).GetComponent<NPCManager>();

                //set the faction manager:
                npcMgrIns.FactionMgr = FactionMgr;

                //init the npc manager:
                npcMgrIns.Init();

                if (TypeInfo != null) //if this faction has a valid type.
                {
                    //set the building center regulator (if there's any):
                    if (TypeInfo.centerBuilding != null)
                        npcMgrIns.territoryManager_NPC.centerRegulator = npcMgrIns.GetBuildingRegulatorAsset(TypeInfo.centerBuilding);
                    //set the population building regulator (if there's any):
                    if (TypeInfo.populationBuilding != null)
                        npcMgrIns.populationManager_NPC.populationBuilding = npcMgrIns.GetBuildingRegulatorAsset(TypeInfo.populationBuilding);

                    //is there extra buildings to add?
                    if (TypeInfo.extraBuildings.Count > 0)
                    {
                        //go through them:
                        foreach (Building b in TypeInfo.extraBuildings)
                        {
                            if (b != null)
                                npcMgrIns.buildingCreator_NPC.independentBuildingRegulators.Add(npcMgrIns.GetBuildingRegulatorAsset(b));
                            else
                                Debug.LogError("[Game Manager]: Faction " + TypeInfo.Name + " (Code: " + TypeInfo.Code + ") has missing building regulator(s) in extra buildings.");
                        }
                    }
                }
            }

            public bool IsNPCFaction() //is this faction NPC?
            {
                return playerControlled == false && npcMgr != null;
            }

            public bool Lost = false; //true when the faction is defeated and can no longer have an impact on the game.

            //multiplayer related attributes:
#if RTSENGINE_MIRROR
            //Mirror: 
            public NetworkLobbyFaction_Mirror LobbyFaction_Mirror { set; get; }
            public NetworkFactionManager_Mirror FactionManager_Mirror { set; get; }
            public int ConnID_Mirror { set; get; }
#endif
        }
        public List<FactionInfo> Factions = new List<FactionInfo>();

        [SerializeField]
        private bool randomFactionSlots = true;

        private int activeFactionsAmount = 0; //Amount of spawned factions;

        //Peace time:
        public float PeaceTime = 60.0f; //Time (in seconds) after the game starts, when no faction can attack the other.

        public static int PlayerFactionID; //Faction ID of the team controlled by the player.
        public static FactionManager PlayerFactionMgr; //The faction manager component of the faction controlled by the player.
        public static bool initialized = false; //Are all factions stats ready? 

        //Borders:
        [HideInInspector]
        public int LastBorderSortingOrder = 0; //In order to draw borders and show which order has been set before the other, their objects have different sorting orders.
        [HideInInspector]
        public List<Border> AllBorders; //All the borders in the game are stored in this game.

        //Other scripts:
        [HideInInspector]
        public ResourceManager ResourceMgr;
        [HideInInspector]
        public UIManager UIMgr;
        [HideInInspector]
        public CameraMovement CamMov;
        [HideInInspector]
        public BuildingPlacement BuildingMgr;
        [HideInInspector]
        public SelectionManager SelectionMgr;
        [HideInInspector]
        public CustomEvents Events;
        [HideInInspector]
        public TaskManager TaskMgr;
        [HideInInspector]
        public BuildingPlacement PlacementMgr;
        [HideInInspector]
        public UnitManager UnitMgr;
        [HideInInspector]
        public TerrainManager TerrainMgr;
        [HideInInspector]
        public MovementManager MvtMgr;

        //Multiplayer related:
        public static bool MultiplayerGame { private set; get; } //If it's a multiplayer game, this will be true.
        public static int HostFactionID { private set; get; } //This is the Faction ID that represents the server/host of the multiplayer game.
#if RTSENGINE_MIRROR
        public NetworkLobbyManager_Mirror NetworkManager_Mirror { private set; get; }
#endif

        public AudioSource GeneralAudioSource; //The audio source where audio will be played generally unless the audio is local. In that case, it will be played 
        [SerializeField]
        private AudioClip winGameAudio = null;
        [SerializeField]
        private AudioClip loseGameAudio = null;

        void Awake()
        {
            //set the instance:
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            Time.timeScale = 1.0f; //unfreeze game if it was frozen from a previous game.

            initialized = false; //faction stats are not ready, yet

            //Get components:
            CamMov = FindObjectOfType(typeof(CameraMovement)) as CameraMovement; //Find the camera movement script.
            ResourceMgr = FindObjectOfType(typeof(ResourceManager)) as ResourceManager; //Find the resource manager script.
            if (ResourceMgr != null)
                ResourceMgr.gameMgr = this;
            UIMgr = FindObjectOfType(typeof(UIManager)) as UIManager; //Find the UI manager script.
            BuildingMgr = FindObjectOfType(typeof(BuildingPlacement)) as BuildingPlacement;
            Events = FindObjectOfType(typeof(CustomEvents)) as CustomEvents;
            TaskMgr = FindObjectOfType(typeof(TaskManager)) as TaskManager;
            UnitMgr = FindObjectOfType(typeof(UnitManager)) as UnitManager;
            SelectionMgr = FindObjectOfType(typeof(SelectionManager)) as SelectionManager;
            PlacementMgr = FindObjectOfType(typeof(BuildingPlacement)) as BuildingPlacement;
            TerrainMgr = FindObjectOfType(typeof(TerrainManager)) as TerrainManager;
            MvtMgr = FindObjectOfType(typeof(MovementManager)) as MovementManager;

            MultiplayerGame = false; //We start by assuming it's a single player game.

            InitFactionMgrs(); //create the faction managers components for the faction slots.

            InitMultiplayerGame(); //to initialize a multiplayer game.

            InitSinglePlayerGame(); //to initialize a single player game.

            SetPlayerFactionID(); //pick the player faction ID.

            InitFactionCapitals(); //init the faction capitals.

            ResourceMgr.InitFactionResources(); //init resources for factions.

            InitFactions(); //init the faction types.

            //In order to avoid having buildings that are being placed by AI players and units collide, we will ignore physics between their two layers:
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Hidden"), LayerMask.NameToLayer("Unit"));

            //Set the amount of the active factions:
            activeFactionsAmount = Factions.Count;

            GameState = GameStates.Running; //the game state is now set to running

            //reaching this point means that all faction info/stats in the game manager are ready:
            initialized = true;
        }

        private bool InitMirrorMultiplayerGame()
        {
#if RTSENGINE_MIRROR
            NetworkManager_Mirror = (NetworkLobbyManager_Mirror)NetworkLobbyManager_Mirror.singleton;

            if (NetworkManager_Mirror == null) //if there's mirror network lobby manager component in the scene then this is not a multiplayer game
                return false;

            //randomize the faction slots in this map before starting the assignment of each faction slot
            RandomizeFactionSlots(NetworkManager_Mirror.FactionSlotIndexes);

            MultiplayerGame = true; //this is now a multiplayer game.
            List<NetworkLobbyFaction_Mirror> lobbyFactions = NetworkLobbyManager_Mirror.LobbyFactions; //get all lobby faction components into a list

            if (lobbyFactions.Count >= Factions.Count) //if there aren't enough slots in this map for all factions.
            {
                Debug.LogError("[Game Manager]: Not enough slots available for all the factions coming from the multiplayer menu.");
                return false;
            }

            //set the defeat condition & speed modifier:
            defeatCondition = NetworkManager_Mirror.UIMgr.defeatConditionMenu.GetValue();
            speedModifier = NetworkManager_Mirror.UIMgr.speedModifierMenu.GetValue();

            //we have enough slots:
            for (int i = 0; i < lobbyFactions.Count; i++) //Loop through all the slots and set up the factions.
            {
                int index = lobbyFactions[i].Index; //get the faction ID.

                if (lobbyFactions[i].isLocalPlayer) //if this lobby faction component is owned by the local player
                    Factions[index].playerControlled = true;
                else
                    Factions[index].playerControlled = false; //not owned by the local player, set to not player controlled.

                if (lobbyFactions[i].IsHost) //does this lobby faction belong to the host?
                    HostFactionID = i; //mark it.

                //Set the info for the factions that we will use:
                Factions[index].Name = lobbyFactions[i].GetFactionName(); //get the faction name
                Factions[index].FactionColor = NetworkManager_Mirror.GetColor(lobbyFactions[i].GetFactionColorID()); //the faction color
                //get the initial max population from the network manager (making it the same for all the players).
                Factions[index].UpdateMaxPopulation(NetworkManager_Mirror.GetMapInitialPopulation(), false);
                Factions[index].Lost = false;

                Factions[index].LobbyFaction_Mirror = lobbyFactions[i]; //linking the faction with its lobby info script.

                Factions[index].TypeInfo = NetworkManager_Mirror.GetFactionTypeInfo(lobbyFactions[i].GetFactionTypeID()); //get the faction type info for this faction slot

                Factions[index].npcMgr = null; //mark as non NPC faction by setting the npc manager prefab to null
            }

            //loop through all faction slots slots and destroy the default capital buildings because the server will spawn new ones for each faction.
            for (int i = 0; i < Factions.Count; i++)
                DestroyImmediate(Factions[i].CapitalBuilding.gameObject);

            //if there are more slots than required, remove them
            while (lobbyFactions.Count < Factions.Count)
            {
                Destroy(Factions[Factions.Count - 1].FactionMgr); //destroy the faction manager component
                //remove the extra slots:
                Factions.RemoveAt(Factions.Count - 1);
            }
#endif
            return true;
        }

        //a method that initializes a multiplayer game
        private bool InitMultiplayerGame()
        {
#if RTSENGINE_MIRROR
            return InitMirrorMultiplayerGame();
#else
            return false;
#endif

        }

        //a method that initializes a single player game:
        private bool InitSinglePlayerGame()
        {
            //if there's no single player manager then
            if (SinglePlayerManager.instance == null)
                return false; //do not proceed.


            //If there's a map manager script in the scene, it means that we just came from the single player menu, so we need to set the NPC players settings!
            SinglePlayerManager singlePlayerMgr = SinglePlayerManager.instance;

            //randomzie the faction slots
            List<int> factionSlots = RTSHelper.GenerateIndexList(Factions.Count);
            RTSHelper.ShuffleList<int>(factionSlots); //randomize the faction slots indexes list IDs by shuffling it.
            RandomizeFactionSlots(factionSlots.ToArray());

            //This where we will set the NPC settings using the info from the single player manager:
            //First check if we have enough faction slots available:
            if (singlePlayerMgr.Factions.Count <= Factions.Count)
            {
                defeatCondition = singlePlayerMgr.defeatConditionMenu.GetValue(); //set defeat condition
                speedModifier = singlePlayerMgr.speedModifierMenu.GetValue(); //set speed modifier

                //loop through the factions slots of this map:
                for (int i = 0; i < singlePlayerMgr.Factions.Count; i++)
                {
                    //Set the info for the factions that we will use:
                    Factions[i].Name = singlePlayerMgr.Factions[i].FactionName; //name
                    Factions[i].FactionColor = singlePlayerMgr.Factions[i].FactionColor; //color
                    Factions[i].playerControlled = singlePlayerMgr.Factions[i].playerControlled; //is this faction controlled by the player? 
                    Factions[i].UpdateMaxPopulation(singlePlayerMgr.Factions[i].InitialPopulation, false); //initial maximum population (which can be increased in the game).
                    Factions[i].TypeInfo = singlePlayerMgr.Factions[i].TypeInfo; //the faction's code.

                    Factions[i].Lost = false; //the game just started.

                    Factions[i].npcMgr = (Factions[i].playerControlled == true) ? null : singlePlayerMgr.Factions[i].npcMgr; //set the npc mgr for this faction.
                }

                //if there are more slots than required.
                while (singlePlayerMgr.Factions.Count < Factions.Count)
                {
                    //remove the extra slots:
                    Destroy(Factions[Factions.Count - 1].FactionMgr); //destroy the faction manager component
                    DestroyImmediate(Factions[Factions.Count - 1].CapitalBuilding.gameObject);
                    Factions.RemoveAt(Factions.Count - 1);
                }

                //Destroy the map manager script because we don't really need it anymore:
                DestroyImmediate(singlePlayerMgr.gameObject);

                return true;
            }
            else
            {
                Debug.LogError("[Game Manager]: Not enough slots available for all the factions coming from the single player menu.");
                return false;
            }
        }

        private void InitFactionMgrs()
        {
            for (int i = 0; i < Factions.Count; i++) //go through the factions list
            {
                //create the faction manager components for each faction:
                Factions[i].FactionMgr = gameObject.AddComponent<FactionManager>();
                Factions[i].FactionMgr.FactionID = i;

                //Get the capital positions:
                Factions[i].CapitalPos = Factions[i].CapitalBuilding.transform.position; //setting the capital pos to spawn the capital building object later
            }
        }

        //a method that sets the player faction ID
        private bool SetPlayerFactionID()
        {
            PlayerFactionID = -1;
            for (int i = 0; i < Factions.Count; i++) //go through the factions list
            {
                if (Factions[i].playerControlled == true) //is this the player controlled faction?
                {
                    //if we have a player faction ID already:
                    if (PlayerFactionID != -1)
                    {
                        Debug.LogError("[Game Manager]: There's more than one faction labeled as player controlled.");
                        return false;
                    }
                    //if the player faction hasn't been set yet:
                    if (PlayerFactionID == -1)
                    {
                        PlayerFactionID = i;
                        PlayerFactionMgr = Factions[i].FactionMgr; //& set the player faction manager as well
                    }
                }
            }
            if (PlayerFactionID == -1) //if the player faction ID hasn't been set.
            {
                Debug.LogError("[Game Manager]: There's no faction labeled as player controlled.");
                return false;
            }

            return true;
        }

        //initialize the faction capitals.
        private void InitFactionCapitals()
        {
            //only in single player:
            if (MultiplayerGame == true)
                return;
            for (int i = 0; i < Factions.Count; i++) //go through the factions list
            {
                //if the faction has a valid faction type:
                if (Factions[i].TypeInfo != null)
                {
                    if (Factions[i].TypeInfo.capitalBuilding != null)
                    { //if the faction to a certain type

                        DestroyImmediate(Factions[i].CapitalBuilding.gameObject); //destroy the default capital and spawn another one:
                        //create new faction center:
                        Factions[i].CapitalBuilding = BuildingManager.CreatePlacedInstanceLocal(
                            Factions[i].TypeInfo.capitalBuilding, 
                            Factions[i].CapitalPos, 
                            Factions[i].TypeInfo.capitalBuilding.transform.rotation.eulerAngles.y, 
                            null, i, true);
                    }
                }

                //mark as faction capital:
                Factions[i].CapitalBuilding.FactionCapital = true;
            }
        }

        void Start()
        {
            //if it's not a MP game:
            if (MultiplayerGame == false)
            {
                //Set the player's initial cam position (looking at the faction's capital building):
                CamMov.LookAt(Factions[PlayerFactionID].CapitalBuilding.transform.position);
                CamMov.SetMiniMapCursorPos(Factions[PlayerFactionID].CapitalBuilding.transform.position);
            }
        }

        //last initialization method for factions: extra buildings and NPC managers init.
        private void InitFactions()
        {
            //no factions?
            if (Factions.Count == 0)
                return; //do not proceed.

            //go through the factions.
            for (int i = 0; i < Factions.Count; i++)
            {
                //Depending on the faction type, add extra units/buildings (if there's actually any) to be created for each faction:
                if (Factions[i].playerControlled == true) //if this faction is player controlled:
                {
                    if (Factions[i].TypeInfo != null) //if this faction has a valid type.
                    {
                        if (Factions[i].TypeInfo.extraBuildings.Count > 0) //if the faction type has extra buildings:
                            foreach (Building b in Factions[i].TypeInfo.extraBuildings)
                            {
                                BuildingMgr.AllBuildings.Add(b); //add the extra buildings so that this faction can use them.
                            }
                    }
                }
                else if (Factions[i].IsNPCFaction() == true) //if this is not controlled by the local player but rather NPC.
                {
                    //Init the NPC Faction manager:
                    Factions[i].InitNPCMgr();
                }

                if (Factions[i].TypeInfo != null) //if this faction has a valid type.
                {
                    Factions[i].FactionMgr.Limits.Clear();
                    //copy the faction type limits in the faction manager:
                    foreach (FactionTypeInfo.FactionLimitsVars LimitElem in Factions[i].TypeInfo.Limits)
                    {
                        FactionTypeInfo.FactionLimitsVars newLimitElem = new FactionTypeInfo.FactionLimitsVars()
                        {
                            Code = LimitElem.Code,
                            MaxAmount = LimitElem.MaxAmount,
                            CurrentAmount = 0
                        };

                        Factions[i].FactionMgr.Limits.Add(newLimitElem);
                    }
                }
            }
        }

        void Update()
        {
            //Peace timer:
            if (PeaceTime > 0)
            {
                PeaceTime -= Time.deltaTime;

                UIMgr.UpdatePeaceTimeUI(PeaceTime); //update the peace timer UI each time.
            }
            if (PeaceTime < 0)
            {
                //when peace timer is ended:
                PeaceTime = 0.0f;

                UIMgr.UpdatePeaceTimeUI(PeaceTime);
            }
        }

        // Are we in peace time?
        public bool InPeaceTime()
        {
            return PeaceTime > 0.0f;
        }

        //Randomize the order of the faction slots:
        private void RandomizeFactionSlots(int[] indexSeedList)
        {
            //do not randomize? okay. also make sure the index seed list has the same length as the faction slots amount
            if (randomFactionSlots == false || indexSeedList.Length != Factions.Count)
                return;

            int i = 0;
            while(i < indexSeedList.Length) //this seed list provides the randomized faction slot indexes
            {
                if (i == indexSeedList[i] || i > indexSeedList[i]) //to avoid reswapping faction slots
                {
                    i++;
                    continue;
                }

                //swap capital building, capital building position & NPC Manager info for the faction slots (that's all we need to swap for randomization).
                RTSHelper.Swap<Building>(ref Factions[i].CapitalBuilding, ref Factions[indexSeedList[i]].CapitalBuilding);
                RTSHelper.Swap<Vector3>(ref Factions[i].CapitalPos, ref Factions[indexSeedList[i]].CapitalPos);
                RTSHelper.Swap<NPCManager>(ref Factions[i].npcMgr, ref Factions[indexSeedList[i]].npcMgr);
                i++;
            }
        }

        //Game state methods:

        //called when a faction is defeated
        public void OnFactionDefeated(int factionID)
        {
            if (MultiplayerGame == false) //in the case of a singleplayer game
                OnFactionDefeatedLocal(factionID); //directly mark the faction as defeated
            else //multiplayer game:
            {
                NetworkInput NewInputAction = new NetworkInput() //ask the server to announce that the faction has been defeated.
                {
                    sourceMode = (byte)InputMode.destroy,
                    targetMode = (byte)InputMode.faction,
                    value = factionID,
                };

                InputManager.SendInput(NewInputAction, null, null); //send input action to the input manager
            }
        }

        //called locally when a faction is defeated (its capital building has fallen):
        public void OnFactionDefeatedLocal(int factionID)
        {
            //Show UI message.
            UIManager.instance.ShowPlayerMessage(GameManager.Instance.Factions[factionID].Name + " (Faction ID:" + factionID.ToString() + ") has been defeated.", UIManager.MessageTypes.Info);
            
            //Destroy all buildings and kill all units:
            if (Factions[factionID].IsNPCFaction() == true) //if his is a NPC faction
            {
                //destroy the active instance of the NPC Manager component:
                Destroy(Factions[factionID].GetNPCMgrIns().gameObject);
            }

            //faction manager of the one that has been defeated.
            FactionManager FactionMgr = Factions[factionID].FactionMgr;

            //go through all the units that this faction owns
            while (FactionMgr.Units.Count > 0)
            {
                if (FactionMgr.Units[0] != null)
                    FactionMgr.Units[0].HealthComp.DestroyFactionEntityLocal(false);
                else
                    FactionMgr.Units.RemoveAt(0);
            }

            //go through all the buildings that this faction owns
            while (FactionMgr.Buildings.Count > 0)
            {
                if (FactionMgr.Buildings[0] != null)
                {
                    FactionMgr.Buildings[0].HealthComp.DestroyFactionEntityLocal(false);
                }
                else
                {
                    FactionMgr.Buildings.RemoveAt(0);
                }
            }

            Factions[factionID].Lost = true; //ofc.
            activeFactionsAmount--; //decrease the amount of active factions:
            if (Events) Events.OnFactionEliminated(Factions[factionID]); //call the custom event.

            if (factionID == PlayerFactionID)
            {
                //If the player is defeated then:
                LooseGame();
            }
            //If one of the other factions was defeated:
            //Check if only the player was left undefeated!
            else if (activeFactionsAmount == 1)
            {
                WinGame(); //Win the game!
                if (Events) Events.OnFactionWin(Factions[factionID]); //call the custom event.
            }
        }

        //Win the game:
        public void WinGame()
        {
            //when all the other factions are defeated, 

            //stop whatever the player is doing:
            UIMgr.SelectionMgr.DeselectBuilding();
            UIMgr.SelectionMgr.DeselectUnits();
            UIMgr.SelectionMgr.DeselectResource();

            UIMgr.WinningMenu.SetActive(true); //Show the winning menu
            AudioManager.PlayAudio(GeneralAudioSource.gameObject, winGameAudio, false);

            Time.timeScale = 0.0f; //freeze the game
            GameState = GameStates.Over; //the game state is now set to over
        }

        //called when the player's faction is defeated:
        public void LooseGame()
        {
            UIMgr.LoosingMenu.SetActive(true); //Show the loosing menu
            AudioManager.PlayAudio(GeneralAudioSource.gameObject, loseGameAudio, false);

            Time.timeScale = 0.0f; //freeze the game
            GameState = GameStates.Over; //the game state is now set to over
        }

        //allows the player to leave the current game:
        public void LeaveGame()
        {
            if (MultiplayerGame == false)
                SceneManager.LoadScene(MainMenuScene); //go back to main menu
            else
            {
#if RTSENGINE_MIRROR
                OnFactionDefeated(PlayerFactionID); //mark the player's faction as defeated.
                NetworkManager_Mirror.LastDisconnectionType = DisconnectionType.left;
                NetworkManager_Mirror.LeaveLobby(); //leave the lobby.
#endif
            }
        }

        //Check if this is the local player:
        public bool IsLocalPlayer(GameObject Obj)
        {
            bool LocalPlayer = false;

            if (Obj.gameObject.GetComponent<Unit>())
            {
                if (Obj.gameObject.GetComponent<Unit>().FactionID == PlayerFactionID)
                { //if the unit and local player have the same faction ID
                    LocalPlayer = true;
                }
                else if (Obj.gameObject.GetComponent<Unit>().IsFree() == true)
                { //if this is a free unit and the local player is the server
                    LocalPlayer = false; //set this initially to false
                    if (MultiplayerGame == true)
                    { //but if it's a MP game
                        if (PlayerFactionID == HostFactionID)
                        { //and this is the server then set it to true.
                            LocalPlayer = true;
                        }
                    }
                }
            }
            else if (Obj.gameObject.GetComponent<Building>())
            {
                if (Obj.gameObject.GetComponent<Building>().FactionID == PlayerFactionID)
                { //if the building and local player have the same faction ID
                    LocalPlayer = true;
                }
                else if (Obj.gameObject.GetComponent<Building>().IsFree() == true)
                { //if this is a free unit and the local player is the server
                    LocalPlayer = false; //set this initially to false
                    if (MultiplayerGame == true)
                    { //but if it's a MP game
                        if (PlayerFactionID == HostFactionID)
                        { //and this is the server then set it to true.
                            LocalPlayer = true;
                        }
                    }
                }
            }

            return LocalPlayer;
        }
    }
}
