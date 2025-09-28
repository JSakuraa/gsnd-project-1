using UnityEngine;

/// <summary>
/// Teleports the player to a designated location when they enter the trigger.
/// Attach this to a GameObject with a Collider set as "Is Trigger".
/// The target location should be an empty GameObject positioned where you want the player to spawn.
/// </summary>
public class RoomTeleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("The GameObject that marks where the player should be teleported to. Usually an empty GameObject positioned at the spawn point.")]
    public Transform teleportDestination;

    [Tooltip("Optional: Play a sound effect when teleporting")]
    public AudioClip teleportSound;

    [Tooltip("Optional: Spawn a particle effect at the destination")]
    public GameObject teleportEffect;

    [Header("Teleport Options")]
    [Tooltip("Should the player's rotation match the destination's rotation?")]
    public bool matchDestinationRotation = false;

    [Tooltip("Delay before teleporting (in seconds)")]
    public float teleportDelay = 0f;

    private AudioSource audioSource;
    private bool isTeleporting = false;
    private bool playerInTrigger = false;

    void Start()
    {
        // Get AudioSource component if it exists
        audioSource = GetComponent<AudioSource>();

        // Ensure this GameObject has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("RoomTeleporter requires a Collider component set as 'Is Trigger'!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("RoomTeleporter collider should be set as 'Is Trigger' for proper functionality.");
        }

        // Check if destination is assigned
        if (teleportDestination == null)
        {
            Debug.LogError("RoomTeleporter: No teleport destination assigned! Please assign a target GameObject.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the object entering is the player
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;

            // Only teleport if not currently teleporting and player just entered
            if (!isTeleporting)
            {
                StartTeleport(other.gameObject);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Reset flags when player leaves the trigger
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            isTeleporting = false;
        }
    }

    void StartTeleport(GameObject player)
    {
        if (teleportDestination == null)
        {
            Debug.LogError("Cannot teleport: No destination assigned!");
            return;
        }

        isTeleporting = true;

        // Play sound effect if available
        if (teleportSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }

        // Teleport immediately or with delay
        if (teleportDelay <= 0f)
        {
            TeleportPlayer(player);
        }
        else
        {
            StartCoroutine(TeleportWithDelay(player));
        }
    }

    System.Collections.IEnumerator TeleportWithDelay(GameObject player)
    {
        yield return new WaitForSeconds(teleportDelay);
        TeleportPlayer(player);
    }

    void TeleportPlayer(GameObject player)
    {
        // Get the CharacterController to properly teleport
        CharacterController playerController = player.GetComponent<CharacterController>();

        if (playerController != null)
        {
            // Disable CharacterController temporarily to avoid conflicts
            playerController.enabled = false;

            // Set player position
            player.transform.position = teleportDestination.position;

            // Optionally match rotation
            if (matchDestinationRotation)
            {
                player.transform.rotation = teleportDestination.rotation;
            }

            // Re-enable CharacterController
            playerController.enabled = true;
        }
        else
        {
            // Fallback for objects without CharacterController
            player.transform.position = teleportDestination.position;

            if (matchDestinationRotation)
            {
                player.transform.rotation = teleportDestination.rotation;
            }
        }

        // Spawn particle effect at destination
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, teleportDestination.position, teleportDestination.rotation);
        }

        // If the destination has a teleporter, mark it as having just teleported someone
        // This prevents immediate re-teleportation at the destination
        RoomTeleporter destinationTeleporter = teleportDestination.GetComponentInParent<RoomTeleporter>();
        if (destinationTeleporter != null && destinationTeleporter != this)
        {
            destinationTeleporter.SetJustTeleportedState();
        }

        // Reset teleporting flag after a short delay to prevent immediate re-triggering
        StartCoroutine(ResetTeleportFlag());

        Debug.Log($"Player teleported to: {teleportDestination.name}");
    }

    /// <summary>
    /// Called by another teleporter to indicate that a player just arrived here
    /// </summary>
    public void SetJustTeleportedState()
    {
        isTeleporting = true;
        playerInTrigger = true;
        StartCoroutine(ResetAfterArrival());
    }

    System.Collections.IEnumerator ResetAfterArrival()
    {
        // Wait a moment for the player to potentially move away
        yield return new WaitForSeconds(1.5f);

        // Check if player is still in the trigger area
        Collider playerCollider = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Collider>();
        if (playerCollider != null)
        {
            Collider thisTrigger = GetComponent<Collider>();
            if (thisTrigger != null && !thisTrigger.bounds.Intersects(playerCollider.bounds))
            {
                // Player has moved away, safe to reset
                playerInTrigger = false;
                isTeleporting = false;
            }
            else
            {
                // Player is still here, check again in a bit
                StartCoroutine(ResetAfterArrival());
            }
        }
        else
        {
            // Fallback reset if we can't find the player
            playerInTrigger = false;
            isTeleporting = false;
        }
    }

    System.Collections.IEnumerator ResetTeleportFlag()
    {
        // Wait a bit longer to ensure player has time to move away from destination teleporter
        yield return new WaitForSeconds(1f);

        // Only reset if player is not currently in this trigger
        if (!playerInTrigger)
        {
            isTeleporting = false;
        }
    }

    // Optional: Visualize the teleporter in the Scene view
    void OnDrawGizmos()
    {
        if (teleportDestination != null)
        {
            // Draw a line from this teleporter to the destination
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, teleportDestination.position);

            // Draw a sphere at the destination
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(teleportDestination.position, 0.5f);

            // Draw an arrow pointing up at the destination
            Gizmos.DrawRay(teleportDestination.position, Vector3.up * 2f);
        }

        // Draw the teleporter area
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.DrawWireCube(capsule.center, new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2));
            }
        }
    }
}