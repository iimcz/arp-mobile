/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionManagerAR
{
    public class SelectMapAndContentPage : Page
    {
        [SerializeField]
        private MapListController mapListController;
        [SerializeField]
        private ContentListController contentListController;
        [SerializeField]
        private GameObject selectButton;


        void OnEnable()
        {
            MapListController.onMapSelected += UpdateContentList;
            ContentListController.onContentSelected += ToggleSelectButton;
            NetworkManager.onIsContentLockedResolved += TryLoadContent;
        }

        void OnDisable()
        {
            MapListController.onMapSelected -= UpdateContentList;
            ContentListController.onContentSelected -= ToggleSelectButton;
            NetworkManager.onIsContentLockedResolved -= TryLoadContent;
        }

        public override void ResetPage()
        {
            contentListController.ClearContent();
            mapListController.UpdateMaps();
            selectButton.SetActive(false);
        }

        private void UpdateContentList(int mapId)
        {
            if (StateManager.currState == State.MainMenu && gameObject.activeSelf)
            {
                if (mapId < 0) // negative mapId is not valid
                {
                    Debug.Log("Removing all from content list.");
                    contentListController.ClearContent();
                    selectButton.SetActive(false);
                    return;
                }

                contentListController.UpdateContent();
            }
        }

        private void ToggleSelectButton(int value)
        {
            if (StateManager.currState == State.MainMenu && gameObject.activeSelf)
            {
                if (value >= 0)
                {
                    selectButton.SetActive(true);
                }
                else
                {
                    selectButton.SetActive(false);
                }
            }
        }

        private void TryLoadContent(string contentName, bool locked)
        {
            if (StateManager.currState != State.MainMenu || !gameObject.activeSelf)
                return;

            if (locked)
            {
                Debug.Log("Content is already being edited by someone else.");
                UIManager.Instance.ShowNotification("This scene is already being edited by someone else.",
                    "Tato scéna je již upravována jiným uživatelem.");
            }
            else
            {
                DataManager.Instance.contentToLoad = contentName;
                StateManager.Instance.SwitchToMapEditingState();
            }
        }

    }
}
