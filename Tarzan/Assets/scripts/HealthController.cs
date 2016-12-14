using UnityEngine;
using System.Collections;

public class HealthController : MonoBehaviour {

    public float maxHealth = 3f;
    public float health;

    public float burnTime = 5f;

    public GameObject killEffect;
    public GameObject damageEffect;
    public GameObject drownEffect;
    public GameObject burnEffect;

    bool isBurning = false;
    float burnDamage = 0;

    void Start()
    {
        health = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        health = health < damage ? 0 : health - damage;

        Instantiate(damageEffect, this.transform.position, Quaternion.identity);
    }

    public void TakeDrownDamage(float damage)
    {
        health = health < damage ? 0 : health - damage;

        Instantiate(drownEffect, this.transform.position, Quaternion.identity);
    }

    public void TakeBurnDamage(float damage)
    {
        if (isBurning)
            return;

        burnDamage = damage;

        GameObject fire = (GameObject)Instantiate(burnEffect);
        fire.transform.SetParent(this.gameObject.transform, false);

        isBurning = true;

        StartCoroutine(StartBurning());
    }

    IEnumerator StartBurning()
    {
        for(int i = 0; i < 5; i++)
        {   
            yield return new WaitForSeconds(burnTime/5f);

            health = health < burnDamage/5f ? 0 : health - burnDamage/5f;

        }

        isBurning = false;
    }


    void Update()
    {
        if (health <= 0)
        {
            Instantiate(killEffect, this.transform.position, Quaternion.identity);
            Destroy(this.gameObject);
        }
    }
}
