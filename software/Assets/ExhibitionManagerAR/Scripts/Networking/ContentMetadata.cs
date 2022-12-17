/*
    Author: Dominik Truong
    Year: 2022
*/

using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionManagerAR
{

    [System.Serializable]
    public struct ContentMetadata
    {
        public List<int> objectTypes;
        public List<Vector3> positions;
        public List<Vector3> rotations;
        public List<Vector3> localRotations;
        public List<Vector3> scales;
    }

    [FirestoreData]
    public class ContentMetadataFirestore
    {
        [FirestoreProperty]
        public string JSON { get; set; }
    }

    [FirestoreData]
    public class ContentListMetadata
    {
        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public int MapId { get; set; }

        [FirestoreProperty]
        public bool Locked { get; set; }
    }

}

