using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/* Terrain Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class TerrainManager : MonoBehaviour
    {
        public static TerrainManager instance;

        [SerializeField]
        private LayerMask groundTerrainMask = new LayerMask(); //layers used for the ground terrain objects
        public LayerMask GetGroundTerrainMask() { return groundTerrainMask; }

        [SerializeField]
        private GameObject flatTerrain = null; 
        //necessary for multiple unit selection and map navigation, it's an invisible plane placed under the map, must have a collider component on.
        public bool IsFlatTerrain(GameObject obj) { return obj == flatTerrain; }

        [SerializeField]
        private GameObject airTerrain = null; //necessary for flying units, its height determine where flying units will be moving
        public float GetFlyingHeight () { return airTerrain.transform.position.y; }

        //the map's approximate size (usually width*height).
        [SerializeField]
        private float mapSize = 16900;
        public float GetMapSize () { return mapSize; }

        void Awake ()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(this);
        }

        public float SampleHeight(Vector3 position, float radius, LayerMask navLayerMask)
        {
            //use sample position to sample the height of the navmesh at the provided position
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, radius*2.0f, navLayerMask))
            {
                return hit.position.y;
            }

            return position.y;
        }

        //determine if an object belongs to the terrain tiles: (only regarding ground terrain objects)
        public bool IsTerrainTile(GameObject obj)
        {
            return groundTerrainMask == (groundTerrainMask | (1 << obj.layer));
        }
    }
}