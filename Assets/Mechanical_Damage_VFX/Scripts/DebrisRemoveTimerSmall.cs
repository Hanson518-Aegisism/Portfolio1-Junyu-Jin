using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MechDamage
{

    public class DebrisRemoveTimerSmall : MonoBehaviour
    {
        private float cogSizeRandomiser;

        private float dustDurationTimer = 0f;

        private bool removingDebris = false;

        void Start()
        {

            GetComponent<Rigidbody>().drag = 1;
            GetComponent<Rigidbody>().mass = Random.Range(0.01f, 1f);
            GetComponent<Rigidbody>().useGravity = true;
            GetComponent<Rigidbody>().detectCollisions = true;
            cogSizeRandomiser = Random.Range(0.1f, 0.3f);
            transform.localScale = new Vector3(cogSizeRandomiser, cogSizeRandomiser, cogSizeRandomiser);
            transform.Rotate(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            GetComponent<Rigidbody>().AddForce(Random.Range(-1f, 1f), Random.Range(1f, 3f), Random.Range(-1f, 1f), ForceMode.Impulse);
            GetComponent<Rigidbody>().AddTorque(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f), ForceMode.VelocityChange);



        }


        void Update()
        {

            dustDurationTimer += Time.deltaTime;

            if (dustDurationTimer > 0.5f)
            {

                if (removingDebris == false)
                {

                    StartCoroutine("ScaleToZero");

                }

            }

        }



        private IEnumerator ScaleToZero()
        {
            removingDebris = true;

            float ratio = 0;
            float duration = 0.5f;
            float start_time = Time.time; 
            Vector3 initial_scale_value = transform.localScale;
            do
            {
                yield return new WaitForEndOfFrame(); 
                ratio = (Time.time - start_time) / duration; 
                transform.localScale = Vector3.Lerp(initial_scale_value, Vector3.zero, ratio); 
            } while (ratio < 1);

            Destroy(gameObject);

        }

    }

}
