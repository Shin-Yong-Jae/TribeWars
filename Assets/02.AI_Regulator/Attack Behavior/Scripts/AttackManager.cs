using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Attack Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class AttackManager : MonoBehaviour
    {
        public static AttackManager instance;

        [System.Serializable]
        public class UnitAttackRange
        {
            [SerializeField]
            private string code = "attack_range_code"; //unique code for this unit attack range
            public string GetCode () { return code; }

            [SerializeField]
            private float unitStoppingDistance = 2.0f; //stopping distance for target units
            [SerializeField]
            private float buildingStoppingDistance = 5.0f; //stopping distance when the unit has a target building to attack
            
            //get either the unit/building stopping distance
            public float GetStoppingDistance (bool unit)
            {
                return (unit == true) ? unitStoppingDistance : buildingStoppingDistance;
            }

            [SerializeField]
            private float moveOnAttackOffset = 3.0f; //when the attack unit can move and attack, the range of attack increases by this value
            public float GetMoveOnAttackOffset () { return moveOnAttackOffset; }

            [SerializeField]
            private float updateMvtDistance = 2.0f; //if the unit is moving towards a target and it changes its position by more than this distance, the attacker's movement will be recalculated
            public float GetUpdateMvtDistance () { return updateMvtDistance; }

            [SerializeField]
            private MovementManager.Formations movemnetFormation = MovementManager.Formations.circular; //the movement formation that units from this range type will have when moving to attack
            public MovementManager.Formations GetFormation() { return movemnetFormation; }
        }
        [SerializeField]
        private UnitAttackRange[] rangeTypes = new UnitAttackRange[0];

        //returns the unit attack range type:
        public UnitAttackRange GetRangeType(string code)
        {
            foreach(UnitAttackRange uar in rangeTypes)
                if (uar.GetCode() == code) //if the code matches, return a pointer to the range type
                    return uar;
            return null;
        }

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(this);
        }
    }
}