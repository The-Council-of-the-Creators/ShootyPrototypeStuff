using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstPerson
{
    public class Pickup : MonoBehaviour
    {
        public GameObject weaponPrefab;
        public PickupType type;
        public int weaponSlotNumber;
        public PickupRotationAxis rotationAxis;
        public int amount;
        public float amplitude;
        public float frequency;
        private Vector3 startPos;
        private Vector3 newPos;

        private void Start()
        {
            startPos = transform.position;
        }

        // Update is called once per frame
        void Update()
        {
            if(rotationAxis == PickupRotationAxis.z)
                transform.Rotate(Vector3.forward, 40.0f * Time.deltaTime);
            else if(rotationAxis == PickupRotationAxis.y)
                transform.Rotate(Vector3.up, 40.0f * Time.deltaTime);
            newPos = startPos;
            newPos.y += Mathf.Sin(Time.fixedTime * Mathf.PI * frequency) * amplitude;

            transform.position = newPos;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Collider>().tag == "Player")
            {
                Destroy(gameObject);
            }
        }
    }
}
