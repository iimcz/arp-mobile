/*
    Author: Dominik Truong
    Year: 2022
*/

using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Immersal;
using Immersal.AR;
using Immersal.REST;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Handles communication with the Firestore and Immersal databases.   
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("No NetworkManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }
        private static NetworkManager instance = null;

        public delegate void OnMapListDownloaded(List<SDKJob> maps);
        public static OnMapListDownloaded onMapListDownloaded;
        public delegate void OnContentListDownloaded(List<string> content);
        public static OnContentListDownloaded onContentListDownloaded;
        public delegate void OnIsContentLockedResolved(string contentName, bool locked);
        public static OnIsContentLockedResolved onIsContentLockedResolved;

        // Firebase, Firestore
        [HideInInspector]
        public FirebaseFirestore db;
        private FirebaseAuth auth;
        private FirebaseUser user;

        // Jobs for Immersal
        private List<JobAsync> jobs = new List<JobAsync>();
        private int jobLock = 0;

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

            // Check that all of the necessary dependencies for Firebase are present on the system
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                DependencyStatus dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    Debug.Log("Setting up Firebase");
                    auth = FirebaseAuth.DefaultInstance;
                }
                else
                {
                    Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
                }
            });
        }

        void Start()
        {
            StartCoroutine(Login());
        }

        void Update()
        {
            if (jobLock == 1)
                return;

            if (jobs.Count > 0)
            {
                jobLock = 1;
                RunJob(jobs[0]);
            }
        }

        /// <summary>
        ///     Downloades the list of maps from server.
        /// </summary>
        public void GetMaps()
        {
            JobListJobsAsync j = new JobListJobsAsync();
            j.token = ImmersalSDK.Instance.developerToken;
            j.OnResult += (SDKJobsResult result) =>
            {
                if (result.count > 0)
                {
                    List<SDKJob> maps = new List<SDKJob>();

                    foreach (SDKJob job in result.jobs)
                    {
                        if (job.type != (int)SDKJobType.Alignment && (job.status == SDKJobState.Sparse || job.status == SDKJobState.Done))
                        {
                            maps.Add(job);
                        }
                    }
                    onMapListDownloaded?.Invoke(maps);
                }
            };

            jobs.Add(j);
        }

        /// <summary>
        ///     Downloads and loads a map;
        /// </summary>
        /// <param name="loadMapOnly"> If true, loads the map without any content. </param>
        public void LoadMap(bool loadMapOnly = false)
        {
            if (loadMapOnly)
            {
                UIManager.Instance.ShowNotification("Loading room.", "Naèítám místnost.");
            }
            else
            {
                UIManager.Instance.ShowNotification("Loading scene.", "Naèítám scénu.");
            }

            string mapId = DataManager.Instance.mapToLoad.id.ToString();
            string contentName = DataManager.Instance.contentToLoad;
            string key = mapId + "." + contentName;

            // Try to find if the map is already in the scene
            // If it is, don't load it again
            ARMap map = DataManager.Instance.TryFindARMap(mapId);
            if (map != null)
            {
                if (loadMapOnly)
                {
                    UIManager.Instance.ShowNotification("The room has been loaded.", "Místnost byla naètena.", 1.5f, true);
                }
                else
                {
                    Debug.Log("Map " + mapId + " already exists in the scene. Will load content into it.");
                    DataManager.Instance.contentToMapDict[key] = map;
                    LoadContent();
                }

                return;
            }
            
            // Otherwise load the map into the scene
            SDKJob job = DataManager.Instance.mapToLoad;
            JobLoadMapBinaryAsync j = new JobLoadMapBinaryAsync();
            j.id = job.id;
            j.OnResult += (SDKMapResult result) =>
            {
                Debug.Log(string.Format("Load map {0} ({1} bytes), content name = {2}", job.id, result.mapData.Length, contentName));

                Color pointCloudColor = ARMap.pointCloudColors[Random.Range(0, ARMap.pointCloudColors.Length)];
                ARMap.RenderMode renderMode = ARMap.RenderMode.EditorAndRuntime;
                ARMap newMap = ARSpace.LoadAndInstantiateARMap(null, result, renderMode, pointCloudColor);

                // Sync the render mode of all maps in the map viewing mode
                if (StateManager.currState == State.MapViewing)
                {
                    newMap.renderMode = MapViewingManager.Instance.mapsVisible ? ARMap.RenderMode.EditorAndRuntime : ARMap.RenderMode.DoNotRender;
                }

                if (loadMapOnly)
                {
                    UIManager.Instance.ShowNotification("The room has been loaded.", "Místnost byla naètena.", 1.5f, true);
                }
                else
                {
                    DataManager.Instance.contentToMapDict[key] = newMap;
                    LoadContent();
                }
            };
            jobs.Add(j);
        }

        /// <summary>
        ///     Creates a new content file in the Firestore database and updates the content list.
        /// </summary>
        /// <param name="contentName"> Name of the virtual content. </param>
        public void CreateContent(string contentName) {
            CollectionReference collectionRef = db.Collection("ContentList");
            int mapId = DataManager.Instance.mapToLoad.id;
            Query query = collectionRef
                .WhereEqualTo("Name", contentName)
                .WhereEqualTo("MapId", mapId);

            query.GetSnapshotAsync().ContinueWithOnMainThread((querySnapshotTask) =>
            {
                bool contentExists = false;

                // Check if content exists
                foreach (DocumentSnapshot snapshot in querySnapshotTask.Result.Documents)
                {
                    Debug.Log(string.Format("Content name {0} already in use.", snapshot.Id));

                    UIManager.Instance.ShowNotification("This name is already being used. Please choose a different one.",
                        "Tento název je již používán. Zvolte prosím jiný.");
                    contentExists = true;
                    break;
                }

                // If not, create a content file and update the list of contents
                if (!contentExists)
                {
                    string docName = mapId.ToString() + "." + contentName;

                    // Update the list of contents
                    ContentListMetadata contentListMetadata = new ContentListMetadata { Name = contentName, MapId = mapId, Locked = false };
                    db.Collection("ContentList").Document(docName).SetAsync(contentListMetadata);

                    // Add a content file
                    ContentMetadataFirestore contentMetadata = new ContentMetadataFirestore { JSON = "" };
                    DocumentReference docRef = db.Collection("Content").Document(docName);
                    db.Collection("Content").Document(docName).SetAsync(contentMetadata).ContinueWithOnMainThread(task =>
                    {
                        Debug.Log("Added content " + contentName + " into document: " + docRef.Id);

                        if (StateManager.currState == State.MainMenu)
                        {
                            DataManager.Instance.contentToLoad = contentName;
                            UIManager.Instance.SwitchMainMenuToContentCreatedPage();
                        }
                        else if (StateManager.currState == State.ContentCopying)
                        {
                            ContentCopyingManager.Instance.SaveContent(contentName);
                            StateManager.Instance.SwitchToMainMenuState();
                            UIManager.Instance.ShowNotification("The scene has been copied successfully.", "Scéna byla úspìšnì zkopírována.");
                        }
                    });
                }
            });
        }

        /// <summary>
        ///     Downloads the list of virtual content from the server.
        /// </summary>
        public void GetContent()
        {
            CollectionReference collRef = db.Collection("ContentList");
            int mapId = DataManager.Instance.mapToLoad.id;
            Query query = collRef.WhereEqualTo("MapId", mapId);

            query.GetSnapshotAsync().ContinueWithOnMainThread((querySnapshotTask) =>
            {
                List<string> content = new List<string>();
                foreach (DocumentSnapshot snapshot in querySnapshotTask.Result.Documents)
                {
                    Dictionary<string, object> documentDict = snapshot.ToDictionary();
                    string contentName = documentDict["Name"].ToString();
                    content.Add(contentName);
                }
                onContentListDownloaded?.Invoke(content);
            });
        }

        /// <summary>
        ///     Locks or unlocks the virtual content to prevent multiple users from editing the same content.
        /// </summary>
        /// <param name="mapId"> ID of the map the content belongs to. </param>
        /// <param name="contentName"> Name of the virtual content to lock. </param>
        public void SetContentLocked(string mapId, string contentName, bool locked)
        {
            string docName = mapId + "." + contentName;
            DocumentReference docRef = db.Collection("ContentList").Document(docName);
            Dictionary<string, object> update = new Dictionary<string, object> { { "Locked", locked } };

            docRef.UpdateAsync(update).ContinueWithOnMainThread(task => {
                Debug.Log(string.Format(" {0} content {1} in map {2}.", locked ? "Locking" : "Unlocking", contentName, mapId));
            });
        }

        /// <summary>
        ///     Checks if virtual content is being edited by someone else and therefore is locked.
        ///     Calls an event when the request is processed.
        /// </summary>
        /// <param name="contentName"> Name of the virtual content. </param>
        public void IsContentLocked(string contentName)
        {
            string mapId = DataManager.Instance.mapToLoad.id.ToString();
            string docName = mapId + "." + contentName;

            db.Collection("ContentList").Document(docName).GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                bool locked = false;
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Dictionary<string, object> jsonDict = snapshot.ToDictionary();
                    locked = (bool) jsonDict["Locked"];
                    Debug.Log("Is map locked: " + locked);
                    onIsContentLockedResolved?.Invoke(contentName, locked);
                }
                else
                {
                    Debug.Log("Document " + snapshot.Id + " does not exist.");
                }
            });
        }

        /// <summary>
        ///     Displays a notification if there's no internet connection.
        /// </summary>
        public void CheckInternetConnection()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogWarning("No internet connection.");
                UIManager.Instance.ShowNotification("No internet connection.", "Pøipojení k internetu není k dispozici.");
            }
        }

        private void LoadContent()
        {
            if (StateManager.currState == State.MainMenu ||
                StateManager.currState == State.MapEditing)
            {
                MapEditingManager.Instance.SetMapAndContent();
                MapEditingManager.Instance.LoadContent();
            }
            else if (StateManager.currState == State.MapViewing)
            {
                MapViewingManager.Instance.SetMapAndContent();
                MapViewingManager.Instance.LoadContent();
                MapViewingManager.Instance.AddRealtimeListenerToContent();
            }
            else if (StateManager.currState == State.ContentCopying)
            {
                ContentCopyingManager.Instance.SetSourceMapAndContent();
                ContentCopyingManager.Instance.SpawnAxesIntoSourceMap();
                ContentCopyingManager.Instance.LoadSourceContent();
            }
        }

        private IEnumerator Login()
        {
            // Wait for Internet connection
            while (Application.internetReachability == NetworkReachability.NotReachable)
            {
                UIManager.Instance.ShowNotification("No internet connection.", "Pøipojení k internetu není k dispozici.", 3.0f);
                yield return new WaitForSecondsRealtime(3.0f);
            }

            // Wait until the user logs in
            while (auth == null)
            {
                yield return new WaitForSecondsRealtime(1.0f);
            }

            var loginTask = auth.SignInWithEmailAndPasswordAsync("admin@testemail.com", "AdminTest1234!");

            yield return new WaitUntil(predicate: () => loginTask.IsCompleted);

            if (loginTask.Exception != null)
            {
                Debug.LogError("Failed to register task with " + loginTask.Exception);
            }
            else
            {
                user = loginTask.Result;
                Debug.Log("User logged in successfully: " + user.Email + ", user ID: " + user.UserId);
                db = FirebaseFirestore.DefaultInstance;
            }

            UIManager.Instance.EnableMainMenu();
        }

        private async void RunJob(JobAsync j)
        {
            await j.RunJobAsync();
            if (jobs.Count > 0)
                jobs.RemoveAt(0);
            jobLock = 0;
        }
    }
}
