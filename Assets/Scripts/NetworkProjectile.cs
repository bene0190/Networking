using UnityEngine;
using Unity.Netcode;
public class NetworkProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifeTime = 3;

    public override void OnNetworkSpawn()
    {
        Invoke(nameof(DestroyProjectile), lifeTime);
    }
    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if(other.TryGetComponent (out PlayerHealth health))
        {
            health.TakeDamage(damage);
        }
    }

    private void DestroyProjectile()
    {
        if(NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}
