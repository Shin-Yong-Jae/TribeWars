using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 * RTSHelper component created by Oussama Bouanani,  SoumiDelRio
 * This script is part of the RTS Engine
 * */

namespace RTSEngine
{
    public class RTSHelper
    {
        //shuffle an input list in O(n) time
        public static void ShuffleList<T>(List<T> inputList)
        {
            if(inputList.Count > 0) //make sure the list already has elements
            {
                //go through the elements of the list:
                for(int i = 0; i < inputList.Count; i++)
                {
                    int swapID = Random.Range(0, inputList.Count); //pick an element to swap with
                    if(swapID != i) //if this isn't the same element
                    {
                        //swap elements:
                        T tempElement = inputList[swapID];
                        inputList[swapID] = inputList[i];
                        inputList[i] = tempElement;
                    }
                }
            }
        }

        //Swap two items:
        public static void Swap<T>(ref T item1, ref T item2)
        {
            T temp = item1;
            item1 = item2;
            item2 = temp;
        }

        //Create an index list: a list of ints where each element contains its index as the element (usually this is randomized to provide to randomize another list)
        public static List<int> GenerateIndexList (int length)
        {
            List<int> indexList = new List<int>();

            int i = 0;
            while (i < length) 
            {
                indexList.Add(i);
                i++;
            }

            return indexList;
        }

        //Check if a layer is inside a layer mask:
        public static bool IsInLayerMask (LayerMask mask, int layer)
        {
            return ((mask & (1 << layer)) != 0);
        }

        //a method to update the current rotation target
        public static Quaternion GetLookRotation(Transform transform, Vector3 targetPosition, bool reversed = false, bool fixYRotation = true)
        {
            if (reversed)
                targetPosition = transform.position - targetPosition;
            else
                targetPosition -= transform.position;

            if(fixYRotation == true)
                targetPosition.y = 0;
            if (targetPosition != Vector3.zero)
                return Quaternion.LookRotation(targetPosition);
            else
                return transform.rotation;
        }
    }
}

