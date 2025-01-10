using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropWeapons : MonoBehaviour
{
    public List<GameObject> Weapons;

    public void DropSwords()
    {
        foreach (GameObject weapons in Weapons)
        {
            weapons.AddComponent<Rigidbody>();
            weapons.AddComponent<BoxCollider>();
            weapons.transform.parent = null;
        }
    }
}
