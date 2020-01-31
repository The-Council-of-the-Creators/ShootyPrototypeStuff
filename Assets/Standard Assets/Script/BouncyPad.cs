using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncyPad : MonoBehaviour
{
    public float bounceHeight;
    public AudioClip fanWhirl;
    public AudioClip bounceSound;

    public void PlayBounceSound()
    {
        GetComponent<AudioSource>().PlayOneShot(bounceSound);
    }
}
