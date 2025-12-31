using System;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class ItemDropList
{
    public ItemSO itemData;
    
    [Range(0.0f, 100.0f)] public float rate;

    [Range(0, 100)] public int maximumDropCount;
}