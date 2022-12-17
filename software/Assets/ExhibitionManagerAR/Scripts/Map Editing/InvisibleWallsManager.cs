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
    ///     Manages the invisible walls within the virtual content. If the user is behind one of the walls, 
    ///     the content will be disabled.
    /// </summary>
    public class InvisibleWallsManager : MonoBehaviour
    {
        [HideInInspector]
        public List<InvisibleWall> invisibleWalls = new List<InvisibleWall>();

        private float checkPeriod = 1.0f;

        void Start()
        {
            if (StateManager.currState == State.MapViewing)
                StartCoroutine(CheckIfUserIsWithinWalls());
        }

        private IEnumerator CheckIfUserIsWithinWalls()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(checkPeriod);

                bool visible = true;
                foreach (InvisibleWall wall in invisibleWalls)
                {
                    // User is behind one of the walls, disable content
                    if (!wall.IsUserInFront())
                    {
                        visible = false;
                        break;
                    }
                }

                ToggleContentVisibility(visible);
            }
        }

        private void ToggleContentVisibility(bool visible)
        {
            foreach (Transform child in transform)
            {
                InvisibleWall wall = child.GetComponent<InvisibleWall>();
                if (wall) continue;
                child.gameObject.SetActive(visible);
            }
        } 
    }
}

