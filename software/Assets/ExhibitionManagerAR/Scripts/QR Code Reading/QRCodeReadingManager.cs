/*
    Author: Dominik Truong
    Year: 2022
*/

using Immersal.AR.Nreal;
using Immersal.REST;
using NRKernal;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Handles QR code reading.
    /// </summary>
    public class QRCodeReadingManager : MonoBehaviour
    {
        public static QRCodeReadingManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("No QRCodeReadingManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }
        private static QRCodeReadingManager instance = null;

        [SerializeField]
        private QRCodeReadingUIManager qrCodeReadingUIManager;
        [SerializeField]
        private ARCameraManager cameraManager;
        [SerializeField]
        private TMP_Text qrCode;

        private BarcodeReader barCodeReader = new BarcodeReader();
        private string qrCodeStr;
        private bool done = true;
        private bool doOnce = false;

        private CameraModelView camTexture;
        private NRRGBCamTextureYUV.YUVTextureFrame frame;

        private SDKJob mapToLoad;
        private string mapToLoadId;
        private string contentToLoad;

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

            NetworkManager.onMapListDownloaded += CheckIfMapExists;
            NetworkManager.onContentListDownloaded += CheckIfContentExists;
            NetworkManager.onIsContentLockedResolved += TryLoadContent;
        }

        void Start()
        {
            if (DataManager.Instance.usingNreal)
            {
                camTexture = NRLocalizer.Instance.CamTexture;
                (camTexture as NRRGBCamTextureYUV).OnUpdate += OnYUVCaptureUpdate;
            }
        }

        public void StartScanning()
        {
            if (StateManager.currState != State.QRCodeReading)
                return;

            // Reset all the values
            ResetAndStopScanning();

            if (DataManager.Instance.currLanguage == Language.English)
            {
                qrCode.text = "Scan a QR code...";
            }
            else if (DataManager.Instance.currLanguage == Language.Czech)
            {
                qrCode.text = "Naskenujte QR kód...";
            }

#if UNITY_EDITOR
            qrCodeStr = "47469.Content";
            ParseQRCode();
#elif PLATFORM_ANDROID
            done = false;
            StartCoroutine(Scan());
#endif
        }

        public void ResetAndStopScanning()
        {
            qrCodeStr = "";
            qrCode.text = "";
            mapToLoadId = "";
            contentToLoad = "";
            done = true;
            doOnce = false;
        }

        private IEnumerator Scan()
        {
            while (!done)
            {
                yield return new WaitForSecondsRealtime(0.5f);

                if (DataManager.Instance.usingNreal)
                {
                    TryReadQRCodeNreal();
                }
                else
                {
                    TryReadQRCode();
                }
            }

            ParseQRCode();
        }

        unsafe void TryReadQRCode()
        {
            XRCpuImage image;
            var cameraSubsystem = cameraManager.subsystem;

            if (cameraSubsystem == null || !cameraSubsystem.TryAcquireLatestCpuImage(out image))
                return;

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.MirrorY
            };

            int size = image.GetConvertedDataSize(conversionParams);
            var buffer = new NativeArray<byte>(size, Allocator.Temp);
            image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);
            image.Dispose();

            Texture2D tex = new Texture2D(
                conversionParams.outputDimensions.x, 
                conversionParams.outputDimensions.y, 
                conversionParams.outputFormat,
                false);

            tex.LoadRawTextureData(buffer);
            tex.Apply();
            
            buffer.Dispose();

            Result result = barCodeReader.Decode(tex.GetPixels32(), tex.width, tex.height);

            if (result != null)
            {
                qrCodeStr = result.Text;
                if (!string.IsNullOrEmpty(qrCodeStr))
                {
                    done = true;
                }
            }
        }

        private void TryReadQRCodeNreal()
        {
            if (frame.textureY == null)
                return;

            Texture2D tex = frame.textureY;
            Result result = barCodeReader.Decode(tex.GetRawTextureData(), tex.width, tex.height, RGBLuminanceSource.BitmapFormat.Unknown);

            if (result != null)
            {
                qrCodeStr = result.Text;
                if (!string.IsNullOrEmpty(qrCodeStr))
                {
                    done = true;
                }
            }
        }

        private void OnYUVCaptureUpdate(NRRGBCamTextureYUV.YUVTextureFrame inFrame)
        {
            if (StateManager.currState != State.QRCodeReading ||
                !DataManager.Instance.usingNreal)
                return;

            frame = inFrame;
        }

        private void ParseQRCode()
        {
            if (string.IsNullOrEmpty(qrCodeStr))
            {
                Debug.LogError("The QR code is null or empty while it shouldn't be.");
                return;
            }

            // The QR code string should be in the form of 'mapId.contentName'
            qrCodeStr = qrCodeStr.Trim();
            string[] qrSplit = qrCodeStr.Split('.');

            // If the length of the split array is not exactly two, it's an invalid QR code
            if (qrSplit.Length != 2)
            {
                Debug.Log("Invalid QR code.");
                UIManager.Instance.ShowNotification("This QR code is not valid, try to load a different one.",
                    "Tento QR kód není validní, načtěte prosím jiný");
                StartScanning();
                return;
            }

            mapToLoadId = qrSplit[0];
            contentToLoad = qrSplit[1];

            Debug.Log("Parsing QR code. Map ID: " + mapToLoadId + ", content name: " + contentToLoad);

            NetworkManager.Instance.GetMaps();
        }

        private void CheckIfMapExists(List<SDKJob> maps)
        {
            if (StateManager.currState != State.QRCodeReading ||
                string.IsNullOrEmpty(mapToLoadId))
                return;

            // Only execute this code once
            if (doOnce)
                return;
            doOnce = true;

            bool mapExists = false;
            foreach (SDKJob map in maps)
            {
                string mapId = map.id.ToString();
                if (mapId.Equals(mapToLoadId))
                {
                    mapToLoad = map;
                    mapExists = true;
                    break;
                }
            }

            if (mapExists)
            {
                Debug.Log("Map " + mapToLoadId + " exists. Checking content name " + contentToLoad);
                DataManager.Instance.mapToLoad = mapToLoad;
                NetworkManager.Instance.GetContent();
            }
            else
            {
                Debug.Log("Map " + mapToLoadId + " does not exist. Scanning again.");
                UIManager.Instance.ShowNotification("This QR code is not valid, try to load a different one.",
                    "Tento QR kód není validní, načtěte prosím jiný");
                StartScanning();
            }
        }

        private void CheckIfContentExists(List<string> content)
        {
            if (StateManager.currState != State.QRCodeReading ||
                string.IsNullOrEmpty(contentToLoad))
                return;

            if (content.Contains(contentToLoad))
            {
                Debug.Log("Content " + contentToLoad + " exists.");

                if (qrCodeReadingUIManager.prevState == State.MainMenu)
                {
                    Debug.Log("Checking if it's locked.");
                    NetworkManager.Instance.IsContentLocked(contentToLoad);
                }
                else if (qrCodeReadingUIManager.prevState == State.MapViewing)
                {
                    DataManager.Instance.contentToLoad = contentToLoad;
                    if (!DataManager.Instance.IsMapAndContentAlreadyLoaded(mapToLoadId, contentToLoad))
                    {
                        StateManager.Instance.SwitchToMapViewingState();
                        NetworkManager.Instance.LoadMap();
                    }
                    else
                    {
                        Debug.Log("Content already loaded");
                        UIManager.Instance.ShowNotification("This scene is already loaded.", "Tato scéna je již načtená.");
                        StartScanning();
                    }
                }
                else if (qrCodeReadingUIManager.prevState == State.ContentCopying)
                {
                    DataManager.Instance.contentToLoad = contentToLoad;
                    StateManager.Instance.SwitchToContentCopyingState(true);
                    NetworkManager.Instance.LoadMap();
                }
            }
            else
            {
                Debug.Log("Content " + contentToLoad + " does not exist. Scanning again.");
                UIManager.Instance.ShowNotification("This QR code is not valid, try to load a different one.",
                    "Tento QR kód není validní, načtěte prosím jiný");
                StartScanning();
            }
        }

        private void TryLoadContent(string contentName, bool locked)
        {
            if (StateManager.currState != State.QRCodeReading)
                return;

            if (locked)
            {
                Debug.Log("Content is already being edited by someone else.");
                UIManager.Instance.ShowNotification("This scene is already being edited by someone else.",
                    "Tato scéna je již upravována jiným uživatelem.");
                StartScanning();
            }
            else
            {
                ResetAndStopScanning();
                DataManager.Instance.contentToLoad = contentName;
                StateManager.Instance.SwitchToMapEditingState();
            }
        }

    }
}

