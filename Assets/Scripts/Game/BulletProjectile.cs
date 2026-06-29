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
            // Ignore collisions with the player themselves
            if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerHealth>() != null)
            {
                transform.position += movement;
                lifeRemaining -= Time.deltaTime;
                return;
            }

            transform.position = hit.point;
            
            // If we hit an enemy, destroy it and give the player score
            EnemyDamage enemy = hit.collider.GetComponentInParent<EnemyDamage>();
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
                
                GameScoreManager scoreManager = FindAnyObjectByType<GameScoreManager>();
                if (scoreManager != null)
                {
                    scoreManager.AddScore(2); // Give 2 points for defeating an enemy
                }
            }

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
