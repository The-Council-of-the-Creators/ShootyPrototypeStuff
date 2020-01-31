using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public Sprite ui_image;
    public bool isRunning;
    public int slotNumber;
    public abstract void UpdateWeaponState();

    public abstract void OnEnable();

}
