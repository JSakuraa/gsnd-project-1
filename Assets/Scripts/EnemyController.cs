using UnityEngine;

/// <summary>
/// Enemy AI controller that supports three different movement types:
/// - Static: Enemies that don't move
/// - Straight Line: Enemies that move back and forth in a straight line
/// - Patrol Area: Enemies that patrol within a defined collider area
/// </summary>
public class EnemyController : MonoBehaviour
{
    [System.Serializable]
    public enum EnemyType
    {
        Static,
        StraightLine,
        PatrolArea
    }

    [Header("Enemy Settings")]
    [Tooltip("Type of enemy movement behavior")]
    public EnemyType enemyType = EnemyType.Static;

    [Tooltip("Speed at which the enemy moves")]
    public float moveSpeed = 2f;

    [Tooltip("Time to pause at waypoints or when changing direction")]
    public float pauseTime = 1f;

    [Header("Straight Line Movement")]
    [Tooltip("Distance the enemy travels in each direction from starting position")]
    public float lineDistance = 5f;

    [Tooltip("Direction of straight line movement (normalized automatically)")]
    public Vector3 lineDirection = Vector3.forward;

    [Header("Patrol Area Movement")]
    [Tooltip("GameObject with a collider that defines the patrol area. Leave empty to use this GameObject's collider.")]
    public GameObject patrolArea;

    [Tooltip("How close the enemy needs to get to a target point before choosing a new one")]
    public float arrivalDistance = 0.5f;

    [Header("Detection & Combat")]
    [Tooltip("How far the enemy can see the player")]
    public float detectionRange = 3f;

    [Tooltip("What happens when player is detected")]
    public bool chasePlayer = false;

    [Header("Visual Feedback")]
    [Tooltip("Optional: GameObject to show when enemy detects player (like an exclamation mark)")]
    public GameObject detectionIndicator;

    [Header("Game Over Settings")]
    [Tooltip("Should this enemy end the game when touched by player?")]
    public bool causesGameOver = true;

    [Tooltip("Optional: Custom game over message for this enemy")]
    public string gameOverMessage = "Game Over! You were caught by an enemy!";

    [Tooltip("Delay before actually ending the game (for dramatic effect)")]
    public float gameOverDelay = 1f;

    // Private variables
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 lineStartPoint;
    private Vector3 lineEndPoint;
    private bool movingToEnd = true;
    private bool isPaused = false;
    private float pauseTimer = 0f;

    // Patrol area variables
    private Collider patrolCollider;
    private bool hasValidPatrolArea = false;

    // Player detection
    private Transform player;
    private bool playerDetected = false;
    private Vector3 lastKnownPlayerPosition;
    private bool gameOverTriggered = false;

    // Components
    private Rigidbody rb;
    private Animator animator;

    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Store starting position
        startPosition = transform.position;

        // Initialize based on enemy type
        InitializeEnemyBehavior();

        // Hide detection indicator at start
        if (detectionIndicator != null)
        {
            detectionIndicator.SetActive(false);
        }
    }

    // Collision detection for game over
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && causesGameOver && !gameOverTriggered)
        {
            TriggerGameOver();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && causesGameOver && !gameOverTriggered)
        {
            TriggerGameOver();
        }
    }

    void TriggerGameOver()
    {
        gameOverTriggered = true;

        Debug.Log(gameOverMessage);

        // Stop enemy movement
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        // Disable player movement
        TopDownController playerController = player?.GetComponent<TopDownController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Optional: Stop time for dramatic effect
        // Time.timeScale = 0f; // Uncomment this if you want to freeze everything

        // Trigger game over after delay
        StartCoroutine(GameOverSequence());
    }

    System.Collections.IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(gameOverDelay);

        // You can customize this section based on how you want to handle game over
        // Here are some options:

        // Option 1: Show game over UI (you'll need to implement your UI system)
        // GameManager.Instance.ShowGameOverScreen(gameOverMessage);

        // Option 2: Reload the current scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );

        // Option 3: Load a specific game over scene
        // UnityEngine.SceneManagement.SceneManager.LoadScene("GameOverScene");

        // Option 4: Quit application (for standalone builds)
        // Application.Quit();
    }

    void InitializeEnemyBehavior()
    {
        switch (enemyType)
        {
            case EnemyType.Static:
                // Nothing to initialize for static enemies
                break;

            case EnemyType.StraightLine:
                InitializeStraightLineMovement();
                break;

            case EnemyType.PatrolArea:
                InitializePatrolArea();
                break;
        }
    }

    void InitializeStraightLineMovement()
    {
        // Normalize the line direction
        lineDirection = lineDirection.normalized;

        // Calculate start and end points of the line
        lineStartPoint = startPosition - (lineDirection * lineDistance / 2f);
        lineEndPoint = startPosition + (lineDirection * lineDistance / 2f);

        // Set initial target
        targetPosition = lineEndPoint;
        movingToEnd = true;
    }

    void InitializePatrolArea()
    {
        // Get patrol area collider
        if (patrolArea != null)
        {
            patrolCollider = patrolArea.GetComponent<Collider>();
        }
        else
        {
            patrolCollider = GetComponent<Collider>();
        }

        if (patrolCollider != null && patrolCollider.isTrigger)
        {
            hasValidPatrolArea = true;
            ChooseNewPatrolTarget();
        }
        else
        {
            Debug.LogWarning($"Enemy '{name}': No valid patrol area found. Make sure the patrol area has a trigger collider.");
            enemyType = EnemyType.Static; // Fallback to static
        }
    }

    void Update()
    {
        // Check for player detection
        DetectPlayer();

        // Handle pause timer
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
            }
            return;
        }

        // Handle movement based on enemy type
        if (!playerDetected || !chasePlayer)
        {
            HandleMovement();
        }
        else if (chasePlayer && playerDetected)
        {
            ChasePlayer();
        }

        // Update animator if available
        UpdateAnimator();
    }

    void DetectPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            if (!playerDetected)
            {
                // Player just detected
                playerDetected = true;
                lastKnownPlayerPosition = player.position;

                if (detectionIndicator != null)
                {
                    detectionIndicator.SetActive(true);
                }

                Debug.Log($"Enemy '{name}' detected player!");
            }
            else
            {
                // Update last known position
                lastKnownPlayerPosition = player.position;
            }
        }
        else
        {
            if (playerDetected)
            {
                // Player lost
                playerDetected = false;

                if (detectionIndicator != null)
                {
                    detectionIndicator.SetActive(false);
                }
            }
        }
    }

    void HandleMovement()
    {
        switch (enemyType)
        {
            case EnemyType.Static:
                // No movement
                break;

            case EnemyType.StraightLine:
                HandleStraightLineMovement();
                break;

            case EnemyType.PatrolArea:
                HandlePatrolMovement();
                break;
        }
    }

    void HandleStraightLineMovement()
    {
        // Move towards target position
        MoveTowards(targetPosition);

        // Check if reached target
        if (Vector3.Distance(transform.position, targetPosition) <= arrivalDistance)
        {
            // Switch direction
            movingToEnd = !movingToEnd;
            targetPosition = movingToEnd ? lineEndPoint : lineStartPoint;

            // Pause at waypoint
            StartPause();
        }
    }

    void HandlePatrolMovement()
    {
        if (!hasValidPatrolArea) return;

        // Move towards target position
        MoveTowards(targetPosition);

        // Check if reached target
        if (Vector3.Distance(transform.position, targetPosition) <= arrivalDistance)
        {
            ChooseNewPatrolTarget();
            StartPause();
        }
    }

    void ChooseNewPatrolTarget()
    {
        if (patrolCollider == null) return;

        // Get random point within the patrol area bounds
        Bounds bounds = patrolCollider.bounds;
        Vector3 randomPoint;
        int attempts = 0;

        do
        {
            randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                transform.position.y, // Keep same Y level
                Random.Range(bounds.min.z, bounds.max.z)
            );
            attempts++;
        }
        while (!patrolCollider.bounds.Contains(randomPoint) && attempts < 10);

        targetPosition = randomPoint;
    }

    void ChasePlayer()
    {
        if (player != null && playerDetected)
        {
            MoveTowards(player.position);
        }
        else
        {
            // Move to last known position
            MoveTowards(lastKnownPlayerPosition);
        }
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;

        // Move using Rigidbody if available, otherwise use transform
        if (rb != null)
        {
            Vector3 velocity = direction * moveSpeed;
            velocity.y = rb.linearVelocity.y; // Preserve Y velocity for gravity
            rb.linearVelocity = velocity;
        }
        else
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
        }

        // Rotate to face movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }

    void StartPause()
    {
        isPaused = true;
        pauseTimer = pauseTime;

        // Stop movement if using Rigidbody
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        // Calculate movement speed for animation
        Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
        float speed = new Vector3(velocity.x, 0, velocity.z).magnitude;

        animator.SetFloat("Speed", speed);
        animator.SetBool("IsMoving", speed > 0.1f && !isPaused);
        animator.SetBool("PlayerDetected", playerDetected);
    }

    // Visualize enemy detection range and patrol areas in Scene view
    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Movement visualization based on type
        switch (enemyType)
        {
            case EnemyType.StraightLine:
                if (Application.isPlaying)
                {
                    // Draw line path
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(lineStartPoint, lineEndPoint);

                    // Draw current target
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(targetPosition, 0.3f);
                }
                else
                {
                    // Preview line in editor
                    Vector3 normalizedDir = lineDirection.normalized;
                    Vector3 start = transform.position - (normalizedDir * lineDistance / 2f);
                    Vector3 end = transform.position + (normalizedDir * lineDistance / 2f);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(start, end);
                }
                break;

            case EnemyType.PatrolArea:
                // Draw current target
                if (Application.isPlaying && hasValidPatrolArea)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(targetPosition, 0.3f);
                }

                // Draw patrol area bounds
                if (patrolArea != null)
                {
                    Collider areaCollider = patrolArea.GetComponent<Collider>();
                    if (areaCollider != null)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube(areaCollider.bounds.center, areaCollider.bounds.size);
                    }
                }
                break;
        }
    }
}