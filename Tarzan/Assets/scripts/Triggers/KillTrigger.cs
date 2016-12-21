using UnityEngine;


public class KillTrigger : MonoBehaviour 
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        HealthController healthController = collider.gameObject.GetComponent<HealthController>();

        if (healthController == null)
            return;

        healthController.TakeDamage(1000);
    }

}