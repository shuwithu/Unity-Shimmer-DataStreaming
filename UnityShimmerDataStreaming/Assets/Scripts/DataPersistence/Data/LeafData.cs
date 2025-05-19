using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LeafData
{
    public string id;
    public bool isActive;

    public LeafData(string id, bool isActive)
    {
        this.id = id;
        this.isActive = isActive;
    }
}
