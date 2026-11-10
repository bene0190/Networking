using UnityEngine;
using Unity.Netcode;
public class PlayerShoot : NetworkBehaviour


{
    [SerializeField] GameObject prejectilePrefab;
    [SerializeField] Transform firePoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetMouseButtonDown(0))
        {
            RequestShootRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestShootRpc()
    {
        GameObject projectile = Instantiate(prejectilePrefab, firePoint.position, firePoint.rotation);
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();
        netObj.Spawn(true);
    }
}
