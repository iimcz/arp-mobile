/*
    Author: Dominik Truong
    Year: 2022
*/

using Immersal.AR;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Manages the user interface.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("No UIManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }
        private static UIManager instance = null;

        [SerializeField]
        private MainMenuUIManager mainMenuUIManager;
        [SerializeField]
        private MapEditingUIManager mapEditingUIManager;
        [SerializeField]
        private MapViewingUIManager mapViewingUIManager;
        [SerializeField]
        private QRCodeReadingUIManager qrCodeReadingUIManager;
        [SerializeField]
        private ContentCopyingUIManager contentCopyingUIManager;

        [SerializeField]
        private Page contentCreatedPage;

        [SerializeField]
        private GameObject loadingScreen;
        [SerializeField]
        private GameObject mainMenu;
        [SerializeField]
        private GameObject menuPinnedToggle;

        [SerializeField]
        private GameObject notification;
        [SerializeField]
        private TMP_Text notificationText;

        [SerializeField]
        private GameObject confirmationDialog;

        private bool notificationOn = false;

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

        /// <summary>
        ///     Switches the UI to the main menu state
        /// </summary>
        public void SwitchToMainMenuState()
        {
            if (StateManager.currState == State.MapEditing)
            {
                mapEditingUIManager.DisableSelf();
                mainMenuUIManager.gameObject.SetActive(true);
            }
            else if (StateManager.currState == State.MapViewing)
            {
                mapViewingUIManager.DisableSelf();
                mainMenuUIManager.gameObject.SetActive(true);
            }
            else if (StateManager.currState == State.QRCodeReading)
            {
                qrCodeReadingUIManager.DisableSelf();
                mainMenuUIManager.gameObject.SetActive(true);
                mainMenuUIManager.ResetCurrPage();
            }
            else if (StateManager.currState == State.ContentCopying)
            {
                contentCopyingUIManager.DisableSelf();
                mainMenuUIManager.gameObject.SetActive(true);
            }
            else if (StateManager.currState == State.MainMenu)
            {
                mainMenuUIManager.DisableSelf();
                mainMenuUIManager.gameObject.SetActive(true);
                mainMenuUIManager.ResetCurrPage();
            }
        }

        /// <summary>
        ///     Switches the UI to the map editing state.
        /// </summary>
        public void SwitchToMapEditingState()
        {
            if (StateManager.currState == State.MainMenu)
            {
                mainMenuUIManager.DisableSelf();
                mapEditingUIManager.EnableSelf();
            }
            else if (StateManager.currState == State.QRCodeReading)
            {
                mainMenuUIManager.DisableSelf();
                qrCodeReadingUIManager.DisableSelf();
                mapEditingUIManager.EnableSelf();
            }
        }

        /// <summary>
        ///     Switches the UI to the map viewing state.
        /// </summary>
        public void SwitchToMapViewingState()
        {
            if (StateManager.currState == State.MainMenu)
            {
                mainMenuUIManager.DisableSelf();
                mapViewingUIManager.EnableSelf();
                UIManager.Instance.ShowNotification("This mode allows you to view created scenes (even multiple at once). " +
                    "The scenes are updated whenever someone else edits them.",
                "Tento režim umožòuje prohlížet vytvoøené scény (i více najednou). " +
                "Scény se aktualizují vždy, když v nich nìkdo jiný provede úpravy.", 5.0f, true);
            }
            else if (StateManager.currState == State.QRCodeReading)
            {
                qrCodeReadingUIManager.DisableSelf();
                mapViewingUIManager.EnableSelf();
            }
        }

        /// <summary>
        ///     Switches the UI to the QR code reading state.
        /// </summary>
        public void SwitchToQRCodeReadingState()
        {
            if (StateManager.currState == State.MainMenu)
            {
                mainMenuUIManager.gameObject.SetActive(false);
                qrCodeReadingUIManager.prevState = StateManager.currState;
                qrCodeReadingUIManager.EnableSelf();
            }
            else if (StateManager.currState == State.MapViewing)
            {
                mapViewingUIManager.DisableSelf();
                qrCodeReadingUIManager.prevState = StateManager.currState;
                qrCodeReadingUIManager.EnableSelf();
            }
            else if (StateManager.currState == State.ContentCopying)
            {
                contentCopyingUIManager.DisableSelf();
                qrCodeReadingUIManager.prevState = StateManager.currState;
                qrCodeReadingUIManager.EnableSelf();
            }
        }

        /// <summary>
        ///     Switches the UI to the copying content state.
        /// </summary>
        public void SwitchToContentCopyingState(bool fromQRCodeReadingAndSuccessful = false)
        {
            if (StateManager.currState == State.MainMenu)
            {
                mainMenuUIManager.DisableSelf();
                contentCopyingUIManager.EnableSelf();
            }
            else if (StateManager.currState == State.QRCodeReading)
            {
                qrCodeReadingUIManager.DisableSelf();
                contentCopyingUIManager.EnableSelfAfterQRCodeReading(fromQRCodeReadingAndSuccessful);
            }
        }

        /// <summary>
        ///     Switches the main menu to the content created page.
        /// </summary>
        public void SwitchMainMenuToContentCreatedPage()
        {
            mainMenuUIManager.SwitchPage(contentCreatedPage);
        }

        /// <summary>
        ///     Handles the back button input.
        /// </summary>
        public void HandleBackButton()
        {
            switch (StateManager.currState)
            {
                case (State.MainMenu):
                    mainMenuUIManager.SwitchToPreviousPage();
                    break;
                case (State.MapEditing):
                    if (mapEditingUIManager.IsMainPanelActive())
                    {
                        ToggleConfirmationDialog(true);
                    }
                    else
                    {
                        mapEditingUIManager.HandleBackButton();
                    }
                    break;
                case (State.MapViewing):
                    if (mapViewingUIManager.AreAnyPanelsActive())
                    {
                        mapViewingUIManager.HandleBackButton();
                    }
                    else
                    {
                        ToggleConfirmationDialog(true);
                    }
                    break;
                case (State.QRCodeReading):
                    if (qrCodeReadingUIManager.prevState == State.MainMenu)
                    {
                        StateManager.Instance.SwitchToMainMenuState();
                    }
                    else if (qrCodeReadingUIManager.prevState == State.MapViewing)
                    {
                        StateManager.Instance.SwitchToMapViewingState();
                    }
                    else if (qrCodeReadingUIManager.prevState == State.ContentCopying)
                    {
                        StateManager.Instance.SwitchToContentCopyingState(false);
                    }
                    break;
                case (State.ContentCopying):
                    if (contentCopyingUIManager.IsSelectReceivingMapPanelActive())
                    {
                        StateManager.Instance.SwitchToMainMenuState();
                    }
                    else
                    {
                        contentCopyingUIManager.HandleBackButton();
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        ///     Toggles maps (point cloud) visibility.    
        /// </summary>
        public void ToggleMapVisibility()
        {
            bool visible = false;

            ARSpace[] arSpaces = FindObjectsOfType<ARSpace>();
            foreach (ARSpace arSpace in arSpaces)
            {
                foreach (Transform child in arSpace.transform)
                {
                    ARMap arMap = child.GetComponent<ARMap>();
                    if (arMap == null)
                        continue;

                    if (arMap.renderMode == ARMap.RenderMode.EditorAndRuntime)
                    {
                        arMap.renderMode = ARMap.RenderMode.DoNotRender;
                        visible = false;
                    }
                    else
                    {
                        arMap.renderMode = ARMap.RenderMode.EditorAndRuntime;
                        visible = true;
                    }
                }
            }

            if (StateManager.currState == State.MapViewing)
            {
                MapViewingManager.Instance.mapsVisible = !MapViewingManager.Instance.mapsVisible;
            }

            if (visible || (StateManager.currState == State.MapViewing && MapViewingManager.Instance.mapsVisible))
            {
                ShowNotification("Room visibility has been turned on.", "Viditelnost místností byla zapnuta.", 2.5f, true);
            }
            else if (!visible || ((StateManager.currState == State.MapViewing && !MapViewingManager.Instance.mapsVisible)))
            {
                ShowNotification("Room visibility has been turned off.", "Viditelnost místností byla vypnuta.", 2.5f, true);
            }
        }

        /// <summary>
        ///     Resumes/pauses localization.
        /// </summary>
        public void ToggleLocalization()
        {
            if (DataManager.Instance.IsLocalizing())
            {
                DataManager.Instance.SetLocalization(false, true);
            }
            else
            {
                DataManager.Instance.SetLocalization(true, true);
            }
        }

        /// <summary>
        ///     Enables main menu UI elements.
        /// </summary>
        public void EnableMainMenu()
        {
            loadingScreen.SetActive(false);
            mainMenu.SetActive(true);
            
            if (DataManager.Instance.usingNreal)
            {
                menuPinnedToggle.SetActive(true);
            }
        }

        /// <summary>
        ///     Displays a notification with the given text.
        /// </summary>
        /// <param name="textEN"> Text to be displayed in English. </param>
        /// <param name="textCZ"> Text to be displayed in Czech. </param>
        /// <param name="notificationTime"> How long the notification should be displayed (optional). </param>
        public void ShowNotification(string textEN, string textCZ, float notificationTime = 2.5f, bool force = false)
        {
            if (notificationOn && !force)
                return;

            if (DataManager.Instance.currLanguage == Language.English)
            {
                StartCoroutine(ShowNotificationCoroutine(textEN, notificationTime));
            } else
            {
                StartCoroutine(ShowNotificationCoroutine(textCZ, notificationTime));
            }
        }

        /// <summary>
        ///     Hides the currently displayed notification.
        /// </summary>
        public void HideNotification()
        {
            if (notificationOn)
                notification.SetActive(false);
        }

        private IEnumerator ShowNotificationCoroutine(string text, float notificationTime)
        {
            notificationOn = true;
            notificationText.text = text;
            notification.SetActive(true);

            yield return new WaitForSecondsRealtime(notificationTime);

            notificationText.text = "";
            notification.SetActive(false);
            notificationOn = false;
        }

        public void ToggleConfirmationDialog(bool enabled)
        {
            confirmationDialog.SetActive(enabled);
        }

    }
}
