/*
    Author: Dominik Truong
    Year: 2022
*/

using Firebase.Firestore;
using Immersal.AR;
using Immersal.REST;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ExhibitionManagerAR
{
    public enum Language { English, Czech }

    /// <summary>
    ///     
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("No DataManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }
        private static DataManager instance = null;

        [SerializeField]
        private LocalizerBase localizer;
        public bool usingNreal = false;
        public List<MovableObject> spawnableObjects;

        // Map and content data
        [HideInInspector]
        public SDKJob mapToLoad;
        [HideInInspector]
        public string contentToLoad;
        [HideInInspector]
        public Dictionary<string, ARMap> contentToMapDict = new Dictionary<string, ARMap>();
        [HideInInspector]
        public Dictionary<string, GameObject> contentToParentDict = new Dictionary<string, GameObject>();
        [HideInInspector]
        public Dictionary<string, ListenerRegistration> contentToListenerDict = new Dictionary<string, ListenerRegistration>();

        // Language, locale
        [HideInInspector]
        public Language currLanguage = Language.English;
        private bool localesLoaded = false;

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

        IEnumerator Start()
        {
            // Set the language
            string currLanguageStr = PlayerPrefs.GetString("lang");

            if (!string.IsNullOrEmpty(currLanguageStr))
            {
                currLanguage = (currLanguageStr.Equals("en")) ? Language.English : Language.Czech;
            }
            else
            {
                currLanguage = Language.English;
            }

            // Set the locale
            yield return LocalizationSettings.InitializationOperation;

            string localeToFind = (currLanguage == Language.English) ? "en" : "cs";
            for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
            {
                Locale locale = LocalizationSettings.AvailableLocales.Locales[i];
                if (locale.LocaleName.Contains(localeToFind))
                {
                    LocalizationSettings.SelectedLocale = locale;
                    localesLoaded = true;
                    break;
                }
            }
        }

        /// <summary>
        ///     Deletes all maps and content.
        /// </summary>
        public void ClearMapsAndContent(bool showNotification = false)
        {
            ARSpace[] arSpaces = FindObjectsOfType<ARSpace>();
            foreach (ARSpace arSpace in arSpaces)
            {
                foreach (Transform child in arSpace.transform)
                {
                    ARMap arMap = child.GetComponent<ARMap>();
                    if (arMap) arMap.FreeMap(true);
                }
                Destroy(arSpace.gameObject);
            }

            contentToMapDict.Clear();
            contentToParentDict.Clear();

            // Stop all listeners
            foreach (ListenerRegistration listener in contentToListenerDict.Values)
            {
                listener.Stop();
            }
            contentToListenerDict.Clear();

            if (showNotification)
                UIManager.Instance.ShowNotification("All scenes were deleted.", "Všechny scény byly smazány.", 1.5f);
        }

        /// <summary>
        ///     Checks if the given map and content combination has already been loaded into the scene.
        /// </summary>
        /// <param name="mapId"> ID of the map to check. </param>
        /// <param name="contentName"> Name of the content to check. </param>
        /// <returns> True if the map and content exists, false otherwise. </returns>
        public bool IsMapAndContentAlreadyLoaded(string mapId, string contentName)
        {
            string key = mapId + "." + contentName;
            return contentToMapDict.ContainsKey(key);
        }

        public ARMap TryFindARMap(string mapId)
        {
            ARSpace[] arSpaces = FindObjectsOfType<ARSpace>();
            foreach (ARSpace arSpace in arSpaces)
            {
                foreach (Transform child in arSpace.transform)
                {
                    if (child.name.Contains(mapId))
                    {
                        ARMap map = child.GetComponent<ARMap>();
                        if (map) return map;
                    }
                }
            }
            return null;
        }

        /// <summary>
        ///     Switches language between English and Czech.
        /// </summary>
        public void SwitchLanguage()
        {
            if (!localesLoaded)
                return;

            currLanguage = (currLanguage == Language.English) ? Language.Czech : Language.English;
            string localeToFind = (currLanguage == Language.English) ? "en" : "cs";
            for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
            {
                Locale locale = LocalizationSettings.AvailableLocales.Locales[i];
                if (locale.LocaleName.Contains(localeToFind))
                {
                    LocalizationSettings.SelectedLocale = locale;
                    PlayerPrefs.SetString("lang", localeToFind);
                    break;
                }
            }

            Debug.Log("Switching language to: " + localeToFind);
        }

        /// <summary>
        ///     Saves a QR code on the device.
        /// </summary>
        /// <param name="qrCodeTex"> The QR code texture to save. </param>
        public void SaveQRCode(Texture2D qrCodeTex, string fileName)
        {
            if (qrCodeTex == null || string.IsNullOrEmpty(fileName))
            {
                Debug.Log("Content name is empty.");
                UIManager.Instance.ShowNotification("Scene name cannot be empty.", "Název scény nesmí být prázdný.");
                return;
            }

            Debug.Log("Saving QR code.");

            byte[] data = qrCodeTex.EncodeToPNG();
            string dirPath = Application.persistentDataPath + "/qrcodes";
            string filePath = dirPath + "/" + fileName + ".png";

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        ///     Resumes/pauses localization.
        /// </summary>
        /// <param name="enabled"> Resumes localization if true, pauses otherwise. </param>
        public void SetLocalization(bool enabled, bool showNotification = false)
        {
            if (enabled)
            {
                localizer.Resume();
                Debug.Log("Localization resumed.");

                if (showNotification)
                    UIManager.Instance.ShowNotification("Localization has been resumed.", "Lokalizace byla obnovena.", 2.5f, true);
                    
            }
            else
            {
                localizer.Pause();
                Debug.Log("Localization paused.");

                if (showNotification)
                    UIManager.Instance.ShowNotification("Localization has been paused.", "Lokalizace byla pozastavena.", 2.5f, true);
            }
        }

        /// <summary>
        ///     Checks if localization is enabled.
        /// </summary>
        /// <returns> True if localization is enabled, false otherwise. </returns>
        public bool IsLocalizing()
        {
            return localizer.IsLocalizing();
        }

    }
}
