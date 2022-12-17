/*
    Author: Dominik Truong
    Year: 2022
*/

using Firebase.Extensions;
using Firebase.Firestore;
using Immersal.AR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Handles map viewing (e.g. content loading).
    /// </summary>
    public class MapViewingManager : MonoBehaviour
    {
        public static MapViewingManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("No MapViewingManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }
        private static MapViewingManager instance = null;

        public bool mapsVisible = true;

        private ARMap currMap;
        private string currContent;

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
        ///     Loads virtual content.
        /// </summary>
        public void LoadContent()
        {
            if (!currMap)
            {
                Debug.LogError("Cannot load virtual content. No map was set.");
                return;
            }

            string docName = currMap.mapId + "." + currContent;
            FirebaseFirestore db = NetworkManager.Instance.db;
            db.Collection("Content").Document(docName).GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Debug.Log("Loading content from: " + snapshot.Id);
                    
                    Dictionary<string, object> jsonDict = snapshot.ToDictionary();
                    string jsonString = jsonDict["JSON"].ToString();

                    GameObject parent = new GameObject(currContent);
                    parent.transform.SetParent(currMap.transform);
                    parent.transform.localPosition = Vector3.zero;
                    parent.transform.localRotation = Quaternion.identity;
                    parent.transform.localScale = Vector3.one;
                    parent.AddComponent<InvisibleWallsManager>();

                    string key = snapshot.Id;
                    DataManager.Instance.contentToParentDict[key] = parent;

                    if (string.IsNullOrEmpty(jsonString))
                    {
                        UIManager.Instance.ShowNotification("Scene " + currContent + " has been loaded.", "Scéna " + currContent + " byla naètena.", 1.5f, true);
                        Debug.Log("There is no content to load.");
                        return;
                    }

                    ContentMetadata metadata = JsonUtility.FromJson<ContentMetadata>(jsonString);
                    for (int i = 0; i < metadata.positions.Count; i++)
                    {
                        int objectType = metadata.objectTypes[i];
                        Vector3 position = metadata.positions[i];
                        Vector3 rotation = metadata.rotations[i];
                        Vector3 scale = metadata.scales[i];

                        GameObject go = Instantiate(DataManager.Instance.spawnableObjects[objectType].gameObject, parent.transform);
                        go.transform.localRotation = Quaternion.Euler(rotation);
                        go.transform.localPosition = position;
                        go.transform.localScale = scale;

                        Vector3 localRotation = metadata.localRotations[i];
                        GameObject modelParent = go.GetComponent<MovableObject>().modelParent;
                        if (modelParent)
                            modelParent.transform.localRotation = Quaternion.Euler(localRotation);
                    }
                    UIManager.Instance.ShowNotification("Scene " + currContent + " has been loaded.", "Scéna " + currContent + " byla naètena.", 1.5f, true);

                    Debug.Log("Successfully loaded file.");
                }
                else
                {
                    Debug.Log("Document " + snapshot.Id + " does not exist.");
                }
            });
        }

        public void AddRealtimeListenerToContent()
        {
            Debug.Log("Adding a listener to content: " + currContent + " in map: " + currMap.mapId);

            string docName = currMap.mapId + "." + currContent;
            FirebaseFirestore db = NetworkManager.Instance.db;
            DocumentReference docRef = db.Collection("Content").Document(docName);
            ListenerRegistration listener = docRef.Listen(snapshot => {
                if (snapshot.Exists)
                {
                    Debug.Log(string.Format("Content {0} has been updated.", snapshot.Id));

                    Dictionary<string, object> jsonDict = snapshot.ToDictionary();
                    string jsonString = jsonDict["JSON"].ToString();
                    ReloadContent(jsonString, snapshot.Id);
                }
            });
            DataManager.Instance.contentToListenerDict[docName] = listener;
        }

        /// <summary>
        ///     Deletes data stored in the variables.
        /// </summary>
        public void DeleteLocalData()
        {
            currMap = null;
            currContent = null;
        }

        /// <summary>
        ///     Sets the current AR map and virtual content
        /// </summary>
        public void SetMapAndContent()
        {
            currContent = DataManager.Instance.contentToLoad;
            string key = DataManager.Instance.mapToLoad.id.ToString() + "." + currContent;
            currMap = DataManager.Instance.contentToMapDict[key];
        }

        private void ReloadContent(string jsonString, string key)
        {
            if (!DataManager.Instance.contentToParentDict.ContainsKey(key))
                return;

            GameObject parent = DataManager.Instance.contentToParentDict[key];

            // Delete old content
            foreach (Transform child in parent.transform)
            {
                Destroy(child.gameObject);
            }

            // Clear the invisible wall manager
            InvisibleWallsManager wallManager = parent.GetComponent<InvisibleWallsManager>();
            if (wallManager)
            {
                wallManager.invisibleWalls.Clear();
            }
            else
            {
                Debug.LogError("Invisible wall manager not found for this content.");
            }

            // Load new content
            if (string.IsNullOrEmpty(jsonString))
            {
                Debug.Log("There is no content to load.");
                return;
            }

            ContentMetadata metadata = JsonUtility.FromJson<ContentMetadata>(jsonString);
            for (int i = 0; i < metadata.positions.Count; i++)
            {
                int objectType = metadata.objectTypes[i];
                Vector3 position = metadata.positions[i];
                Vector3 rotation = metadata.rotations[i];
                Vector3 scale = metadata.scales[i];

                GameObject go = Instantiate(DataManager.Instance.spawnableObjects[objectType].gameObject, parent.transform);
                go.transform.localRotation = Quaternion.Euler(rotation);
                go.transform.localPosition = position;
                go.transform.localScale = scale;

                Vector3 localRotation = metadata.localRotations[i];
                GameObject modelParent = go.GetComponent<MovableObject>().modelParent;
                if (modelParent)
                    modelParent.transform.localRotation = Quaternion.Euler(localRotation);
            }
        }

    }
}

