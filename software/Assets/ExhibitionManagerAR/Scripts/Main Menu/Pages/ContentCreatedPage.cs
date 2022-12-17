/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionManagerAR
{
    public class ContentCreatedPage : Page
    {
        [SerializeField]
        private GameObject backButton;

        public override void ResetPage()
        {
            backButton.SetActive(false);
        }

        public void OnOKClicked()
        {
            backButton.SetActive(true);
            StateManager.Instance.SwitchToMainMenuState();
        }
    }
}
