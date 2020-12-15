using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngineDemo
{
    public class ControlsMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject controlsMenu = null;

        public void ToggleControlsMenu ()
        {
            controlsMenu.SetActive(!controlsMenu.gameObject.activeInHierarchy);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                Time.timeScale = 5.0f;
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                Time.timeScale = 1.0f;
            }
        }
    }
}
