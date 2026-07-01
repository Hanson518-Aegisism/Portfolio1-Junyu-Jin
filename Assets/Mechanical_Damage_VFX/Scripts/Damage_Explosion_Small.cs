using UnityEngine;
using System.Collections;


namespace MechDamage
{

    public class Damage_Explosion_Small : MonoBehaviour
    {

        public GameObject explodeFX;

        private bool strikeFlag = false;

        void Start()
        {

            explodeFX.SetActive(false);

        }

        void Update()
        {

            if (Input.GetButtonDown("Fire1"))
            {

                if (strikeFlag == false)
                {
                    StartCoroutine("Strike");
                }

            }

        }

        IEnumerator Strike()
        {

            strikeFlag = true;
            explodeFX.SetActive(true);

            yield return new WaitForSeconds(2f);

            explodeFX.SetActive(false);
            strikeFlag = false;

        }


    }

}