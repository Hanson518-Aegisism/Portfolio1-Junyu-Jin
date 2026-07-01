using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MechDamage
{

    public class SparkingCable : MonoBehaviour
    {

        public float minInterval = 1f;
        public float maxInterval = 5f;
        public float minForce = 200f;
        public float maxForce = 2000f;

        public GameObject sparksVFX;

        public ParticleSystem sparkSmoke;
        public ParticleSystem strikeSpark;
        public ParticleSystem fallSparks;
        public ParticleSystem streakSparks;
        public ParticleSystem electricity;

        public GameObject cable;

        public GameObject constructedCable;

        public GameObject cableSingleSection;

        public AudioSource Electriciy_Spark_Audio_A;
        public AudioSource Electriciy_Spark_Audio_B;
        public AudioSource Electriciy_Spark_Audio_C;
        public AudioSource Electriciy_Spark_Audio_D;

        private GameObject objectToFind;
        private Rigidbody rb;
        private string cableName;
        private bool begin = false;
        private GameObject lastChildObject;

        void Start()
        {
            if (cableSingleSection != null)
                cableSingleSection.SetActive(true);
        }

        void Update()
        {

            if (begin == false)
            {
                StartCoroutine("AttachVFXToCable");
            }

        }



        IEnumerator RandomCoroutine()
        {

            if (sparkSmoke != null) sparkSmoke.Play();
            if (strikeSpark != null) strikeSpark.Play();
            if (fallSparks != null) fallSparks.Play();
            if (streakSparks != null) streakSparks.Play();
            if (electricity != null) electricity.Play();


            int sparkAudioRnd = (Random.Range(1, 5));

            if (sparkAudioRnd == 1 && Electriciy_Spark_Audio_A != null)
            {
                Electriciy_Spark_Audio_A.Play();
            }
            else if (sparkAudioRnd == 2 && Electriciy_Spark_Audio_B != null)
            {
                Electriciy_Spark_Audio_B.Play();
            }
            else if (sparkAudioRnd == 3 && Electriciy_Spark_Audio_C != null)
            {
                Electriciy_Spark_Audio_C.Play();
            }
            else if (sparkAudioRnd == 4 && Electriciy_Spark_Audio_D != null)
            {
                Electriciy_Spark_Audio_D.Play();
            }


            if (rb != null)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, Random.Range(0, 360), transform.eulerAngles.z);
                float speed = Random.Range(minForce, maxForce);
                rb.isKinematic = false;
                Vector3 force = transform.forward;
                force = new Vector3(force.x, 1, force.z);
                rb.AddForce(force * speed);
            }

            yield return new WaitForSeconds(0.1f);

        }

        IEnumerator AttachVFXToCable()
        {

            begin = true;

            yield return new WaitForSeconds(1);

            if (cable == null || sparksVFX == null || constructedCable == null)
            {
                Debug.LogWarning("[SparkingCable] Missing references on " + name + "; skipping cable attach.", this);
                yield break;
            }

            cableName = cable.name;
            string nameOfCable = cableName + "/" + "Cable_Joined" + "/" + "1";
            objectToFind = GameObject.Find(nameOfCable);

            if (objectToFind == null)
            {
                Debug.LogWarning("[SparkingCable] Could not find " + nameOfCable + "; skipping attach.", this);
                yield break;
            }

            sparksVFX.transform.parent = objectToFind.transform;
            sparksVFX.transform.localPosition = Vector3.zero;

            if (constructedCable.transform.childCount > 0)
            {
                int lastChildIndex = constructedCable.transform.childCount - 1;
                lastChildObject = constructedCable.transform.GetChild(lastChildIndex).gameObject;
                MeshRenderer mr = lastChildObject.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.enabled = false;
            }

            rb = objectToFind.GetComponent<Rigidbody>();

            if (cableSingleSection != null)
                cableSingleSection.SetActive(false);

            while (true)
            {

                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
                StartCoroutine("RandomCoroutine");

            }

        }


    }


}
