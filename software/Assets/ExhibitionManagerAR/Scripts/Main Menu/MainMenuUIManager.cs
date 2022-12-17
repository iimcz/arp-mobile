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
    ///     Manages the user interface elements in the main menu.
    /// </summary>
    public class MainMenuUIManager : MonoBehaviour
    { 
        [SerializeField]
        private Page selectModePage;

        private List<Page> pageHistory = new List<Page>();

        void Start()
        {
            pageHistory.Add(selectModePage); // The first page after app launch will always be the select mode page 
        }

        /// <summary>
        ///     Switches to a new page. The new page is added to the history, the current page is disabled.
        ///     All of the UI elements of the new page are reset to their defaults.
        /// </summary>
        /// <param name="newPage"> The new page to switch to. </param>
        public void SwitchPage(Page newPage)
        {
            if (pageHistory.Count == 0)
            {
                Debug.LogError("The page history is empty, but there should always be at least one page (the current one).");
                return;
            }

            int currPageId = pageHistory.Count - 1;
            Page currPage = pageHistory[currPageId];

            currPage.gameObject.SetActive(false);
            newPage.gameObject.SetActive(true);
            newPage.ResetPage();
            pageHistory.Add(newPage);
        }

        /// <summary>
        ///     Switches to the previous page. The current page is disabled and removed from history.
        /// </summary>
        public void SwitchToPreviousPage()
        {
            if (pageHistory.Count == 0)
            {
                Debug.LogWarning("The page history is empty, but there should always be at least one page (the current one). ");
                StateManager.Instance.Quit();
                return;
            }

            int currPageId = pageHistory.Count - 1;
            Page currPage = pageHistory[currPageId];

            if (pageHistory.Count == 1)  // This is the first page in history, going back means quitting the app
            {
                StateManager.Instance.Quit();
            }
            else
            {
                currPage.gameObject.SetActive(false);
                pageHistory.RemoveAt(currPageId--);
                Page newPage = pageHistory[currPageId];
                newPage.gameObject.SetActive(true);
                newPage.ResetPage();
            }
        }

        /// <summary>
        ///     Resets the UI elements of the current page.
        /// </summary>
        public void ResetCurrPage()
        {
            int currPageId = pageHistory.Count - 1;
            Page currPage = pageHistory[currPageId];
            currPage.ResetPage();
        }

        /// <summary>
        ///     Disables the current page. Re-adds the first page to the history, since the history must always have at least one page in it.
        ///     Enables the first page and the disables the owning GameObject.
        /// </summary>
        public void DisableSelf()
        {
            int currPageId = pageHistory.Count - 1;
            Page currPage = pageHistory[currPageId];
            currPage.gameObject.SetActive(false);

            pageHistory.Clear();
            pageHistory.Add(selectModePage);

            selectModePage.gameObject.SetActive(true);
            selectModePage.ResetPage();

            gameObject.SetActive(false);
        }

    }
}
