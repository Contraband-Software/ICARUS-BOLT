using UnityEngine;
using Cinemachine;

public class FOVController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The Rigidbody whose velocity will be used to adjust the FOV.")]
    public Rigidbody targetRigidbody;

    [Tooltip("The Cinemachine Virtual Camera to control.")]
    public CinemachineFreeLook virtualCamera;

    [Header("FOV Settings")]
    [Tooltip("An Animation Curve that maps speed to FOV.\n" +
             "X-axis: Speed (from 0 to maxSpeed).\n" +
             "Y-axis: The target Field of View value.")]
    public AnimationCurve fovCurve = new AnimationCurve(new Keyframe(0, 40), new Keyframe(10, 60));

    [Tooltip("The speed that corresponds to the end of the Animation Curve's X-axis.")]
    public float maxSpeed = 10f;

    [Tooltip("How quickly the camera's FOV changes. Higher values mean a faster transition.")]
    public float smoothing = 5f;

    private void Start()
    {
        // If no Rigidbody is assigned, log an error and disable the script
        if (targetRigidbody == null)
        {
            Debug.LogError("FOVController requires a 'targetRigidbody' to be assigned.", this);
            this.enabled = false;
            return;
        }

        if (virtualCamera == null)
        {
            Debug.LogError("FOVController requires a 'virtualCamera' to be assigned.", this);
            this.enabled = false;
        }
    }

    private void Update()
    {
        if (targetRigidbody == null || virtualCamera == null) return;

        // Get the current speed of the Rigidbody
        float speed = targetRigidbody.velocity.magnitude;

        // Use the Animation Curve to determine the target FOV based on the speed
        float targetFov = fovCurve.Evaluate(Mathf.Clamp(speed, 0, maxSpeed));

        // Smoothly interpolate the current FOV towards the target FOV
        // For Cinemachine, you modify the Lens settings.
        virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(virtualCamera.m_Lens.FieldOfView, targetFov, Time.deltaTime * smoothing);
    }
}