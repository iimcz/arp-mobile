/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionManagerAR
{
    public class SelectLoadingOptionPage : Page
    {

        public override void ResetPage()
        {
            UIManager.Instance.ShowNotification("This mode allows you to add and edit exhibits in the scene. A scene can only be edited by one user at a time.",
                "Tento režim umožňuje přidávat a upravovat exponáty ve scéně. Scéna může být vždy upravována pouze jedním uživatelem.", 5.0f, true);
        }
    }
}
