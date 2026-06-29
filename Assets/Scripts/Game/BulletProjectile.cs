using UnityEngine;

public sealed class BulletProjectile : MonoBehaviour
{
    private Vector3 velocity;
    private float lifeRemaining;

    public void Initialize(Vector3 direction, float speed, float lifetime)
    {
        velocity = direction.normalized * speed;
        lifeRemaining = lifetime;
    }

    private void Update()
    {
        Vector3 movement = velocity * Time.deltaTime;

        if (Physics.Raycast(transform.position, movement.normalized, out RaycastHit hit, movement.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
            Destroy(gameObject);
            return;
        }

        transform.position += movement;
        lifeRemaining -= Time.deltaTime;

        if (lifeRemaining <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
