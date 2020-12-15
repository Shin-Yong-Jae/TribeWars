using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public class NPCDefenseManager : NPCComponent
    {
        private bool isDefending = false; //is the faction currently in defense mode?

        public bool IsDefending() { return isDefending; } //get if the defenese manager is in defense mode or not.

        //ratio of the army units that will stay during an attacking for defensing purposes.
        public FloatRange defenseRatioRange = new FloatRange(0.1f,0.2f);

        //if there's a in progress attack and this faction is attacked, do we cancel the attack?
        public bool cancelAttackOnDefense = true;

        //timer that will assign defense orders for units in case the faction is in defensive mode:
        public FloatRange cancelDefenseReloadRange = new FloatRange(3.0f, 7.0f);
        private float cancelDefenseTimer;

        private Building lastDefenseCenter; //the last defense center is saved here.

        //support:
        public bool unitSupportEnabled = true; //if enabled, then when a unit is attacked, it can ask support from in range units.
        public FloatRange unitSupportRange = new FloatRange(5, 10); //the actual support range.

        GameManager gameMgr;

        void Start ()
        {
            gameMgr = GameManager.Instance;

            //add event listeners:
            CustomEvents.UnitHealthUpdated += OnUnitHealthUpdated;
            CustomEvents.BuildingHealthUpdated += OnBuildingHealthUpdated;
        }

        private void OnDisable()
        {
            //stop listening to events:
            CustomEvents.UnitHealthUpdated -= OnUnitHealthUpdated;
            CustomEvents.BuildingHealthUpdated -= OnBuildingHealthUpdated;
        }

        //called each time a unit's health is updated
        void OnUnitHealthUpdated (Unit unit, float value, FactionEntity source)
        {
            //if the unit belongs to this faction, it's an attack & that there's a valid source:
            if(unit.FactionID == factionMgr.FactionID && value < 0.0f && source != null)
            {
                //if the source faction ID is not this faction:
                if (source.FactionID != factionMgr.FactionID)
                {
                    OnUnitSupportRequest(unit.transform.position, source);

                    //check if the unit is not actually part of the attacking units:
                    if (npcMgr.attackManager_NPC.IsUnitDeployed(unit) == false)
                    {
                        //=> faction is getting attacked:

                        //launch defense
                        LaunchDefense(unit.transform.position);
                    }
                }
            }
        }

        //called each time a building's health is updated
        void OnBuildingHealthUpdated (Building building, int value, FactionEntity source)
        {
            //if the building belongs to this faction, it's an attack & that there's a valid source:
            if (building.FactionID == factionMgr.FactionID && value < 0.0f && source != null)
            {
                //if the source faction ID is not this faction:
                if (source.FactionID != factionMgr.FactionID)
                {
                    //=> faction is getting attacked:

                    //launch defense
                    LaunchDefense(building.transform.position);
                }
            }
        }

        //called when a unit requests support from nearby units:
        void OnUnitSupportRequest (Vector3 pos, FactionEntity target)
        {
            //if the unit support feature is disabled
            if (unitSupportEnabled == false)
                return; //do not proceed.

            //go through the faction's attack units:
            foreach(Unit u in factionMgr.Army)
            {
                //check if the unit is within the required distance:
                if(Vector3.Distance(u.transform.position, pos) < unitSupportRange.getRandomValue())
                {
                    //request support (as long as the unit isn't busy):
                    MovementManager.instance.LaunchAttack(u, target, MovementManager.AttackModes.none, false);
                }
            }
        }

        //get a list of units and then sends them back to their creator buildings
        public void SendBackUnits (List<Unit> unitsList)
        {
            //go through the units:
            foreach(Unit u in unitsList)
            {
                //if the unit is valid:
                if(u != null)
                {
                    //if it doesn't have a creator building
                    if (u.Creator == null)
                        u.Creator = gameMgr.Factions[factionMgr.FactionID].CapitalBuilding;

                    //send unit to back to creator building:
                    MovementManager.instance.Move(u, u.Creator.transform.position, u.Creator.GetRadius(), u.Creator.gameObject, InputMode.building, false);
                }
            }
        }

        void Update()
        {
            OnDefenseProgress();
        }

        //a method that launches the defense:
        void LaunchDefense (Vector3 attackPos)
        {
            //reload the defense timer:
            cancelDefenseTimer = cancelDefenseReloadRange.getRandomValue();

            ToggleCenterDefense(attackPos, true); //enable defense for the closest building center to the attack pos.

            //if the defense is already activated
            if (isDefending == true)
                return; //do not proceed.

            isDefending = true;

            //is the NPC faction is undergoing an attack on another faction but it's not allowed in defense mode?
            if(cancelAttackOnDefense == true && npcMgr.attackManager_NPC.IsAttacking() == true)
            {
                //cancel attack:
                npcMgr.attackManager_NPC.CancelAttack();
            } 
        }

        //toggling defense for building centers:
        void ToggleCenterDefense(Vector3 attackPos, bool enable)
        {
            //if we're enabling center defense:
            if (enable == true)
            {
                //if the last defense center was the capital then do not change it:
                if (lastDefenseCenter != gameMgr.Factions[factionMgr.FactionID].CapitalBuilding)
                {
                    //get closest capital building to attack position:
                    lastDefenseCenter = BuildingManager.GetClosestBuilding(attackPos, factionMgr.BuildingCenters);
                }

                //if no valid center is assigned:
                if (lastDefenseCenter == null)
                    return; //do not continue
            }

            //go through the army units and set the defense center:
            foreach (Unit u in factionMgr.Army)
            {
                //make sure the unit is not deployed for attack:
                if (npcMgr.attackManager_NPC.IsUnitDeployed(u) == false)
                {
                    //if there are multiple attack components
                    if (u.MultipleAttackMgr != null)
                        //go through them and set the same settings.
                        foreach (AttackEntity a in u.MultipleAttackMgr.AttackEntities)
                            a.SearchRangeCenter = (enable == true) ? lastDefenseCenter.BorderComp : null;
                    else //only one attack component:
                        u.AttackComp.SearchRangeCenter = (enable == true) ? lastDefenseCenter.BorderComp : null;
                }
            }
        }

        //called when the faction is defending:
        void OnDefenseProgress ()
        {
            if(isDefending == true) //making sure the faction is actually defending:
            {
                //defense timer:
                if (cancelDefenseTimer > 0)
                    cancelDefenseTimer -= Time.deltaTime;
                else
                {
                    //if the timer is over -> defense mode is no longer required:
                    StopDefense();
                }
            }
        }

        //a method to stop defending:
        void StopDefense()
        { 
            isDefending = false; //no longer defending
            ToggleCenterDefense(Vector3.zero, false); //stop center defense mode.

            lastDefenseCenter = null;
        }
    }
}
