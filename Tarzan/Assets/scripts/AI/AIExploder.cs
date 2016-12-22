using UnityEngine;
using System.Collections;

public class AIExploder : AIZombie {

    public float explosionDelay = 2f;

    protected override void TryDoDamage()
    {

        if (target == null)
            return;


        if (attackTime > attackReloadTime)
        {
            bool isZombieEating = false;

            if (diff.sqrMagnitude < attackRange)
                isZombieEating = true;

            if (isZombieEating)
                StartCoroutine(BeginExplosion());

            attackTime = 0;
        }
        attackTime += Time.fixedDeltaTime;
    }

    IEnumerator BeginExplosion()
    {
        float time = 0f;
        Material material = GetComponentInChildren<MeshRenderer>().material;
        bool flip = false;
        for (;;)
        {
            yield return new WaitForEndOfFrame();

            time += Time.deltaTime;

            material.SetColor("_EmissionColor", flip ? Color.white : Color.red);

            if (time > explosionDelay)
            {
                DigController.instance.ExplodeBomb(this.transform.position);
                yield break;
            }

            flip = !flip;
        }
    }
}
