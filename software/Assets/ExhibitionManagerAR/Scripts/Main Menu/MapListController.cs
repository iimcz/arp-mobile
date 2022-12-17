/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Immersal.REST;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Handles the proper updating of the map list drop down.
    /// </summary>
    [RequireComponent(typeof(TMP_Dropdown))]
    public class MapListController : MonoBehaviour
    {
        public delegate void OnMapSelected(int mapId);
        public static OnMapSelected onMapSelected;

        private List<SDKJob> maps = new List<SDKJob>();
        private TMP_Dropdown dropdown;

        void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        void OnEnable()
        {
            NetworkManager.onMapListDownloaded += OnMapListDownloaded;
        }

        void OnDisable()
        {
            NetworkManager.onMapListDownloaded -= OnMapListDownloaded;
        }

        public void OnValueChanged(TMP_Dropdown dropdown)
        {
            int value = dropdown.value - 1;

            if (value >= 0)
            {
                if (StateManager.currState == State.MainMenu ||
                    StateManager.currState == State.MapViewing ||
                    StateManager.currState == State.ContentCopying)
                {
                    SDKJob map = maps[value];
                    DataManager.Instance.mapToLoad = map;
                }

            }
            onMapSelected?.Invoke(value);
        }

        /// <summary>
        ///     Updates the list of available maps.
        /// </summary>
        public void UpdateMaps()
        {
            Debug.Log("Updating map list");

            NetworkManager.Instance.CheckInternetConnection();

            dropdown.interactable = false;
            dropdown.ClearOptions();

            if (DataManager.Instance.currLanguage == Language.Czech)
            {
                dropdown.AddOptions(new List<string>() { "Naèítám místnosti..." });
            } 
            else
            {
                dropdown.AddOptions(new List<string>() { "Loading rooms..." });
            }

            maps.Clear();
            Invoke("GetMaps", 0.5f);
        }

        /// <summary>
        ///     Delete all maps from the list.
        /// </summary>
        public void ClearMaps()
        {
            dropdown.ClearOptions();

            if (DataManager.Instance.currLanguage == Language.Czech)
            {
                dropdown.AddOptions(new List<string>() { "Vyberte místnost..." });
            }
            else
            {
                dropdown.AddOptions(new List<string>() { "Select the room..." });
            }

            maps.Clear();
        }

        private void GetMaps()
        {
            NetworkManager.Instance.GetMaps();
        }

        private void OnMapListDownloaded(List<SDKJob> maps)
        {
            if (StateManager.currState == State.MainMenu || 
                StateManager.currState == State.MapViewing || 
                StateManager.currState == State.ContentCopying)
            {
                dropdown.ClearOptions();
                if (DataManager.Instance.currLanguage == Language.Czech)
                {
                    dropdown.AddOptions(new List<string>() { "Vyberte místnost..." });
                }
                else
                {
                    dropdown.AddOptions(new List<string>() { "Select the room..." });
                }

                this.maps = maps;
                List<string> names = new List<string>();
                foreach (SDKJob map in maps)
                {
                    names.Add(map.name);
                }
                dropdown.AddOptions(names);
                dropdown.interactable = true;
            }
        }

    }
}
