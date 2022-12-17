/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Manages the user interface elements in the map editing mode.
    /// </summary>
    public class MapEditingUIManager : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown dropdown;
        [SerializeField]
        private GameObject backButton;
        [SerializeField]
        private GameObject menuPinnedToggle;
        [SerializeField]
        private Toggle yawToggle;
        
        public GameObject mainPanel;
        public GameObject objectEditingPanel;
        public GameObject movePanel;
        public GameObject rotatePanel;
        public GameObject scalePanel;

        private GameObject currPanel = null;

        /// <summary>
        ///     Handles the back button input.
        /// </summary>
        public void HandleBackButton()
        {
            if (currPanel == objectEditingPanel)
            {
                SetCurrentPanel(mainPanel);
                MapEditingManager.Instance.SetSelectedObject(null);
            }
            else if (currPanel == movePanel)
            {
                MapEditingManager.Instance.SetMovingAllowed(false);
                SetCurrentPanel(objectEditingPanel);
            }
            else if (currPanel == rotatePanel)
            {
                yawToggle.isOn = true;
                MapEditingManager.Instance.SetRotationYAxis(yawToggle);
                MapEditingManager.Instance.SetRotationAllowed(false);
                SetCurrentPanel(objectEditingPanel);
            }
            else if (currPanel == scalePanel)
            {
                MapEditingManager.Instance.SetScalingAllowed(false);
                SetCurrentPanel(objectEditingPanel);
            }
        }

        public void OnValueChanged(TMP_Dropdown dropdown)
        {
            int value = dropdown.value - 1;
            MapEditingManager.Instance.AddObject(value);
            dropdown.SetValueWithoutNotify(0);
        }

        /// <summary>
        ///     Shows or hides the menu.
        /// </summary>
        /// <param name="toggle"> The toggle which determines the visibility of the menu. </param>
        public void ToggleMenuVisibility(Toggle toggle)
        {
            bool visible = toggle.isOn;

            if (currPanel)
                currPanel.SetActive(visible);

            backButton.SetActive(visible);
            menuPinnedToggle.SetActive(visible);
            menuPinnedToggle.GetComponent<Toggle>().isOn = false;
        }

        /// <summary>
        ///     Initializes and enables its UI elements.
        /// </summary>
        public void EnableSelf()
        {
            gameObject.SetActive(true);
            SetCurrentPanel(mainPanel);
            InitializeDropdownItems();
        }

        /// <summary>
        ///     Resets UI elements to their default state and disables the owning GameObject.
        /// </summary>
        public void DisableSelf()
        {
            dropdown.ClearOptions();
            dropdown.SetValueWithoutNotify(0);
            SetCurrentPanel(null);
            gameObject.SetActive(false);
        }

        /// <summary>
        ///     Sets the current panel.
        /// </summary>
        /// <param name="panel"> The new panel. </param>
        public void SetCurrentPanel(GameObject panel)
        {
            bool wasActive = true;
            if (currPanel)
            {
                wasActive = currPanel.activeSelf;
                currPanel.SetActive(false);
            }

            if (panel)
            {
                if (wasActive)
                {
                    panel.SetActive(true);
                }
                else
                {
                    panel.SetActive(false);
                }
            }

            currPanel = panel;

            if (currPanel)
            {
                if (currPanel == movePanel)
                {
                    if (DataManager.Instance.usingNreal)
                    {
                        UIManager.Instance.ShowNotification("Move the exhibit with your laser or by using the control buttons.",
                            "Hýbejte s exponátem táhnutím laseru nebo pomocí tlaèítek.", 3.0f);
                    }
                    else
                    {
                        UIManager.Instance.ShowNotification("Move the exhibit with your finger or by using the control buttons.",
                            "Hýbejte s exponátem posouváním prstu po displeji nebo pomocí tlaèítek.", 3.0f);
                    }
                }
                else if (currPanel == rotatePanel)
                {
                    if (DataManager.Instance.usingNreal)
                    {
                        UIManager.Instance.ShowNotification("Rotate the exhibit by clicking on it and moving the laser.",
                            "Rotujte exponát kliknutím na nìj a táhnutím laseru.", 5.0f);
                    }
                    else
                    {
                        UIManager.Instance.ShowNotification("Rotate the exhibit by clicking on in and moving your finger.",
                            "Rotujte exponát kliknutím na nìj a posouváním prstu.", 5.0f);
                    }
                }
                else if (currPanel == scalePanel)
                {
                    UIManager.Instance.ShowNotification("Scale the exhibit with the control buttons.",
                        "Škálujte exponát pomocí tlaèítek");
                }
                Debug.Log("Current panel: " + currPanel.name);
            }
            else
            {
                Debug.Log("No panel set");
            }
        }

        /// <summary>
        ///     Checks if the main panel is active.
        /// </summary>
        /// <returns> True if the main panel is active, false otherwise. </returns>
        public bool IsMainPanelActive()
        {
            return currPanel == mainPanel && mainPanel.activeSelf;
        }

        private void InitializeDropdownItems()
        {
            dropdown.ClearOptions();

            if (DataManager.Instance.currLanguage == Language.Czech)
            {
                dropdown.AddOptions(new List<string>() { "Vyberte exponát..." });
            }
            else
            {
                dropdown.AddOptions(new List<string>() { "Select the exhibit..." });
            }

            List<string> names = new List<string>();
            List<MovableObject> objects = DataManager.Instance.spawnableObjects;
            foreach (MovableObject obj in objects)
            {
                names.Add(obj.objectName);
            }
            dropdown.AddOptions(names);
            dropdown.SetValueWithoutNotify(0);
        }
    }
}

