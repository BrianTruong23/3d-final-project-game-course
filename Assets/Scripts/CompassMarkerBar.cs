using UnityEngine;
using UnityEngine.UI;

public class CompassMarkerBar : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private RectTransform marker;
    [SerializeField] private Text headingText;
    [SerializeField] private bool trackNearestEnemy = true;

    private EnemyDamage nearestEnemy;
    private float nextEnemySearchTime;

    public void Initialize(Transform targetTransform, RectTransform markerTransform, Text headingLabel)
    {
        target = targetTransform;
        marker = markerTransform;
        headingText = headingLabel;
        UpdateHeading();
    }

    private void Update()
    {
        UpdateHeading();
    }

    private void UpdateHeading()
    {
        if (target == null)
        {
            return;
        }

        Transform enemy = trackNearestEnemy ? GetNearestEnemy() : null;
        float heading = enemy != null ? GetRelativeEnemyHeading(enemy) : 0f;

        if (marker != null)
        {
            marker.localRotation = Quaternion.Euler(0f, 0f, -heading);
        }

        if (headingText != null)
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(target.position, enemy.position);
                headingText.text = $"Enemy {Mathf.RoundToInt(distance)}m";
            }
            else
            {
                headingText.text = "Enemy --";
            }
        }
    }

    private Transform GetNearestEnemy()
    {
        if (nearestEnemy != null && Time.time < nextEnemySearchTime)
        {
            return nearestEnemy.transform;
        }

        nextEnemySearchTime = Time.time + 0.25f;
        EnemyDamage[] enemies = FindObjectsByType<EnemyDamage>(FindObjectsSortMode.None);
        float bestDistance = float.PositiveInfinity;
        nearestEnemy = null;

        foreach (EnemyDamage enemy in enemies)
        {
            float distance = (enemy.transform.position - target.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy != null ? nearestEnemy.transform : null;
    }

    private float GetRelativeEnemyHeading(Transform enemy)
    {
        Vector3 direction = enemy.position - target.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return 0f;
        }

        float worldHeading = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        return Mathf.Repeat(worldHeading - target.eulerAngles.y, 360f);
    }
}
