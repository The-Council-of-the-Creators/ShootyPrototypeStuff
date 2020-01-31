using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gate : MonoBehaviour
{
    private Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Open Door");
        if(other.tag == "Player")
        {
            anim.Play("OpenDoor");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Close Door");
        if (other.tag == "Player")
        {
            anim.Play("CloseDoor");
        }
    }
}
