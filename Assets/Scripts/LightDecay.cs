using UnityEngine;

public class LightDecay : MonoBehaviour
{
    [Header("Decay Settings")]
    public float minTime = 1f;
    public float maxTime = 5f;
    
    private Light pointLight;
    private float initialIntensity;
    private float decayDuration;
    private float elapsedTime;
    private bool isDecaying = false;
    
    void Start()
    {
        // Get the Light component
        pointLight = GetComponent<Light>();
        
        if (pointLight == null)
        {
            Debug.LogError("LightDecay script requires a Light component on the same GameObject!");
            return;
        }
        
        // Store the initial intensity
        initialIntensity = pointLight.intensity;
        
        // Start the decay process
        StartDecay();
    }
    
    void Update()
    {
        if (isDecaying && pointLight != null)
        {
            // Update elapsed time
            elapsedTime += Time.deltaTime;
            
            // Calculate decay progress (0 to 1)
            float progress = elapsedTime / decayDuration;
            
            // Clamp progress to ensure it doesn't exceed 1
            progress = Mathf.Clamp01(progress);
            
            // Lerp from initial intensity to 0
            pointLight.intensity = Mathf.Lerp(initialIntensity, 0f, progress);
            
            // Check if decay is complete
            if (progress >= 1f)
            {
                pointLight.intensity = 0f;
                isDecaying = false;
            }
        }
    }
    
    /// <summary>
    /// Starts the decay process with a random duration between minTime and maxTime
    /// </summary>
    public void StartDecay()
    {
        if (pointLight == null) return;
        
        // Reset values
        elapsedTime = 0f;
        decayDuration = Random.Range(minTime, maxTime);
        isDecaying = true;
        
        Debug.Log($"Light decay started. Duration: {decayDuration:F2} seconds");
    }
    
    /// <summary>
    /// Resets the light to its initial intensity and restarts decay
    /// </summary>
    public void ResetAndDecay()
    {
        if (pointLight == null) return;
        
        pointLight.intensity = initialIntensity;
        StartDecay();
    }
    
    /// <summary>
    /// Stops the decay process
    /// </summary>
    public void StopDecay()
    {
        isDecaying = false;
    }
}