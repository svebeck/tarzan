using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthPanel : MonoBehaviour {

    public List<Image> hearts;

    public HealthController healthController;

    float oldHealth = -1f;

	void Update () 
    {
        if (oldHealth == healthController.health)
            return;
        
        for(int i = 0; i < healthController.maxHealth; i++)
        {
            float heartHealth = healthController.health - (i);
            heartHealth = Mathf.Clamp01(heartHealth);
            hearts[i].fillAmount = heartHealth;
        }

        oldHealth = healthController.health;

	}
}
