using UnityEngine;
using System.Collections;


namespace MechDamage
{

    public class Damage_Explosion_Big : MonoBehaviour
    {

        public GameObject explodeFX;
        public GameObject cog;
        public GameObject metalRod;
        public GameObject spring;
        public Transform explodePosition;

        private bool explodeFlag = false;
        private GameObject cloneCog;
        private GameObject cloneMetalRod;
        private GameObject cloneSpring;
        private float cogSizeRandomiser;

        void Start()
        {

            explodeFX.SetActive(false);
            cog.SetActive(false);

        }

        void Update()
        {

            if (Input.GetButtonDown("Fire1"))
            {

                if (explodeFlag == false)
                {
                    StartCoroutine("Strike");
                }

            }

        }

        IEnumerator Strike()
        {

            explodeFlag = true;

            explodeFX.SetActive(true);
            StartCoroutine("Metal");
            yield return new WaitForSeconds(5f);
            explodeFX.SetActive(false);

            explodeFlag = false;

        }

        IEnumerator Metal()
        {

            int metalCount = 1;

            while (metalCount < 10)
            {

                cloneCog = Instantiate(cog, explodePosition.position, explodePosition.rotation) as GameObject;
                cloneCog.SetActive(true);
                metalCount++;

            }

            metalCount = 1;

            while (metalCount < 20)
            {

                cloneMetalRod = Instantiate(metalRod, explodePosition.position, explodePosition.rotation) as GameObject;
                cloneMetalRod.SetActive(true);
                metalCount++;

            }

            metalCount = 1;

            while (metalCount < 4)
            {

                cloneSpring = Instantiate(spring, explodePosition.position, explodePosition.rotation) as GameObject;
                cloneSpring.SetActive(true);
                metalCount++;

            }

            yield return new WaitForSeconds(0.01f);

        }

    }

}