using UnityEngine;

public sealed class PlayerGunController : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 36f;
    [SerializeField] private float bulletLifetime = 2.5f;
    [SerializeField] private float fireCooldown = 0.2f;

    private Transform equippedGun;
    private float nextFireTime;
    private Material bulletMaterial;

    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private float recoilTimer = 0f;
    private float recoilDuration = 0.15f;

    public bool HasGun => equippedGun != null;

    public void SetEquippedGun(Transform gun)
    {
        equippedGun = gun;
        if (gun != null)
        {
            originalLocalPos = gun.localPosition;
            originalLocalRot = gun.localRotation;
        }
    }

    private void Update()
    {
        if (equippedGun == null)
        {
            return;
        }

        if (recoilTimer > 0f)
        {
            recoilTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(recoilTimer / recoilDuration);
            float kick = Mathf.Sin(t * Mathf.PI);

            equippedGun.localPosition = originalLocalPos;
            equippedGun.localRotation = originalLocalRot;
            
            // Kick backwards and pitch up in world space for realistic recoil
            equippedGun.position += transform.forward * (-0.08f * kick) + transform.up * (0.04f * kick);
            equippedGun.Rotate(transform.right, -15f * kick, Space.World);
        }
        else
        {
            equippedGun.localPosition = originalLocalPos;
            equippedGun.localRotation = originalLocalRot;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        bool shootPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
        {
            shootPressed = true;
        }
#else
        if (Input.GetKeyDown(KeyCode.F))
        {
            shootPressed = true;
        }
#endif

        if (shootPressed)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        nextFireTime = Time.time + fireCooldown;
        recoilTimer = recoilDuration;

        // Use the player's facing direction instead of the camera's
        Vector3 direction = transform.forward;
        // Spawn the bullet slightly ahead of the gun in the facing direction
        Vector3 origin = equippedGun.position + direction.normalized * 0.7f + Vector3.up * 0.1f;

        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Bullet";
        bullet.transform.position = origin;
        bullet.transform.localScale = Vector3.one * 0.16f;

        Collider collider = bullet.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        Renderer renderer = bullet.GetComponent<Renderer>();
        renderer.sharedMaterial = GetBulletMaterial();

        bullet.AddComponent<BulletProjectile>().Initialize(direction, bulletSpeed, bulletLifetime);
    }

    private Material GetBulletMaterial()
    {
        if (bulletMaterial != null)
        {
            return bulletMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        bulletMaterial = new Material(shader);
        bulletMaterial.name = "Runtime Bullet Material";
        bulletMaterial.color = new Color(1f, 0.92f, 0.2f);
        return bulletMaterial;
    }
}
