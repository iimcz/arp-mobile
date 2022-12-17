/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionManagerAR
{
    public class SelectMapPage : Page
    {
        [SerializeField]
        private MapListController mapListController;
        [SerializeField]
        private GameObject selectButton;

        void OnEnable()
        {
            MapListController.onMapSelected += ToggleSelectButton;
        }

        void OnDisable()
        {
            MapListController.onMapSelected -= ToggleSelectButton;
        }

        public override void ResetPage()
        {
            mapListController.UpdateMaps();
            selectButton.SetActive(false);
        }

        private void ToggleSelectButton(int mapId)
        {
            if (StateManager.currState == State.MainMenu && gameObject.activeSelf)
            {
                if (mapId >= 0) 
                {
                    selectButton.SetActive(true);
                }
                else
                {
                    selectButton.SetActive(false);
                }
            }
        } 

    }
}
