/*
    Author: Dominik Truong
    Year: 2022
*/

using Firebase.Extensions;
using Firebase.Firestore;
using Immersal.AR;
using Immersal.REST;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Handles content copying.
    /// </summary>
    public class ContentCopyingManager : MonoBehaviour
    {
        public static ContentCopyingManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("No ContentCopyingManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }
        private static ContentCopyingManager instance = null;

        [SerializeField]
        private GameObject axesPrefab;

        [HideInInspector]
        public SDKJob receivingMapSDKJob;
        private string receivingMap = null;
        private ARMap sourceMap = null;
        private GameObject sourceMapParent = null;
        private string sourceContent = null;

        private float movementSpeed = 0.5f;
        private Vector3 movementDirection = Vector3.zero;
        private bool moving = false;

        private float rotationSpeed = 60.0f;
        private Vector3 rotationDirection = Vector3.zero;
        private bool rotating = false;

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

        void FixedUpdate()
        {
            if (StateManager.currState == State.ContentCopying)
            {
                if (sourceMapParent && moving)
                {
                    sourceMapParent.transform.position += movementDirection * movementSpeed * Time.deltaTime;
                }
                if (sourceMap && rotating)
                {
                    Vector3 newRotationVector = rotationDirection * rotationSpeed * Time.deltaTime;
                    sourceMap.transform.Rotate(newRotationVector.x, newRotationVector.y, newRotationVector.z);
                }
            }
        }

        /// <summary>
        ///     Loads virtual content of the source map.
        /// </summary>
        public void LoadSourceContent()
        {
            if (!sourceMap)
            {
                Debug.LogError("Cannot load virtual content. No map was set.");
                return;
            }

            string docName = sourceMap.mapId + "." + sourceContent;
            FirebaseFirestore db = NetworkManager.Instance.db;
            db.Collection("Content").Document(docName).GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Debug.Log("Loading content from: " + snapshot.Id);

                    Dictionary<string, object> jsonDict = snapshot.ToDictionary();
                    string jsonString = jsonDict["JSON"].ToString();

                    GameObject parent = new GameObject(sourceContent);
                    parent.transform.SetParent(sourceMap.transform);
                    parent.transform.localPosition = Vector3.zero;
                    parent.transform.localRotation = Quaternion.identity;
                    parent.transform.localScale = Vector3.one;
                    parent.AddComponent<InvisibleWallsManager>();

                    //string key = snapshot.Id;
                    //DataManager.Instance.contentToParentDict[key] = parent;

                    if (string.IsNullOrEmpty(jsonString))
                    {
                        UIManager.Instance.ShowNotification("Scene " + sourceContent + " has been loaded.", "Scéna " + sourceContent + " byla načtena.", 1.5f, true);
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
                    UIManager.Instance.ShowNotification("Scene " + sourceContent + " has been loaded.", "Scéna " + sourceContent + " byla načtena.", 1.5f, true);

                    Debug.Log("Successfully loaded file.");
                }
                else
                {
                    Debug.Log("Document " + snapshot.Id + " does not exist.");
                }
            });
        }

        /// <summary>
        ///     Deletes the virtual content and the source map.
        /// </summary>
        public void DeleteSourceData()
        {
            if (sourceMap && !sourceMap.mapId.ToString().Equals(receivingMap))
            {
                sourceMap.FreeMap(true);
                if (sourceMapParent) Destroy(sourceMapParent);
            } 
            else if (sourceMap && sourceMap.mapId.ToString().Equals(receivingMap))
            {
                Transform sourceContentTransform = sourceMap.transform.Find(sourceContent);
                if (sourceContentTransform)
                {
                    Destroy(sourceContentTransform.gameObject);
                }
                
                foreach (Transform child in sourceMapParent.transform)
                {
                    if (child.name.Contains("Axes"))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            sourceMap = null;
            sourceMapParent = null;
            sourceContent = null;
            DataManager.Instance.contentToMapDict.Clear();
            DataManager.Instance.contentToParentDict.Clear();
        }

        /// <summary>
        ///     Loads the map, into which the content will be copied.
        /// </summary>
        public void LoadReceivingMap()
        {
            SetReceivingMap(DataManager.Instance.mapToLoad);
            NetworkManager.Instance.LoadMap(true);
        }

        /// <summary>
        ///     Loads the map and content, which will be copied.
        /// </summary>
        public void LoadSourceMapAndContent()
        {
            NetworkManager.Instance.LoadMap();
        }

        /// <summary>
        ///     Copies the virtual content from the source map onto the receive map and saves the newly copied content.
        /// </summary>
        /// <param name="contentName"> The name of the newly created content. </param>        
        public void SaveContent(string contentName)
        {
            // Find the receving map gameobject
            GameObject receivingMapGO = null;
            ARSpace[] arSpaces = FindObjectsOfType<ARSpace>();
            foreach (ARSpace arSpace in arSpaces)
            {
                foreach (Transform child in arSpace.transform)
                {
                    if (child.name.Contains(receivingMap))
                        receivingMapGO = child.gameObject; 
                }
            }

            if (!receivingMapGO)
            {
                Debug.LogError("Could not find the receving map");
                return;
            }

            // Create an empty gameobject in the receving map as the center of the new virtual content
            GameObject receivingMapContentGO = new GameObject(contentName);
            receivingMapContentGO.transform.parent = receivingMapGO.transform;
            receivingMapContentGO.transform.localPosition = Vector3.zero;
            receivingMapContentGO.transform.localRotation = Quaternion.identity;
            receivingMapContentGO.transform.localScale = Vector3.one;

            GameObject sourceMapContentGO = null; 
            foreach (Transform child in sourceMap.transform)
            {
                if (child.name.Contains(sourceContent))
                    sourceMapContentGO = child.gameObject;
            }

            if (!sourceMapContentGO)
            {
                Debug.LogError("Could not find the source map");
                return;
            }

            // Copy the content from the source map onto the receiving map
            while (sourceMapContentGO.transform.childCount > 0)
            {
                Transform child = sourceMapContentGO.transform.GetChild(0);
                child.SetParent(receivingMapContentGO.transform);
            }

            // Delete the source map
            sourceMap.FreeMap();
            Destroy(sourceMapParent);

            // Save the content
            List<int> objectTypes = new List<int>();
            List<Vector3> positions = new List<Vector3>();
            List<Vector3> rotations = new List<Vector3>();
            List<Vector3> localRotations = new List<Vector3>();
            List<Vector3> scales = new List<Vector3>();

            foreach (Transform child in receivingMapContentGO.transform)
            {
                MovableObject obj = child.gameObject.GetComponent<MovableObject>();
                objectTypes.Add(obj.id);
                positions.Add(obj.transform.localPosition);
                rotations.Add(obj.transform.localRotation.eulerAngles);
                localRotations.Add(obj.modelParent.transform.localRotation.eulerAngles);
                scales.Add(obj.transform.localScale);
            }

            ContentMetadata metadata;
            metadata.positions = positions;
            metadata.rotations = rotations;
            metadata.localRotations = localRotations;
            metadata.scales = scales;
            metadata.objectTypes = objectTypes;

            string docName = receivingMap + "." + contentName;
            string jsonString = JsonUtility.ToJson(metadata, true);
            ContentMetadataFirestore data = new ContentMetadataFirestore { JSON = jsonString };
            FirebaseFirestore db = NetworkManager.Instance.db;
            db.Collection("Content").Document(docName).SetAsync(data);
        }

        /// <summary>
        ///     Deletes data stored in the variables.
        /// </summary>
        public void DeleteLocalData()
        {
            receivingMap = null;
            sourceMap = null;
            sourceMapParent = null;
            sourceContent = null;
            rotating = false;
            moving = false;
            DataManager.Instance.contentToMapDict.Clear();
            DataManager.Instance.contentToParentDict.Clear();
        }

        /// <summary>
        ///     Instantiates the axes prefab.
        /// </summary>
        public void SpawnAxesIntoSourceMap()
        {
            if (sourceMapParent && axesPrefab)
            {
                GameObject axes = Instantiate(axesPrefab, sourceMapParent.transform);
                axes.SetActive(false);
            }
        }

        /// <summary>
        ///    Destroys the ARSpace, so that the source map does not get localized (the user will move it manually) 
        /// </summary>
        public void DestroyARSpaceOfSourceMap()
        {
            if (sourceMap && !sourceMap.mapId.ToString().Equals(receivingMap))
            {
                sourceMap.FreeMap(false, false);
                ARSpace arSpace = sourceMapParent.GetComponent<ARSpace>();
                if (arSpace)
                {
                    Destroy(arSpace);
                }
            }
        }

        /// <summary>
        ///     Checks, if the content is being copied within the same map.
        /// </summary>
        /// <returns> True if the content is being copied within the same map, false otherwise. </returns>
        public bool IsCopyingContentWithinTheSameMap()
        {
            return sourceMap && sourceMap.mapId.ToString().Equals(receivingMap);
        }

        /// <summary>
        ///     Checks if the receiving map has been loaded.
        /// </summary>
        /// <returns> True if the receiving map has been loaded, false otherwise. </returns>
        public bool IsReceivingMapLoaded()
        {
            ARSpace[] arSpaces = FindObjectsOfType<ARSpace>();
            foreach (ARSpace arSpace in arSpaces)
            {
                foreach (Transform child in arSpace.transform)
                {
                    if (child.name.Contains(receivingMap))
                        return true;
                }
            }

            return false;
        }

        public void SetReceivingMap(SDKJob inMap)
        {
            receivingMapSDKJob = inMap;
            receivingMap = inMap.id.ToString();
        } 

        public void SetSourceMapAndContent()
        {
            sourceContent = DataManager.Instance.contentToLoad;
            string key = DataManager.Instance.mapToLoad.id.ToString() + "." + sourceContent;
            sourceMap = DataManager.Instance.contentToMapDict[key];
            sourceMapParent = sourceMap.transform.parent.gameObject;
        }

        public void SetMovementDirection(int directionId)
        {
            if (!sourceMapParent)
                return;

            switch (directionId)
            {
                case 0:
                    movementDirection = sourceMapParent.transform.right;
                    break;
                case 1:
                    movementDirection = -sourceMapParent.transform.right;
                    break;
                case 2:
                    movementDirection = -sourceMapParent.transform.forward;
                    break;
                case 3:
                    movementDirection = sourceMapParent.transform.forward;
                    break;
                case 4:
                    movementDirection = sourceMapParent.transform.up;
                    break;
                case 5:
                    movementDirection = -sourceMapParent.transform.up;
                    break;
                default:
                    movementDirection = Vector3.zero;
                    break;
            }

            moving = directionId != -1;
        }

        public void SetRotationDirection(int rotationId)
        {
            if (!sourceMapParent)
                return;

            switch (rotationId)
            {
                case 0:
                    rotationDirection = new Vector3(0.0f, -1.0f, 0.0f);
                    break;
                case 1:
                    rotationDirection = new Vector3(0.0f, 1.0f, 0.0f);
                    break;
                case 2:
                    rotationDirection = new Vector3(1.0f, 0.0f, 0.0f);
                    break;
                case 3:
                    rotationDirection = new Vector3(-1.0f, 0.0f, 0.0f);
                    break;
                default:
                    rotationDirection = Vector3.zero;
                    break;
            }

            rotating = rotationId != -1;
        }

        public void SetAxesVisibility(bool visible)
        {
            if (!sourceMapParent)
                return;

            GameObject axes = null;
            foreach (Transform child in sourceMapParent.transform)
            {
                if (child.name.Contains("Axes"))
                {
                    axes = child.gameObject;
                }
            }

            if (axes)
                axes.SetActive(visible);
        }
    }
}

