/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionManagerAR
{
    public enum State { MainMenu, MapEditing, MapViewing, QRCodeReading, ContentCopying }

    /// <summary>
    ///     Handles switching between states.    
    /// </summary>
    public class StateManager : MonoBehaviour
    {
        public static StateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("No StateManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }
        private static StateManager instance = null;

        public static State currState = State.MainMenu;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                DestroyImmediate(this);
                return;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackButton();
            }
        }

        void OnApplicationFocus(bool focus)
        {
            // If the app goes into the background while editing a map, the map could stay locked forever,
            // so unlock it before going to background
            if (!focus && currState == State.MapEditing)
            {
                //HandleBackButton();
                SwitchToMainMenuState();
            }
        }

        /// <summary>
        ///     Switches to the main menu state.
        /// </summary>
        public void SwitchToMainMenuState()
        {
            Debug.Log("Switching to main menu state.");

            if (currState == State.MapEditing)
            {
                DataManager.Instance.ClearMapsAndContent();
                MapEditingManager.Instance.DeleteLocalData();

                // Unlock the content so that other users can edit it
                string mapId = DataManager.Instance.mapToLoad.id.ToString();
                string contentName = DataManager.Instance.contentToLoad;
                NetworkManager.Instance.SetContentLocked(mapId, contentName, false);
            }
            else if (currState == State.MapViewing)
            {
                DataManager.Instance.ClearMapsAndContent();
                MapViewingManager.Instance.DeleteLocalData();
            }
            else if (currState == State.QRCodeReading)
            {
                QRCodeReadingManager.Instance.ResetAndStopScanning();
            }
            else if (currState == State.ContentCopying)
            {
                DataManager.Instance.ClearMapsAndContent();
                ContentCopyingManager.Instance.DeleteLocalData();
            }

            UIManager.Instance.SwitchToMainMenuState();
            currState = State.MainMenu;
        }

        /// <summary>
        ///     Switches to the map editing state.
        /// </summary>
        public void SwitchToMapEditingState()
        {
            Debug.Log("Switching to map editing state.");

            // Just in case
            DataManager.Instance.ClearMapsAndContent();
            DataManager.Instance.SetLocalization(true);

            // Lock the content so that no other user can edit it
            string mapId = DataManager.Instance.mapToLoad.id.ToString();
            string contentName = DataManager.Instance.contentToLoad;
            NetworkManager.Instance.SetContentLocked(mapId, contentName, true);

            // Load the map, set the selected object to null
            NetworkManager.Instance.LoadMap();
            MapEditingManager.Instance.SetSelectedObject(null);
            UIManager.Instance.SwitchToMapEditingState();
            currState = State.MapEditing;
        }

        /// <summary>
        ///     Switches to the map viewing state.
        /// </summary>
        public void SwitchToMapViewingState()
        {
            Debug.Log("Switching to map viewing state.");

            // Maps should always be visible by default
            if (currState == State.MainMenu)
            {
                // Just in case
                DataManager.Instance.ClearMapsAndContent(); 
                DataManager.Instance.SetLocalization(true);

                MapViewingManager.Instance.mapsVisible = true;
            }
            else if (currState == State.QRCodeReading)
            {
                QRCodeReadingManager.Instance.ResetAndStopScanning();
            }

            UIManager.Instance.SwitchToMapViewingState();
            currState = State.MapViewing;
        }

        /// <summary>
        ///     Switches to the QR code reading state.
        /// </summary>
        public void SwitchToQRCodeReadingState()
        {
            Debug.Log("Switching to QR code reading state.");

            UIManager.Instance.SwitchToQRCodeReadingState();
            currState = State.QRCodeReading;
            QRCodeReadingManager.Instance.StartScanning();
        }

        /// <summary>
        ///     Switches to the copying content state.
        /// </summary>
        public void SwitchToContentCopyingState(bool fromQRCodeReadingAndSuccessful = false)
        {
            Debug.Log("Switching to content copying state.");

            if (currState == State.MainMenu)
            {
                // Just in case
                DataManager.Instance.ClearMapsAndContent();
                DataManager.Instance.SetLocalization(true);
            }
            else if (currState == State.QRCodeReading)
            {
                QRCodeReadingManager.Instance.ResetAndStopScanning();
            }

            UIManager.Instance.SwitchToContentCopyingState(fromQRCodeReadingAndSuccessful);
            currState = State.ContentCopying;
        }

        /// <summary>
        ///     Quits the application.
        /// </summary>
        public void Quit()
        {
            Debug.Log("Quitting application.");

            // Doesn't work on Nreal glasses, use their own implementation for quitting
            if (!DataManager.Instance.usingNreal)
            {
                Application.Quit();
            }
            else
            {
                UIManager.Instance.ShowNotification("To quit the application, press the DOWN button", "Pro ukonèení aplikace stisknìte tlaèítko DOLÙ");
            }
        }

        /// <summary>
        ///     Handles the back button input.
        /// </summary>
        public void HandleBackButton()
        {
            UIManager.Instance.HandleBackButton();
        }

    }
}

