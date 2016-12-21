using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthPanel : MonoBehaviour {
    public static HealthPanel instance;

    public List<Image> hearts;

    public HealthController healthController;

    float oldHealth = -1f;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

	void Update () 
    {
        if (healthController == null)
        {
            GameObject player = App.instance.GetPlayer();

            if (player != null)
            {
                healthController = player.GetComponent<HealthController>();
            }

            return;
        }


        if (oldHealth == healthController.health)
            return;
        
        for(int i = 0; i < healthController.maxHealth; i++)
        {
            float heartHealth = healthController.health - i;
            heartHealth = Mathf.Clamp01(heartHealth);
            hearts[i].fillAmount = heartHealth;
        }

        oldHealth = healthController.health;

	}
}
