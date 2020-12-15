using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public class BuildingSelection : SelectionEntity
    {
        private Building building; //the building's main component
        public override void UpdateMainObject(GameObject mainObject) { building = mainObject.GetComponent<Building>(); } //update the main component
        public override GameObject GetMainObject() { return building.gameObject; }

        //can the building be selected?
        public override bool CanSelect()
        {
            return !(canSelect == false || building.Placed == false || (selectOwnerOnly == true && building.FactionID != GameManager.PlayerFactionID));
        }

        //called when the selection manager attempts to select the unit associated with this selection object
        protected override void OnSelected()
        {
            if (CanSelect() == false) //only faction owner can select this but the local player is not (or if building is not placed yet)
                return;

            SelectionManager.instance.SelectBuilding(building);

            if (building.HealthComp.IsDestroyed == false) //if the building is not destroyed yet
            {
                AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, building.GetSelectionAudio(), false);
                if (building.PortalComp != null) //if the building has portal component
                    building.PortalComp.OnMouseClick();
            }
        }

        //is the building currently selected?
        public override bool IsSelected()
        {
            return SelectionManager.instance.IsBuildingSelected(building);
        }

        //deselect the building if it's selected:
        public override void Deselect()
        {
            if (IsSelected())
                SelectionManager.instance.DeselectBuilding();
        }

        //is the building managed by this component a free one?
        public override bool IsFree() { return building.IsFree(); }

        //get the minimap icon color of the building (the faction color)
        public override Color GetMinimapIconColor () { return GameManager.Instance.Factions[building.FactionID].FactionColor;  }

        //called when the mouse hovers over the building in order to show the hover health bar
        protected override void OnHoverHealthBarRequest()
        {
            if (building.Placed == false || building.HealthComp.IsDead() == true) //if the building is not placed or is marked as dead, do not proceed
                return;

            UIManager.instance.TriggerHoverHealthBar(true, this, building.HealthComp.GetHoverHealthBarY()); //enable the hover health bar
            if (UIManager.instance.IsHoverSource(this)) //if the hover health bar was successfully enabled
                UIManager.instance.UpdateHoverHealthBar(building.HealthComp.CurrHealth, building.HealthComp.MaxHealth); //update it
        }
    }
}
