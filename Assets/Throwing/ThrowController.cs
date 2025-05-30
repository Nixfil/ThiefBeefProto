// FILE: ThrowController.cs
using UnityEngine;

public class ThrowController : MonoBehaviour
{
    [Header("References")]
    public ProjectileLauncher launcher;
    public ThrowVisualsManager visualsManager;

    [Header("Throw Settings")]
    public float minThrowRange = 2f;
    public float maxThrowRange = 25f;
    public float AimOffset;
    public float gravity = 9.81f;
    public int trajectorySteps = 30;
    public float trajectoryStepDeltaTime = 0.1f;
    public float minThrowAngle = 25f;
    public float maxThrowAngle = 60f;
    public AnimationCurve angleByDistance;

    private Vector3? cachedTarget = null;
    private Vector3 cachedVelocity;
    private Camera cam;
    private bool isAiming;
    private bool isThrowValid = false;
    private float estimatedDuration; // Still needed for AudioManager

    // New private field to store the ThrowData from the last visual update
    private ThrowData _lastVisualThrowData;


    void Start()
    {
        cam = Camera.main;

        if (visualsManager == null)
        {
            Debug.LogError("ThrowVisualsManager reference is missing in ThrowController. Please assign it in the Inspector!", this);
            enabled = false;
            return;
        }

        visualsManager.Initialize(
            minThrowRange,
            maxThrowRange,
            AimOffset,
            trajectorySteps,
            trajectoryStepDeltaTime,
            GameLayers.InterruptThrowMask,
            GameLayers.TriggerInterruptLayerMask,
            GameLayers.GroundMask
        );

        if (launcher == null)
        {
            Debug.LogError("ProjectileLauncher reference is missing in ThrowController. Please assign it in the Inspector!", this);
            enabled = false;
            return;
        }
        if (GameLayers.ValidThrowMask.value == 0 || GameLayers.InterruptThrowMask.value == 0)
        {
            Debug.LogWarning("GameLayers not fully configured. Please check 'GameSettings' GameObject layers.", this);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isAiming = true;
        }

        if (Input.GetMouseButton(0) && isAiming)
        {
            Vector3? target = GetMouseTargetPoint();
            if (target.HasValue)
            {
                Vector3 velocity;
                if (TrajectoryCalculator.ComputeVelocityArc(launcher.launchPoint.position, target.Value, minThrowAngle, maxThrowAngle, angleByDistance, gravity, out velocity))
                {
                    cachedTarget = target.Value;
                    cachedVelocity = velocity;

                    // Delegate all visual updates to the manager AND get the simplified ThrowData back
                    _lastVisualThrowData = visualsManager.ShowAimingVisuals(launcher.launchPoint.position, cachedTarget, cachedVelocity);
                    isThrowValid = !_lastVisualThrowData.InterruptedByTable; // Validity based on if it hit an invalid surface

                    // Estimated duration calculation remains here as it's throw-logic related
                    estimatedDuration = TrajectoryCalculator.CalculateThrowDuration(
                        launcher.launchPoint.position,
                        cachedVelocity,
                        trajectorySteps,
                        trajectoryStepDeltaTime,
                        GameLayers.InterruptThrowMask
                    );
                }
                else
                {
                    visualsManager.HideAllVisuals();
                    isThrowValid = false;
                }
            }
            else
            {
                visualsManager.HideAllVisuals();
                isThrowValid = false;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isAiming = false;

            if (cachedTarget.HasValue && isThrowValid)
            {
                // Here, you would use the _lastVisualThrowData if you needed to pass it to the projectile or another system.
                // For this simplified ThrowData, the launcher likely doesn't need it yet, but it's available.
                ThrowProjectile(cachedVelocity); // Still passing just velocity to launcher for now

                AudioManager.Instance.PlaySFX(AudioManager.Instance.Throw);
                AudioManager.Instance.PlayBoomerangLoop(estimatedDuration);
            }
            else
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.CancelThrow);
            }

            visualsManager.HideAllVisuals();
            cachedTarget = null;
        }
    }


    Vector3? GetMouseTargetPoint()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, GameLayers.ValidThrowMask))
        {
            Vector3 fromSpawn = hit.point - launcher.launchPoint.position;
            fromSpawn.y = 0f;

            float dist = fromSpawn.magnitude;

            float clampedDist = Mathf.Clamp(dist, minThrowRange, maxThrowRange);

            Vector3 clampedPoint = launcher.launchPoint.position + fromSpawn.normalized * clampedDist;
            clampedPoint.y = hit.point.y;

            return clampedPoint;
        }

        return null;
    }

    // This method now takes just the velocity, as the simplified ThrowData is not directly used for projectile launch
    void ThrowProjectile(Vector3 velocity)
    {
        if (launcher != null)
        {
            launcher.LaunchProjectile(velocity);
        }
        else
        {
            Debug.LogError("ProjectileLauncher reference is missing in ThrowController! Cannot throw projectile.", this);
        }
    }
}