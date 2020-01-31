using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public class BR_Anim_Script : WeaponBase
{
    private FirstPersonController player;
    private AudioSource audioSource;
    public GameObject bulletSpawn;
    public GameObject[] bulletHoles;
    public ParticleSystem flash;
    public AudioClip fireSound;
    public AudioClip emptyFireSound;
    public AudioClip selectSound;
    private Animator anim;
    private bool isFiring;
    private bool isInspecting;
    private float barrelLength = -0.2f;
    private Vector3 originalBarrelFlashPos;
    private bool selecting;
    private int ammoCount;
    private Coroutine inspectAnim;

    // Start is called before the first frame update
    public override void OnEnable()
    {
        player = GetComponentInParent<FirstPersonController>();
        bulletSpawn = GameObject.FindWithTag("bulletSpawn");
        audioSource = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();
        originalBarrelFlashPos = flash.transform.localPosition;
        StartCoroutine(PlaySelectAnimation());

        isFiring = false;
        isInspecting = false;
    }

    private void Fire()
    {
        var dir = bulletSpawn.transform.forward;

        RaycastHit hit;
        Physics.Raycast(bulletSpawn.transform.position, dir, out hit, 500);

        if(hit.collider != null)
        {
            switch (hit.collider.tag)
            {
                case "Destroyable":
                    Destroy(hit.collider.gameObject);
                    StartCoroutine(PlayFireSound(hit, false));
                    break;
                case "Pickups":
                    StartCoroutine(PlayFireSound(hit, false));
                    break;
                default:
                    StartCoroutine(PlayFireSound(hit));
                    break;
            }
        }
        else
            StartCoroutine(PlayFireSound(hit, false));
        ammoCount = player.ammoList[slotNumber];
    }

    // Update is called once per frame
    public override void UpdateWeaponState()
    {
        ammoCount = player.ammoList[slotNumber];
        if (!selecting && !isFiring)
        {
            if (Input.GetMouseButton(0) && ammoCount == 0)
            {
                Fire();
            }
            if (Input.GetMouseButton(0) && ammoCount != 0)
            {
                if (inspectAnim != null)
                    StopInspectAnimation();
                anim.speed = 2;
                anim.Play("Fire");
                Fire();
            }

            else if(!isInspecting)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    inspectAnim = StartCoroutine(PlayInspectAnimation());
                }
                else if (Input.GetKey(KeyCode.Space))
                {
                    anim.speed = 1;
                    anim.Play("Jump");
                }
                else if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
                {
                    if (isRunning)
                        anim.speed = 0.5f;
                    else
                        anim.speed = 1;
                    anim.Play("WalkCycle");
                }
                else
                {
                    anim.speed = 1;
                    anim.Play("Idle");
                }
            }
            
        }
        if (Input.GetMouseButtonUp(0))
        {
            flash.gameObject.transform.localPosition = originalBarrelFlashPos;
            barrelLength = -0.2f;
        }
    }

    private void ChangeBarrelFlash()
    {
        var pos = flash.gameObject.transform.localPosition;
        flash.gameObject.transform.localPosition = new Vector3(pos.x, pos.y + barrelLength, pos.z);
        barrelLength = -barrelLength;
    }

    private IEnumerator PlaySelectAnimation()
    {
        selecting = true;
        anim.Play("Select");
        audioSource.PlayOneShot(selectSound);
        yield return new WaitForSeconds(GetAnimationClipLength("BR_Select"));
        selecting = false;
    }

    private void StopInspectAnimation()
    {
        StopCoroutine(inspectAnim);
        isInspecting = false;
        inspectAnim = null;
    }
    private IEnumerator PlayInspectAnimation()
    {
        if (isInspecting)
            yield break;
        isInspecting = true;
        anim.Play("Inspect");
        float waittime = GetAnimationClipLength("BR_Inspect");
        yield return new WaitForSeconds(waittime);
        isInspecting = false;
        inspectAnim = null;
    }

    private float GetAnimationClipLength(string name)
    {
        var animator = GetComponent<Animator>();
        foreach(var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == name)
                return clip.length;
        }

        return 0;
    }

    private IEnumerator PlayFireSound(RaycastHit hit, bool spawnBulletHole = true)
    {
        float delay;
        if (isFiring)
            yield break;
        isFiring = true;
        if (ammoCount > 0)
        {
            delay = 0.1f;
            audioSource.clip = fireSound;
            var bulletHole = bulletHoles[Random.Range(0, bulletHoles.Length)];
            if(spawnBulletHole)
                Instantiate(bulletHole, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
            ChangeBarrelFlash();
            flash.Play();
            ammoCount = player.ammoList[slotNumber]--;
        }
        else
        {
            delay = 1f;
            audioSource.clip = emptyFireSound;
        }
        audioSource.PlayOneShot(audioSource.clip);
        yield return new WaitForSeconds(delay);
        isFiring = false;
    }
}
