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
    ///     A simple handler of the throbber (loading circle). 
    /// </summary>
    public class Throbber : MonoBehaviour
    {
        [SerializeField] Transform throbber;

        void FixedUpdate()
        {
            Vector3 rotation = throbber.localRotation.eulerAngles;
            throbber.localRotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z - 270.0f * Time.deltaTime);
        }
    }

}

