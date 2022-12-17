/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Handles the control button (movement, rotation, scaling buttons)   
    /// </summary>
    public class ControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public UnityEvent onPointerDown;
        public UnityEvent onPointerUp;

        ///--------------------------------------------------------- Nreal input handling -------------------------------------------------------------------------------///

        public void OnPointerDown(PointerEventData eventData)
        {
            if (onPointerDown != null)
                onPointerDown?.Invoke();

        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (onPointerUp != null)
                onPointerUp?.Invoke();

        }

        ///--------------------------------------------------------- Android input handling -----------------------------------------------------------------------------///

        private void OnMouseDown()
        {
            if (onPointerDown != null)
                onPointerDown?.Invoke();
        }

        private void OnMouseUp()
        {
            if (onPointerUp != null)
                onPointerUp?.Invoke();
        }

    }
}
