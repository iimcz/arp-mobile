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
using UnityEngine.UI;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Handles map editing, such as content placement, editing, saving or removal.
    /// </summary>
    public class MapEditingManager : MonoBehaviour
    {
        public static MapEditingManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("No MapEditingManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }
        private static MapEditingManager instance = null;

        [SerializeField]
        private MapEditingUIManager mapEditingUIManager;

        [HideInInspector]
        public List<MovableObject> contentList = new List<MovableObject>();
        [HideInInspector]
        public MovableObject selectedObject = null;

        [HideInInspector]
        public bool movingAllowed = false;
        [HideInInspector]
        public bool rotationAllowed = false;
        [HideInInspector]
        public bool rotationXAxis = false;
        [HideInInspector]
        public bool rotationYAxis = false;
        [HideInInspector]
        public bool rotationZAxis = false;
        [HideInInspector]
        public bool scalingAllowed = false;

        private ARMap currMap;
        private string currContent;
        private List<int> objectTypes = new List<int>();
        private List<Vector3> positions = new List<Vector3>();
        private List<Vector3> rotations = new List<Vector3>();
        private List<Vector3> localRotations = new List<Vector3>();
        private List<Vector3> scales = new List<Vector3>();

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
        ///     Spawns an object based on the given object ID. The object is spawned in front of the player.
        /// </summary>
        /// <param name="objectId"> ID of the object to be spawned. </param>
        public void AddObject(int objectId)
        {
            List<MovableObject> spawnableObjects = DataManager.Instance.spawnableObjects;
            if (objectId >= spawnableObjects.Count)
            {
                Debug.LogError("Cannot add this object. Object ID is out of range! Object ID: " + objectId);
                return;
            }
            if (!currMap)
            {
                Debug.Log("Cannot add this object. No map was set.");
                return;
            }

            string key = currMap.mapId + "." + currContent;
            GameObject parent = DataManager.Instance.contentToParentDict[key];
            GameObject objectToSpawn = spawnableObjects[objectId].gameObject;
            Transform cameraTransform = Camera.main.transform;

            GameObject spawnedObject = null;
            if (DataManager.Instance.usingNreal)
            {
                spawnedObject = Instantiate(objectToSpawn, cameraTransform.position + cameraTransform.forward * 2.2f, Quaternion.identity, parent.transform);

            }
            else
            {
                spawnedObject = Instantiate(objectToSpawn, cameraTransform.position + cameraTransform.forward * 1.5f, Quaternion.identity, parent.transform);
            }

            // Rotate the spawned object so that it's facing the camera but remove the x rotation
            spawnedObject.transform.LookAt(cameraTransform);
            Vector3 rotation = spawnedObject.transform.eulerAngles;
            spawnedObject.transform.eulerAngles = new Vector3(0.0f, rotation.y, rotation.z);

            if (spawnedObject.name.Contains("Invisible Wall"))
            {
                UIManager.Instance.ShowNotification("An invisible wall allows to add barriers through which the user cannot see. " +
                    "The arrow indicates the side, on which the scene is visible.", 
                    "Neviditelná zeï umožòuje pøidávat bariéry, za které uživatel neuvidí. " +
                    "Šipka indikuje stranu, na které je scéna viditelná.", 7.0f);
            }

            UIManager.Instance.ShowNotification("Exhibit " + objectToSpawn.name + " has been added to the scene.", "Exponát " + objectToSpawn.name + " byl pøidán do scény.", 1.5f);
        }

        /// <summary>
        ///     Saves virtual content metadata to the Firestore database.
        /// </summary>
        public void SaveContent()
        {
            if (!currMap || string.IsNullOrEmpty(currContent))
            {
                Debug.LogError("Cannot save virtual content. No map was set or no content was loaded.");
                return;
            }

            objectTypes.Clear();
            positions.Clear();
            rotations.Clear();
            localRotations.Clear();
            scales.Clear();
            foreach (MovableObject obj in contentList)
            {
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

            string docName = currMap.mapId + "." + currContent;
            string jsonString = JsonUtility.ToJson(metadata, true);
            ContentMetadataFirestore data = new ContentMetadataFirestore { JSON = jsonString };
            FirebaseFirestore db = NetworkManager.Instance.db;
            db.Collection("Content").Document(docName).SetAsync(data);
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

        /// <summary>
        ///     Deletes all virtual content.
        /// </summary>
        public void DeleteAllContent()
        {
            List<MovableObject> copy = new List<MovableObject>();

            foreach (MovableObject obj in contentList)
            {
                copy.Add(obj);
            }

            foreach (MovableObject obj in copy)
            {
                obj.RemoveObject();
            }

            UIManager.Instance.ShowNotification("All exhibits have been deleted.", "Všechny exponáty byly smazány.", 1.5f);
        }

        /// <summary>
        ///     Deletes data stored in the variables.
        /// </summary>
        public void DeleteLocalData()
        {
            SetMovingAllowed(false);
            SetRotationAllowed(false);
            SetScalingAllowed(false);
            currMap = null;
            currContent = null;
            contentList.Clear();
            objectTypes.Clear();
            positions.Clear();
            rotations.Clear();
            localRotations.Clear();
            scales.Clear();
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


        /// <summary>
        ///     Sets the object that the user has selected.
        /// </summary>
        /// <param name="inObject"> Object to be selected. </param>
        public void SetSelectedObject(MovableObject inObject)
        {
            // Can't select another object when already editting one
            if (movingAllowed || rotationAllowed || scalingAllowed)
                return;

            if (inObject == null) // No object selected
            {
                if (selectedObject) 
                    selectedObject.ToggleSelectionIndicatorVisibility(false);

                selectedObject = inObject;
                mapEditingUIManager.SetCurrentPanel(mapEditingUIManager.mainPanel);
            }
            else if (inObject == selectedObject) // The same object selected -> deselect it
            {
                if (selectedObject)
                    selectedObject.ToggleSelectionIndicatorVisibility(false);

                selectedObject = null;
                mapEditingUIManager.SetCurrentPanel(mapEditingUIManager.mainPanel);
            }
            else // Other object selected
            {
                if (selectedObject)
                    selectedObject.ToggleSelectionIndicatorVisibility(false);

                inObject.ToggleSelectionIndicatorVisibility(true);
                selectedObject = inObject;
                mapEditingUIManager.SetCurrentPanel(mapEditingUIManager.objectEditingPanel);
            }

            if (selectedObject)
            {
                Debug.Log("Selected object: " + selectedObject.name);
            }
            else
            {
                Debug.Log("No object selected");
            }
        }

        /// <summary>
        ///     Sets the direction in which the selected object will move.
        /// </summary>
        /// <param name="directionId"> The direction in which the selected object will move. </param>
        public void SetSelectedObjectDirection(int directionId)
        {
            if (!selectedObject)
                return;

            // The right and left directions are flipped, so are the forward and backward.
            // This is because when the object is spawned, it faces camera, so the forward vector
            // is pointing towards the camera.
            switch (directionId)
            {
                case 0:
                    selectedObject.SetObjectDirection(selectedObject.transform.right);
                    break;
                case 1:
                    selectedObject.SetObjectDirection(-selectedObject.transform.right);
                    break;
                case 2:
                    selectedObject.SetObjectDirection(selectedObject.transform.forward);
                    break;
                case 3:
                    selectedObject.SetObjectDirection(-selectedObject.transform.forward);
                    break;
                case 4:
                    selectedObject.SetObjectDirection(selectedObject.transform.up);
                    break;
                case 5:
                    selectedObject.SetObjectDirection(-selectedObject.transform.up);
                    break;
                default:
                    selectedObject.SetObjectDirection(Vector3.zero);
                    break;
            }
        }

        /// <summary>
        ///     Deletes the selected object.
        /// </summary>
        public void DeleteSelectedObject()
        {
            if (selectedObject)
                selectedObject.RemoveObject();

            StateManager.Instance.HandleBackButton();
        }

        /// <summary>
        ///     Starts scaling the selected object up.
        /// </summary>
        public void ScaleSelectedObjectUp()
        {
            if (selectedObject)
                selectedObject.ScaleUp();
        }

        /// <summary>
        ///     Starts scaling the selected object donw.
        /// </summary>
        public void ScaleSelectedObjectDown()
        {
            if (selectedObject)
                selectedObject.ScaleDown();
        }

        /// <summary>
        ///     Stops scaling the selected object.
        /// </summary>
        public void StopScalingSelectedObject()
        {
            if (selectedObject)
                selectedObject.StopScaling();
        }

        public void SetMovingAllowed(bool inMovingAllowed)
        {
            movingAllowed = inMovingAllowed;
            if (selectedObject)
                selectedObject.SetAxesVisibility(inMovingAllowed);
            Debug.Log("Moving allowed: " + movingAllowed);
        }

        public void SetRotationAllowed(bool inRotationAllowed)
        {
            rotationAllowed = inRotationAllowed;
            Debug.Log("Rotation allowed: " + rotationAllowed);
        }

        public void SetRotationXAxis(Toggle toggle)
        {
            rotationXAxis = toggle.isOn;
            Debug.Log("Rotation on x-axis allowed: " + rotationXAxis);
        }

        public void SetRotationYAxis(Toggle toggle)
        {
            rotationYAxis = toggle.isOn;
            Debug.Log("Rotation on y-axis allowed: " + rotationYAxis);
        }

        public void SetRotationZAxis(Toggle toggle)
        {
            rotationZAxis = toggle.isOn;
            Debug.Log("Rotation on z-axis allowed: " + rotationZAxis);
        }

        public void SetScalingAllowed(bool inScalingAllowed)
        {
            scalingAllowed = inScalingAllowed;
            Debug.Log("Scaling allowed: " + scalingAllowed);
        }

    }
}

