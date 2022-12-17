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
    ///     Represents an invisible wall. If the user is standing in front of it, the virtual objects 
    ///     in the scene will be visible to him. Otherwise they'll be hidden.
    /// </summary>
    public class InvisibleWall : MonoBehaviour
    {
        [SerializeField]
        private GameObject modelParent;

        private InvisibleWallsManager wallManager;

        void Start()
        {
            wallManager = transform.parent.GetComponent<InvisibleWallsManager>();

            if (!wallManager)
                Debug.LogError("No invisible manager found for this wall.");

            // We don't want the arrow to be visible in the map viewing state,
            // only in the map editing state. Also only add this wall to its
            // manager in the map viewing state.
            if (StateManager.currState == State.MapViewing)
            {
                modelParent.SetActive(false);
                if (wallManager)
                    wallManager.invisibleWalls.Add(this);
            }
        }

        /// <summary>
        ///     Checks if the user is in front of the wall.
        /// </summary>
        /// <returns> True if the user is in front of the wall, false otherwise. </returns>
        public bool IsUserInFront()
        {
            Transform cameraTranform = Camera.main.transform;
            Vector3 heading = cameraTranform.position - transform.position;
            float dot = Vector3.Dot(heading, modelParent.transform.forward);

            return dot > 0.0f;
        }
    }
}

