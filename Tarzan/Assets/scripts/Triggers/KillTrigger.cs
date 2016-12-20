using UnityEngine;


public class KillTrigger : MonoBehaviour 
{

    bool pickedUp = false;


    void OnTriggerEnter2D(Collider2D collider)
    {
        if (pickedUp)
            return;

        pickedUp = true;

        GameObject player = App.instance.GetPlayer();
        player.GetComponent<HealthController>().TakeDamage(1000);
    }

}