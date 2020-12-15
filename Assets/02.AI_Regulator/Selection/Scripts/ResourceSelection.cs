using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public class ResourceSelection : SelectionEntity
    {
        private Resource resource; //the resource's main component
        public override void UpdateMainObject(GameObject mainObject) { resource = mainObject.GetComponent<Resource>(); } //update the main component
        public override GameObject GetMainObject() { return resource.gameObject; }

        //can the resource be selected?
        public override bool CanSelect()
        {
            return canSelect == true;
        }

        //called when the selection manager attempts to select the unit associated with this selection object
        protected override void OnSelected()
        {
            if (CanSelect() == false)
                return;

            resource.DisableSelectionFlash(); 

            SelectionManager.instance.SelectResource(resource);

            AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, ResourceManager.instance.ResourcesInfo[resource.ID].TypeInfo.SelectionAudio, false);
        }

        //is the resource currently selected?
        public override bool IsSelected()
        {
            return SelectionManager.instance.IsResourceSelected(resource);
        }

        //deselect the resource if it's selected:
        public override void Deselect()
        {
            if (IsSelected())
                SelectionManager.instance.DeselectResource();
        }

        public override bool IsFree() { return false; }

        //get the minimap icon color of the unit (the faction color)
        public override Color GetMinimapIconColor() { return ResourceManager.instance.ResourcesInfo[resource.ID].TypeInfo.MinimapIconColor; ; }

        //called when the mouse hovers over the resource in order to show the hover health bar
        protected override void OnHoverHealthBarRequest() { }
    }
}