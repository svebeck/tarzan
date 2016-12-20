using UnityEngine;


public class KillTrigger : MonoBehaviour 
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        GameObject player = App.instance.GetPlayer();
        player.GetComponent<HealthController>().TakeDamage(1000);
    }

}