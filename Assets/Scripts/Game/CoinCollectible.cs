using UnityEngine;
using StarterAssets;

public sealed class CoinCollectible : MonoBehaviour
{
    private GameScoreManager scoreManager;
    private int scoreValue = 1;
    private bool collected;

    public void Initialize(GameScoreManager manager, int value)
    {
        scoreManager = manager;
        scoreValue = Mathf.Max(1, value);
    }

    private void Update()
    {
        transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || other.GetComponentInParent<ThirdPersonController>() == null)
        {
            return;
        }

        collected = true;

        if (scoreManager != null)
        {
            scoreManager.AddScore(scoreValue);
        }

        Destroy(gameObject);
    }
}
