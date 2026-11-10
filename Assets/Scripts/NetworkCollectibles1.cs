using UnityEngine;
using Unity.Netcode;
public class NetworkCollectibles1 : NetworkBehaviour
{
    [SerializeField] private int healAmount = 25;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if(other.TryGetComponent (out PlayerHealth health))
        {
            health.currentHealth.Value += healAmount;
            if (health.currentHealth.Value > 100)
            {
                health.currentHealth.Value = 100;
            }
 
        }
    }
}
