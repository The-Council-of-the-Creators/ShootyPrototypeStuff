using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class BP_Anim_Script : WeaponBase
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
    private bool selecting;
    private int ammoCount;
    private float firerate = 0.2f;
    private bool isCoolingDown;
    private Coroutine cooldown;

    public override void OnEnable()
    {
        player = GetComponentInParent<FirstPersonController>();
        bulletSpawn = GameObject.FindWithTag("bulletSpawn");
        audioSource = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();

        selecting = false;
        StartCoroutine(PlaySelectAnimation());

        isCoolingDown = false;
        isFiring = false;
    }

    private void Fire()
    {
        var dir = bulletSpawn.transform.forward;

        RaycastHit hit;
        Physics.Raycast(bulletSpawn.transform.position, dir, out hit, 500);

        if (hit.collider != null)
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
        cooldown = StartCoroutine(BeginFireCooldown());
    }

    public override void UpdateWeaponState()
    {
        ammoCount = player.ammoList[slotNumber];
        if (!selecting && !isCoolingDown)
        {
            if (Input.GetMouseButtonDown(0) && ammoCount == 0)
            {
                Fire();
            }

            if (Input.GetMouseButtonDown(0) && ammoCount != 0)
            {
                anim.speed = 1;
                anim.Play("Fire");
                Fire();
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
            if (spawnBulletHole)
                Instantiate(bulletHole, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
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

    private IEnumerator BeginFireCooldown()
    {
        if (isCoolingDown)
            yield break;
        isCoolingDown = true;
        yield return new WaitForSeconds(firerate);
        isCoolingDown = false;
        cooldown = null;
    }

    private void StopFireCooldown()
    {
        isCoolingDown = false;
        StopCoroutine(cooldown);
        cooldown = null;
    }

    private float GetAnimationClipLength(string name)
    {
        var animator = GetComponent<Animator>();
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == name)
                return clip.length;
        }

        return 0;
    }

    private IEnumerator PlaySelectAnimation()
    {
        selecting = true;
        anim.Play("Select");
        audioSource.PlayOneShot(selectSound);
        yield return new WaitForSeconds(GetAnimationClipLength("BP_Select"));
        selecting = false;
    }
}
