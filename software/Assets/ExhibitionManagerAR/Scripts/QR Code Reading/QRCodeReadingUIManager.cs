/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Handles the UI in the QR code reading state.
    /// </summary>
    public class QRCodeReadingUIManager : MonoBehaviour
    {
        [HideInInspector]
        public State prevState;

        /// <summary>
        ///     Initializes and enables its UI elements.
        /// </summary>
        public void EnableSelf()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        ///     Resets UI elements to their default state and disables the owning GameObject.
        /// </summary>
        public void DisableSelf()
        {
            gameObject.SetActive(false);
        }
    }
}

