/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Handles the canvas movement for Nreal glasses.
    /// </summary>
    public class CanvasMovementController : MonoBehaviour
    {
        [SerializeField]
        private Camera cam;
        [SerializeField]
        private float distanceFromCamera = 4.0f;

        private bool pinned = false;

        void Update()
        {
            if (!pinned)
            {
                Vector3 velocity = Vector3.zero;
                Vector3 targetPosition = cam.transform.position + cam.transform.forward * distanceFromCamera;
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, Time.deltaTime * 2);
                transform.LookAt(2 * transform.position - cam.transform.position);
                transform.localPosition += new Vector3(0.0f, -0.0085f, 0.0f);
            }
        }

        public void OnPinnedValueChanged(Toggle toggle)
        {
            pinned = toggle.isOn;

            Debug.Log("Menu pinned: " + pinned);
        }

    }
}

