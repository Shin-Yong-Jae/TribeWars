using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public class UnitSelection : SelectionEntity
    {
        private Unit unit; //the unit's main component
        public override void UpdateMainObject(GameObject mainObject) { unit = mainObject.GetComponent<Unit>(); } //update the main component
        public override GameObject GetMainObject() { return unit.gameObject; }

        //can the unit be selected?
        public override bool CanSelect()
        {
            return !(canSelect == false || selectOwnerOnly == true && unit.FactionID != GameManager.PlayerFactionID);
        }

        //called when the selection manager attempts to select the unit associated with this selection object
        protected override void OnSelected()
        {
            if (CanSelect() == false)
                return;

            unit.OnMouseClick(); //trigger a mouse click in the unit's main component

            if ((unit.FactionID == GameManager.PlayerFactionID || unit.IsFree() == true) && unit.HealthComp.IsDead() == false) //local player? and unit is not dead
                AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, unit.GetSelectionAudio(), false);
        }

        //is the unit currently selected?
        public override bool IsSelected()
        {
            return SelectionManager.instance.IsUnitSelected(unit);
        }

        //deselect the unit if it's selected:
        public override void Deselect()
        {
            SelectionManager.instance.DeselectUnit(unit);
        }

        //is the unit managed by this component a free one?
        public override bool IsFree() { return unit.IsFree(); }

        //get the minimap icon color of the unit (the faction color)
        public override Color GetMinimapIconColor() { return GameManager.Instance.Factions[unit.FactionID].FactionColor; }

        //called when the mouse hovers over the unit in order to show the hover health bar
        protected override void OnHoverHealthBarRequest()
        {
            if (unit.HealthComp.IsDead() == true) //if the unit is dead, do not show the hover health bar
                return;

            UIManager.instance.TriggerHoverHealthBar(true, this, unit.HealthComp.GetHoverHealthBarY()); //enable the hover health bar
            if (UIManager.instance.IsHoverSource(this)) //if the hover health bar was successfully enabled
                UIManager.instance.UpdateHoverHealthBar(unit.HealthComp.CurrHealth, unit.HealthComp.MaxHealth); //update it
        }
    }
}
