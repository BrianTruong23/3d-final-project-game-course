using StarterAssets;
using UnityEngine;

public sealed class GunPickup : MonoBehaviour
{
    private GameScoreManager game;
    private GameObject gunPrefab;
    private string weaponName;
    private bool collected;

    public void Initialize(GameScoreManager manager, GameObject prefab, string displayName)
    {
        game = manager;
        gunPrefab = prefab;
        weaponName = displayName;
    }

    private void Update()
    {
        transform.Rotate(0f, 50f * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || other.GetComponentInParent<ThirdPersonController>() == null)
        {
            return;
        }

        collected = true;
        game.EquipGun(gunPrefab, weaponName);
        Destroy(gameObject);
    }
}
