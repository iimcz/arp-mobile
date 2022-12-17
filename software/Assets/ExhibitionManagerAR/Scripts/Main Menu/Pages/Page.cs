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
    ///     A base class for a page.
    /// </summary>
    public abstract class Page : MonoBehaviour
    {
        /// <summary>
        ///     Resets all UI elements of the page.
        /// </summary>
        public abstract void ResetPage();
    }
}
