using UnityEngine;

public sealed class PlayerGunController : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 36f;
    [SerializeField] private float bulletLifetime = 2.5f;
    [SerializeField] private float fireCooldown = 0.2f;

    private Transform equippedGun;
    private float nextFireTime;
    private Material bulletMaterial;

    public bool HasGun => equippedGun != null;

    public void SetEquippedGun(Transform gun)
    {
        equippedGun = gun;
    }

    private void Update()
    {
        if (equippedGun == null || Time.time < nextFireTime)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        nextFireTime = Time.time + fireCooldown;

        Camera camera = Camera.main;
        Vector3 direction = camera != null ? camera.transform.forward : transform.forward;
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
