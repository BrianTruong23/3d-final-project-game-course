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

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            game.ShowDialogue("Guide: Collect coins, learn each weapon, and practice shooting before exploring deeper into the forest.");
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
