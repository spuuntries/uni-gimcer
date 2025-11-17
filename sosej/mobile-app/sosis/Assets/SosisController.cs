using UnityEngine;
using System.Runtime.InteropServices;

[System.Serializable]
public class Landmark
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class LandmarkList
{
    public Landmark[] landmark;
}

public class SosisController : MonoBehaviour
{
    public float forceAmount = 5f; // seberapa kuat dorongan (for keyboard fallback)
    public float handTrackingMultiplier = 5f; // multiplier for hand tracking force
    public bool useHandTracking = true;
    public float handDataTimeout = 0.5f; // seconds before hand data is considered stale
    
    // Position-based hand tracking settings
    public Vector3 movementBoundsMin = new Vector3(-10f, 0f, 0f);
    public Vector3 movementBoundsMax = new Vector3(10f, 5f, 0f);
    public float positionSmoothSpeed = 3f; // how smoothly to move to target position
    public float fixedZPosition = 0f; // fixed Z position (no forward/backward movement)

    private Rigidbody rb;
    private LandmarkList handLandmarks;
    private bool hasHandData = false;
    private float lastHandDataTime = 0f;
    private Vector3 targetPosition;

    [DllImport("__Internal")]
    private static extern void MediaPipe_Init(string gameObjectName, string methodName);

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        targetPosition = transform.position;
        Debug.Log("SosisController: Start called");
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("SosisController: Calling MediaPipe_Init");
        MediaPipe_Init(gameObject.name, "OnLandmarks");
#else
        Debug.Log("SosisController: Not WebGL build, skipping MediaPipe_Init");
#endif
    }

    public void OnLandmarks(string landmarksJson)
    {
        Debug.Log("SosisController: OnLandmarks called with: " + landmarksJson.Substring(0, Mathf.Min(100, landmarksJson.Length)) + "...");
        string json = "{\"landmark\":" + landmarksJson + "}";
        handLandmarks = JsonUtility.FromJson<LandmarkList>(json);
        hasHandData = (handLandmarks != null && handLandmarks.landmark != null && handLandmarks.landmark.Length > 0);
        lastHandDataTime = Time.time;
        Debug.Log("SosisController: Parsed " + (handLandmarks != null && handLandmarks.landmark != null ? handLandmarks.landmark.Length : 0) + " landmarks, hasHandData=" + hasHandData);
    }

    void FixedUpdate()
    {
        // Check if hand data is stale
        if (hasHandData && (Time.time - lastHandDataTime) > handDataTimeout)
        {
            hasHandData = false;
            Debug.Log("SosisController: Hand data timeout, clearing hasHandData");
        }

        if (useHandTracking && hasHandData && handLandmarks != null && handLandmarks.landmark != null && handLandmarks.landmark.Length > 0)
        {
            // Use wrist position (landmark 0)
            var wrist = handLandmarks.landmark[0];
            
            // Map hand position to world position
            // x: 0 (left) to 1 (right) -> map to movementBoundsMin.x to movementBoundsMax.x
            // y: 0 (top) to 1 (bottom) -> map to movementBoundsMin.y to movementBoundsMax.y
            // z: fixed position (no forward/backward movement)
            
            float worldX = Mathf.Lerp(movementBoundsMin.x, movementBoundsMax.x, wrist.x);
            float worldY = Mathf.Lerp(movementBoundsMin.y, movementBoundsMax.y, 1f - wrist.y); // Invert Y because screen coords are top-down
            
            targetPosition = new Vector3(worldX, worldY, fixedZPosition);
            
            // Smoothly move to target position
            Vector3 newPosition = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * positionSmoothSpeed);
            rb.MovePosition(newPosition);
            
            Debug.Log("SosisController: Setting position - wrist(" + wrist.x + ", " + wrist.y + ") -> position(" + newPosition.x + ", " + newPosition.y + ", " + newPosition.z + ")");
        }
        else
        {
            // Fallback to keyboard controls if no hand tracking data
            // Only left/right and up/down movement, no forward/backward
            if (Input.GetKey(KeyCode.A))
                rb.AddForce(Vector3.left * forceAmount);
            if (Input.GetKey(KeyCode.D))
                rb.AddForce(Vector3.right * forceAmount);
            if (Input.GetKey(KeyCode.Space))
                rb.AddForce(Vector3.up * forceAmount * 2f);
            if (Input.GetKey(KeyCode.LeftShift))
                rb.AddForce(Vector3.down * forceAmount);
        }
    }
}
