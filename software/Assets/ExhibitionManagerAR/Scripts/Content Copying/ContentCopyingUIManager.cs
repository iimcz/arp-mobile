/*
    Author: Dominik Truong
    Year: 2022
*/

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ExhibitionManagerAR
{
    public class ContentCopyingUIManager : MonoBehaviour
    {
        // Panels
        [SerializeField]
        private GameObject selectReceivingMapPanel;
        [SerializeField]
        private GameObject localizeReceivingMapPanel;
        [SerializeField]
        private GameObject selectLoadingOptionPanel;
        [SerializeField]
        private GameObject selectSourceMapPanel;
        [SerializeField]
        private GameObject automaticAlignmentPanel;
        [SerializeField]
        private GameObject alignMapsPanel;
        [SerializeField]
        private GameObject movePanel;
        [SerializeField]
        private GameObject rotatePanel;
        [SerializeField]
        private GameObject newContentPanel;
        [SerializeField]
        private NewContentPage newContentPage;
        private GameObject currPanel = null;

        // Receving and source map data
        [SerializeField]
        private MapListController mapListControllerReceiver;
        [SerializeField]
        private GameObject selectReceivingMapButton;
        [SerializeField]
        private MapListController mapListControllerSource;
        [SerializeField]
        private ContentListController contentListControllerSource;
        [SerializeField]
        private GameObject selectSourceMapButton;

        // Other
        [SerializeField]
        private GameObject automaticAlignmentPanelHeader;
        [SerializeField]
        private GameObject automaticAlignmentPanelHeaderNotLocalized;
        [SerializeField]
        private TMP_Text automaticAlignmentPanelHeaderText;
        [SerializeField]
        private GameObject automaticAlignmentPanelYesButton;
        [SerializeField]
        private GameObject automaticAlignmentPanelNoButton;

        void OnEnable()
        {
            MapListController.onMapSelected += ToggleSelectReceivingMapButton;
            MapListController.onMapSelected += UpdateContentList;
            ContentListController.onContentSelected += ToggleSelectSourceMapButton;
        }

        void OnDisable()
        {
            MapListController.onMapSelected -= ToggleSelectReceivingMapButton;
            MapListController.onMapSelected -= UpdateContentList;
            ContentListController.onContentSelected -= ToggleSelectSourceMapButton;
        }

        /// <summary>
        ///     Switches between panels.
        /// </summary>
        public void SwitchPanel(GameObject inPanel)
        {
            if (currPanel)
            {
                currPanel.SetActive(false);
            }
            if (inPanel)
            {
                inPanel.SetActive(true);
            }

            if (inPanel == selectReceivingMapPanel)
            {
                SwitchToSelectReceivingMapPanel();
            }
            else if (inPanel == localizeReceivingMapPanel)
            {
                SwitchToLocalizeReceivingMapPanel();
            }
            else if (inPanel == selectLoadingOptionPanel)
            {
                SwitchToSelectLoadingOptionPanel();
            }
            else if (inPanel == selectSourceMapPanel)
            {
                SwitchToSelectSourceMapPanel();
            }
            else if (inPanel == automaticAlignmentPanel)
            {
                SwitchToAutomaticAlignmentPanel();
            }
            else if (inPanel == alignMapsPanel)
            {
                SwitchToAlignMapsPanel();
            }
            else if (inPanel == movePanel || inPanel == rotatePanel)
            {
                if (ContentCopyingManager.Instance.IsCopyingContentWithinTheSameMap())
                {
                    UIManager.Instance.ShowNotification("You are copying content within the same room, therefore no alignment is necessary. " +
                        "Please, press the Done button in the previous menu to finish the copying process.",
                        "Kopírujete obsah v rámci stejné mapy, zarovnání tedy není potřeba. " +
                        "Pro dokončení procesu kopírování prosím klikněte na tlačítko Hotovo v předchozím menu.", 8.0f);
                }

                if (inPanel == movePanel)
                    SwitchToMovePanel();
            }
            else if (inPanel == newContentPanel)
            {
                SwitchToNewContentPanel();
            }

            currPanel = inPanel;
        }

        /// <summary>
        ///     Handles the back button input.
        /// </summary>
        public void HandleBackButton()
        {
            if (currPanel == localizeReceivingMapPanel)
            {
                SwitchPanel(selectReceivingMapPanel);
            }
            else if (currPanel == selectLoadingOptionPanel)
            {
                SwitchPanel(localizeReceivingMapPanel);
            }
            else if (currPanel == selectSourceMapPanel)
            {
                SwitchPanel(selectLoadingOptionPanel);
            }
            else if (currPanel == automaticAlignmentPanel)
            {
                ContentCopyingManager.Instance.DeleteSourceData();
                SwitchPanel(selectLoadingOptionPanel);
            }
            else if (currPanel == alignMapsPanel)
            {
                ContentCopyingManager.Instance.DeleteSourceData();
                SwitchPanel(selectLoadingOptionPanel);
            }
            else if (currPanel == movePanel || currPanel == rotatePanel)
            {
                SwitchPanel(alignMapsPanel);
            }
            else if (currPanel == newContentPanel)
            {
                SwitchPanel(alignMapsPanel);
            }
        }

        /// <summary>
        ///     Switches to the select receiving map panel.
        /// </summary>
        public void SwitchToSelectReceivingMapPanel()
        {
            DataManager.Instance.ClearMapsAndContent();
            selectReceivingMapButton.SetActive(false);
            mapListControllerReceiver.UpdateMaps();
        }

        /// <summary>
        ///     Switches to the localize receiving map panel.
        /// </summary>
        public void SwitchToLocalizeReceivingMapPanel()
        {
            DataManager.Instance.SetLocalization(true);
        }

        /// <summary>
        ///     Switches to the select loading option panel.
        /// </summary>
        public void SwitchToSelectLoadingOptionPanel()
        {
            // NOTE: Uncomment if needed
            //DataManager.Instance.SetLocalization(false);
        }

        /// <summary>
        ///     Switches to the select source map panel.
        /// </summary>
        public void SwitchToSelectSourceMapPanel()
        {
            selectSourceMapButton.SetActive(false);
            contentListControllerSource.ClearContent();
            mapListControllerSource.UpdateMaps();
        }

        /// <summary>
        ///     Switches to the automatic alignment map panel.
        /// </summary>
        public void SwitchToAutomaticAlignmentPanel()
        {
            automaticAlignmentPanelHeader.SetActive(true);
            automaticAlignmentPanelHeaderNotLocalized.SetActive(false);
            automaticAlignmentPanelYesButton.SetActive(false);
            automaticAlignmentPanelNoButton.SetActive(false);
            StartCoroutine(AutomaticMappingCoroutine());
        }

        /// <summary>
        ///     Switches to the align maps panel.
        /// </summary>
        public void SwitchToAlignMapsPanel()
        {
            ContentCopyingManager.Instance.DestroyARSpaceOfSourceMap();
            ContentCopyingManager.Instance.SetAxesVisibility(false);
        }

        /// <summary>
        ///     Switches to the move panel.
        /// </summary>
        public void SwitchToMovePanel()
        {
            ContentCopyingManager.Instance.SetAxesVisibility(true);
        }
        /// <summary>
        ///     Switches to the new content panel.
        /// </summary>
        public void SwitchToNewContentPanel()
        {
            DataManager.Instance.mapToLoad = ContentCopyingManager.Instance.receivingMapSDKJob;
            newContentPage.ResetPage();
        }

        /// <summary>
        ///     Checks if the select receiving map panel is active.
        /// </summary>
        /// <returns> True if the panel is active, false otherwise. </returns>
        public bool IsSelectReceivingMapPanelActive()
        {
            return selectReceivingMapPanel.activeSelf;
        }

        /// <summary>
        ///     Initializes and enables its UI elements.
        /// </summary>
        public void EnableSelf()
        {
            gameObject.SetActive(true);
            SwitchPanel(selectReceivingMapPanel);
        }

        /// <summary>
        ///     Initializes and enables its UI elements after switching from the QR code reading state.
        /// </summary>
        /// <param name="successful"> If true, then the QR code has been read successfully. </param>
        public void EnableSelfAfterQRCodeReading(bool successful)
        {
            if (successful)
            {
                gameObject.SetActive(true);
                SwitchPanel(automaticAlignmentPanel);
            }
            else
            {
                gameObject.SetActive(true);
                SwitchPanel(selectLoadingOptionPanel);
            }
        }

        /// <summary>
        ///     Resets UI elements to their default state and disables the owning GameObject.
        /// </summary>
        public void DisableSelf()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        ///     Handles the OnClick event of the Done button in the localize receiving map panel.
        /// </summary>
        public void OnDoneButtonClicked()
        {
            if (ContentCopyingManager.Instance.IsReceivingMapLoaded())
            {
                SwitchPanel(selectLoadingOptionPanel);
            }
            else
            {
                UIManager.Instance.ShowNotification("Wait until the scene is loaded.", "Počkejte, dokud se scéna nenačte.");
            }
        }

        /// <summary>
        ///     Handles the OnClick event of the Close button which disables the gameobject given in the parameter objToClose.
        /// </summary>
        /// <param name="objToClose"></param>
        public void OnCloseButtonClicked(GameObject objToClose)
        {
            if (objToClose)
                objToClose.SetActive(false);
        }

        private void ToggleSelectReceivingMapButton(int mapId)
        {
            if (StateManager.currState == State.ContentCopying && selectReceivingMapPanel.activeSelf)
            {
                if (mapId >= 0)
                {
                    selectReceivingMapButton.SetActive(true);
                }
                else
                {
                    selectReceivingMapButton.SetActive(false);
                }
            }
        }

        private void ToggleSelectSourceMapButton(int mapId)
        {
            if (StateManager.currState == State.ContentCopying && selectSourceMapPanel.activeSelf)
            {
                if (mapId >= 0)
                {
                    selectSourceMapButton.SetActive(true);
                }
                else
                {
                    selectSourceMapButton.SetActive(false);
                }
            }
        }

        private void UpdateContentList(int mapId)
        {
            if (StateManager.currState == State.ContentCopying && selectSourceMapPanel.activeSelf)
            {
                if (mapId < 0) // negative mapId is not valid
                {
                    Debug.Log("Removing all from content list.");
                    contentListControllerSource.ClearContent();
                    selectSourceMapButton.SetActive(false);
                    return;
                }

                contentListControllerSource.UpdateContent();
            }
        }

        private IEnumerator AutomaticMappingCoroutine()
        {
            yield return new WaitForSecondsRealtime(15.0f);

            if (currPanel == automaticAlignmentPanel)
            {

                if (DataManager.Instance.currLanguage == Language.Czech)
                {
                    automaticAlignmentPanelHeaderText.text = "Automatické zarovnání dokončeno. Pokud není zarovnání optimální, stiskněte tlačítko Ne a zarovnejte místnosti manuálně.";
                }
                else
                {
                    automaticAlignmentPanelHeaderText.text = "Automatic alignment is finished. If the alignment is not optimal, press the No button and align the rooms manually";
                }

                automaticAlignmentPanelHeaderNotLocalized.SetActive(true);
                automaticAlignmentPanelHeader.SetActive(false);
                automaticAlignmentPanelYesButton.SetActive(true);
                automaticAlignmentPanelNoButton.SetActive(true);
            }
        }
    }
}
