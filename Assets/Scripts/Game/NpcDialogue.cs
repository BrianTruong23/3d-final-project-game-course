using StarterAssets;
using UnityEngine;

public sealed class NpcDialogue : MonoBehaviour
{
    private const string Prompt = "Press E to talk";

    private GameScoreManager game;
    private bool playerInRange;

    public void Initialize(GameScoreManager manager)
    {
        game = manager;
    }

    private void Start()
    {
        if (game == null)
        {
            game = FindAnyObjectByType<GameScoreManager>();
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && game != null)
        {
            game.ShowDialogue("Guide: Collect 20 coins to win the game!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<ThirdPersonController>() == null)
        {
            return;
        }

        playerInRange = true;
        game.SetPrompt(Prompt);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<ThirdPersonController>() == null)
        {
            return;
        }

        playerInRange = false;
        game.ClearPrompt(Prompt);
    }
}
