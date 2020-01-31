using FirstPerson;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        public GameObject[] weapons;
        public WeaponBase weapon;
        public GameObject handPos;
        public Text ammoCounter;
        public TextMeshProUGUI fpsCounter;
        public WeaponPopUp weapPopUp;
        [HideInInspector] public int[] ammoList;
        public AudioClip ammoCollect;
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private bool m_IsCrouching;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;

        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private Vector3 spawnPos;
        private Quaternion spawnRot;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;
        private float acceleration;
        private float originalHeight;
        private int selectedWeapon;
        private float movingDir;
        private bool bouncing;
        private float bounceHeight;
        // Use this for initialization
        private void Start()
        {
            spawnPos = transform.position;
            spawnRot = transform.rotation;
            ammoList = new int[3];
            weapons = new GameObject[3];
            if (weapon != null)
            {
                weapons[weapon.slotNumber] = weapon.gameObject;
                selectedWeapon = weapon.slotNumber;
            }
            else
                ammoCounter.enabled = false;
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);
            m_CharacterController.height *= 1.5f;
            originalHeight = m_CharacterController.height;
            ammoCounter.text = ammoList[0] + "";
        }


        // Update is called once per frame
        private void Update()
        {
            // FPS Counter
            var current = 0;
            current = (int)(1f / Time.unscaledDeltaTime);
            fpsCounter.text = current.ToString() + "FPS";

            if (weapon != null)
            {
                weapon.UpdateWeaponState();
                ammoCounter.text = ammoList[selectedWeapon] + "";
            }
            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump && m_CharacterController.isGrounded)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;

            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
            if(m_Input.magnitude == 0)
            {
                m_MoveDir = new Vector3(m_MoveDir.x * 0.9f, m_MoveDir.y, m_MoveDir.z * 0.9f);
                acceleration = Mathf.Clamp01(acceleration - 0.03f);
            }
            else
            {
                m_MoveDir.x = Mathf.Lerp(m_MoveDir.x, desiredMove.x * speed * acceleration, Time.deltaTime * speed * .5f);
                m_MoveDir.z = Mathf.Lerp(m_MoveDir.z, desiredMove.z * speed * acceleration, Time.deltaTime * speed * .5f);
                acceleration = Mathf.Clamp01(acceleration + 0.03f);
            }
            
            if(Input.GetKey(KeyCode.A))
            {
                if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
                {
                    if(movingDir < -0.5f)
                        movingDir = Mathf.Clamp(movingDir + 0.05f, -1, -0.5f);
                    else
                        movingDir = Mathf.Clamp(movingDir - 0.05f, -0.5f, 1);
                }
                else
                    movingDir = Mathf.Clamp(movingDir - 0.05f, -1, 1);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
                {
                    if (movingDir > 0.5f)
                        movingDir = Mathf.Clamp(movingDir - 0.05f, 0.5f, 1);
                    else
                        movingDir = Mathf.Clamp(movingDir + 0.05f, -1, 0.5f);
                }
                else
                    movingDir = Mathf.Clamp(movingDir + 0.05f, -1, 1);
            }
            else
            {
                movingDir *= 0.95f;
            }

            m_Camera.transform.localRotation = new Quaternion(m_Camera.transform.localRotation.x, m_Camera.transform.localRotation.y, movingDir * 0.02f, m_Camera.transform.localRotation.w);

            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.deltaTime;
            }

            if (bouncing)
            {
                m_MoveDir.y = m_JumpSpeed * bounceHeight;
                m_Jump = false;
                m_Jumping = true;
                bouncing = false;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.deltaTime);

            ProgressStepCycle(speed);
            //UpdateCameraPosition(speed);

            m_MouseLook.UpdateCursorLock();
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.tag == "Pickups")
            {
                var pickup = other.GetComponent<Pickup>();
                if (pickup.type == PickupType.Ammo)
                {
                    AddAmmo(pickup.weaponSlotNumber, pickup.amount);
                }
                else if(pickup.type == PickupType.Weapon)
                {
                    if (weapons[pickup.weaponPrefab.GetComponent<WeaponBase>().slotNumber] != null)
                        AddAmmo(pickup.weaponPrefab.GetComponent<WeaponBase>().slotNumber, pickup.amount);
                    else
                    {
                        weapons[pickup.weaponPrefab.GetComponent<WeaponBase>().slotNumber] = Instantiate(pickup.weaponPrefab, handPos.transform.position, handPos.transform.rotation, handPos.transform);
                        SelectWeapon(pickup.weaponPrefab.GetComponent<WeaponBase>().slotNumber);
                    }
                }
            }
            else if(other.tag == "Killbox")
            {
                Debug.Log("Death Rotation: " + transform.rotation);
                Debug.Log("Respawn Rotation: " + spawnRot);
                m_CharacterController.enabled = false;
                transform.position = spawnPos;
                transform.rotation = spawnRot;
                StartCoroutine(RestartController());
            }
        }

        private IEnumerator RestartController()
        {
            yield return new WaitForSeconds(0.1f);
            m_CharacterController.enabled = true;
        }
        public void AddAmmo(int slotNumber, int amount)
        {
            ammoList[slotNumber] += amount;
            m_AudioSource.PlayOneShot(ammoCollect);
        }

        public void SelectWeapon(int slot)
        {
            if (weapons[slot] == null)
                return;
            ammoCounter.enabled = true;
            if(weapons[selectedWeapon] != null)
                weapons[selectedWeapon].SetActive(false);
            selectedWeapon = slot;
            weapon = weapons[selectedWeapon].GetComponent<WeaponBase>();
            weapons[selectedWeapon].SetActive(true);
            weapPopUp.Display(weapons[selectedWeapon].GetComponent<WeaponBase>().ui_image);
        }

        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            if(Input.GetKey(KeyCode.LeftShift))
            {
                m_CharacterController.height = originalHeight / 8;
                m_IsCrouching = true;
            }
            else
            {
                m_CharacterController.height = originalHeight;
                m_IsCrouching = false;
            }
            if(Input.mouseScrollDelta.y < 0)
            {
                int slot = (selectedWeapon - 1 < 0) ? weapons.Length - 1 : selectedWeapon - 1;
                SelectWeapon(slot);
            }
            else if(Input.mouseScrollDelta.y > 0)
            {
                int slot = (selectedWeapon + 1 == weapons.Length) ? 0 : selectedWeapon + 1;
                SelectWeapon(slot);
            }

            if(weapon != null)
                weapon.isRunning = !m_IsWalking;

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider.tag == "BouncyPad")
            {
                bouncing = true;
                bounceHeight = hit.gameObject.GetComponent<BouncyPad>().bounceHeight;
                hit.gameObject.GetComponent<BouncyPad>().PlayBounceSound();
            }

            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
