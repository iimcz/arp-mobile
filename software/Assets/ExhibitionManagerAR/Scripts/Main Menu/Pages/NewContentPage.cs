/*
    Author: Dominik Truong
    Year: 2022
*/

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;

namespace ExhibitionManagerAR
{
    public class NewContentPage : Page
    {
        [SerializeField]
        private RawImage qrCode;
        [SerializeField]
        private TMP_InputField contentNameField;

        public override void ResetPage()
        {
            contentNameField.text = "";
            if (DataManager.Instance.usingNreal)
            {
                contentNameField.interactable = false;
                GenerateRandomName();
            }
        }

        /// <summary>
        ///     Generate a random name from of the length of five. 
        /// </summary>
        public void GenerateRandomName()
        {
            const string glyphs = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            int charAmount = UnityEngine.Random.Range(5, 5);
            string contentName = "";

            for (int i = 0; i < charAmount; i++)
            {
                contentName += glyphs[UnityEngine.Random.Range(0, glyphs.Length)];
            }

            contentNameField.text = contentName;
        }

        /// <summary>
        ///     Generates a QR code based on the text in the input field.
        /// </summary>
        public void GenerateQRCode()
        {
            Texture2D tex = new Texture2D(256, 256);
            string textToEncode = "";

            if (!string.IsNullOrEmpty(contentNameField.text))
            {
                string mapId = DataManager.Instance.mapToLoad.id.ToString();
                textToEncode = mapId + "." + contentNameField.text;
                Color32[] pixels = EncodeTextToQRCode(tex.width, tex.height, textToEncode);
                tex.SetPixels32(pixels);
                tex.Apply();
                qrCode.texture = tex;
            } else
            {
                qrCode.texture = null;
            }
        }

        /// <summary>
        ///     Saves a QR code on the device.
        /// </summary>
        public void SaveQRCode()
        {
            string fileName = "";
            if (!string.IsNullOrEmpty(contentNameField.text))
            {
                string mapId = DataManager.Instance.mapToLoad.id.ToString();
                fileName = mapId + "." + contentNameField.text;
            }

            DataManager.Instance.SaveQRCode(qrCode.texture as Texture2D, fileName);
            UIManager.Instance.ShowNotification("QR code has been saved.", "QR kód byl uložen.");
        }

        /// <summary>
        ///     Checks, if the virtual content name is already being used. 
        ///     If not, creates new content with a name given in the input field.
        /// </summary>
        public void CreateContent()
        {
            string contentName = contentNameField.text;
            
            if (string.IsNullOrEmpty(contentName))
            {
                Debug.Log("Content name is empty.");
                UIManager.Instance.ShowNotification("Scene name cannot be empty.", "Název scény nesmí být prázdný.");
                return;
            }

            NetworkManager.Instance.CreateContent(contentName);
        }

        private Color32[] EncodeTextToQRCode(int width, int height, string textToEncode)
        {
            BarcodeWriter writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = width,
                    Height = height
                }
            };

            return writer.Write(textToEncode);
        }

    }
}

