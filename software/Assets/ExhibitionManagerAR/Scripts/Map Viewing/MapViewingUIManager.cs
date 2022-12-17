/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Manages the user interface elements in the map viewing mode.
    /// </summary>
    public class MapViewingUIManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject selectLoadingOptionPanel;
        [SerializeField]
        private GameObject addMapAndContentPanel;
        [SerializeField]
        private GameObject backButton;
        [SerializeField]
        private GameObject menuPinnedToggle;
        [SerializeField]
        private MapListController mapListController;
        [SerializeField]
        private ContentListController contentListController;

        void OnEnable()
        {
            MapListController.onMapSelected += UpdateContentList;
            ContentListController.onContentSelected += DisablePanels;
        }

        void OnDisable()
        {
            MapListController.onMapSelected -= UpdateContentList;
            ContentListController.onContentSelected -= DisablePanels;
        }

        /// <summary>
        ///     Handles the back button input.
        /// </summary>
        public void HandleBackButton()
        {
            if (addMapAndContentPanel.activeSelf)
            {
                ToggleAddMapAndContentPanel();
            }
            else if (selectLoadingOptionPanel.activeSelf)
            {
                ToggleSelectLoadingOptionPanel();
            }
        }

        /// <summary>
        ///     Enables/disables the "Select Loading Option" Panel.
        /// </summary>
        public void ToggleSelectLoadingOptionPanel()
        {
            if (!addMapAndContentPanel.activeSelf)
                selectLoadingOptionPanel.SetActive(!selectLoadingOptionPanel.activeSelf);
        }

        /// <summary>
        ///     Enables/disables the "Add Map And Content" panel.
        /// </summary>
        public void ToggleAddMapAndContentPanel()
        {
            addMapAndContentPanel.SetActive(!addMapAndContentPanel.activeSelf);

            // Only update the list of maps, when the panel is active
            if (addMapAndContentPanel.activeSelf)
            {
                mapListController.UpdateMaps();
                contentListController.ClearContent();
            }
        }

        /// <summary>
        ///     Checks if any panels are active.
        /// </summary>
        /// <returns> True if at least one panel is active, false otherwise. </returns>
        public bool AreAnyPanelsActive()
        {
            return selectLoadingOptionPanel.activeSelf || addMapAndContentPanel.activeSelf;
        }

        /// <summary>
        ///     Shows or hides the menu.
        /// </summary>
        /// <param name="toggle"> The toggle which determines the visibility of the menu. </param>
        public void ToggleMenuVisibility(Toggle toggle)
        {
            bool visible = toggle.isOn;

            foreach (Transform child in transform)
            {
                if (child.name.Contains("Toggle Menu Visibility")) 
                    continue;

                if (visible && child.name.Contains("Add Map And Content Panel"))
                    continue;

                if (visible && child.name.Contains("Select Loading Option Panel"))
                    continue;

                child.gameObject.SetActive(visible);
            }

            backButton.SetActive(visible);
            menuPinnedToggle.SetActive(visible);
            menuPinnedToggle.GetComponent<Toggle>().isOn = false;
        }

        /// <summary>
        ///     Initializes and enables its UI elements.
        /// </summary>
        public void EnableSelf()
        {
            addMapAndContentPanel.SetActive(false);
            selectLoadingOptionPanel.SetActive(false);
            gameObject.SetActive(true);
        }

        /// <summary>
        ///     Resets UI elements to their default state and disables the owning GameObject.
        /// </summary>
        public void DisableSelf()
        {
            gameObject.SetActive(false);
        }

        private void UpdateContentList(int mapId)
        {
            if (StateManager.currState == State.MapViewing)
            {
                if (mapId < 0) // negative mapId is not valid
                {
                    Debug.Log("Removing all from content list.");
                    contentListController.ClearContent();
                    return;
                }

                Debug.Log("Updating content list.");
                contentListController.UpdateContent();
            }
        }

        private void DisablePanels(int value)
        {
            if (StateManager.currState != State.MapViewing)
                return;

            if (value >= 0)
            {
                // This order must be kept
                ToggleAddMapAndContentPanel();
                ToggleSelectLoadingOptionPanel();
            }
        }

    }
}

