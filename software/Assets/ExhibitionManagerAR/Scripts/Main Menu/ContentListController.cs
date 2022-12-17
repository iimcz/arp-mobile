/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Handles the proper updating of the virtual content list drop down.
    /// </summary>
    [RequireComponent(typeof(TMP_Dropdown))]
    public class ContentListController : MonoBehaviour
    {
        public delegate void OnContentSelected(int value);
        public static OnContentSelected onContentSelected;

        private List<string> content = new List<string>();
        private TMP_Dropdown dropdown;

        void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        void OnEnable()
        {
            NetworkManager.onContentListDownloaded += OnContentListDownloaded;
        }

        void OnDisable()
        {
            NetworkManager.onContentListDownloaded -= OnContentListDownloaded;
        }

        public void OnValueChanged(TMP_Dropdown dropdown)
        {
            int value = dropdown.value - 1;

            if (value >= 0)
            {
                string contentName = content[value];
                if (StateManager.currState == State.MapViewing)
                {
                    string mapId = DataManager.Instance.mapToLoad.id.ToString();
                    DataManager.Instance.contentToLoad = contentName;
                    if (!DataManager.Instance.IsMapAndContentAlreadyLoaded(mapId, contentName))
                    {
                        NetworkManager.Instance.LoadMap();
                    }
                    else
                    {
                        Debug.Log("Content already loaded");
                        UIManager.Instance.ShowNotification("This scene is already loaded.", "Tato scéna je již naètená.");
                    }
                }
                else if (StateManager.currState == State.ContentCopying)
                {
                    DataManager.Instance.contentToLoad = contentName;
                }
            }

            onContentSelected?.Invoke(value);
        }

        /// <summary>
        ///     Updates the list of virtual content.
        /// </summary>
        public void UpdateContent()
        {
            Debug.Log("Updating content list");
            NetworkManager.Instance.CheckInternetConnection();
            ClearContent();
            Invoke("GetContent", 0.5f);
        }

        /// <summary>
        ///     Delete all content from the list.
        /// </summary>
        public void ClearContent()
        {
            dropdown.interactable = false;
            dropdown.ClearOptions();

            if (DataManager.Instance.currLanguage == Language.Czech)
            {
                dropdown.AddOptions(new List<string>() { "Naèítám scény..." });
            }
            else
            {
                dropdown.AddOptions(new List<string>() { "Loading scenes..." });
            }

            content.Clear();
        }

        public void OnSelectButtonClicked()
        {
            int value = dropdown.value - 1;

            if (value >= 0)
            {
                string contentName = content[value];
                NetworkManager.Instance.IsContentLocked(contentName);
            }
        }

        private void GetContent()
        {
            NetworkManager.Instance.GetContent();
        }

        private void OnContentListDownloaded(List<string> content)
        {
            if (StateManager.currState == State.MainMenu || 
                StateManager.currState == State.MapViewing ||
                StateManager.currState == State.ContentCopying)
            {
                dropdown.ClearOptions();
                if (DataManager.Instance.currLanguage == Language.Czech)
                {
                    if (content.Count <= 0)
                    {
                        dropdown.AddOptions(new List<string>() { "Žádné scény..." });
                    }
                    else
                    {
                        dropdown.AddOptions(new List<string>() { "Vyberte scénu..." });
                    }
                }
                else
                {
                    if (content.Count <= 0)
                    {
                        dropdown.AddOptions(new List<string>() { "No scenes..." });
                    }
                    else
                    {
                        dropdown.AddOptions(new List<string>() { "Select the scene..." });
                    }
                }

                this.content = content;
                dropdown.AddOptions(content);
                dropdown.interactable = true;
            }
        }

    }
}

