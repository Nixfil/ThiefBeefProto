// FILE: ShootVisualsManager.cs (Modified - Projectile Children Circles)
using UnityEngine;

// Define a struct to return information about the redirection trajectory's visual outcome
public struct TrajectoryData
{
    public Vector3 StartPosition;
    public Vector3 EndPosition;
    public bool WasInterrupted;
    public Vector3 InterruptPoint;
    public bool InterruptedByTable;
    public RaycastHit LastHit;
}

public struct ShotData
{
    public Vector3 StartPosition;
    public Vector3 EndPosition;
    public bool WasInterrupted;
    public RaycastHit LastHit;
}

/// <summary>
/// Manages the visual feedback for the projectile redirection mechanic.
/// Draws the redirection trajectory, ghost indicator, interrupt marker, and range circles.
/// </summary>
public class ShootVisualsManager : MonoBehaviour
{
    [Header("Controller")]
    public ShootController Controller;

    [Header("Visual References")]
    public LineRenderer redirectionLineRenderer; // Dedicated LineRenderer for the redirection trajectory
    public LineRenderer playerToProjectileLineRenderer; // Line from player to the frozen projectile
    public GameObject ghostIndicatorPrefab; // Prefab for the ghost indicator at the redirection target
    public GameObject interruptMarkerPrefab; // Prefab for the X marker at interruption points
    public GameObject AimLaserImpactPointPrefab;
    public Transform GunPoint;

    // REMOVED: Direct references to RangeCircleRenderer as they will now be children of the projectile
    // public RangeCircleRenderer MinRedirectionRangeCircle; 
    // public RangeCircleRenderer MaxRedirectionRangeCircle; 

    [Header("Visual Materials & Colors")]
    public Material CorrectRedirectionMaterial;
    public Material WrongRedirectionMaterial;
    public Color CorrectRedirectionColor;
    public Color WrongRedirectionColor;
    // RangeCircleMaterial and RangeCircleColor are now managed by RangeCircleRenderer itself

    [Tooltip("Name of the child GameObject on the Projectile prefab containing the Min RangeCircleRenderer.")]
    public string minRedirectionCircleName = "MinRedirectionRangeCircleVisual"; // Name to find child by
    [Tooltip("Name of the child GameObject on the Projectile prefab containing the Max RangeCircleRenderer.")]
    public string maxRedirectionCircleName = "MaxRedirectionRangeCircleVisual"; // Name to find child by

    // Internal instances of prefabs
    private GameObject _ghostInstance;
    private GameObject _interruptMarkerInstance;
    private GameObject _aimLaserImpactPointInstance;


    // --- Configuration values passed from ShootController ---
    private float _aimOffset;
    private int _trajectorySteps;
    private float _trajectoryStepDeltaTime;
    private LayerMask _interruptThrowMask;
    private LayerMask _triggerInterruptLayerMask;
    private LayerMask _groundMask;

    // Store references to the currently active redirection circles
    private RangeCircleRenderer _activeMinRedirectionCircle;
    private RangeCircleRenderer _activeMaxRedirectionCircle;


    // Call this from ShootController's Start()
    public void Initialize(float aimOffset, int trajectorySteps, float trajectoryStepDeltaTime, LayerMask interruptMask, LayerMask triggerMask, LayerMask groundMask)
    {
        _aimOffset = aimOffset;
        _trajectorySteps = trajectorySteps;
        _trajectoryStepDeltaTime = trajectoryStepDeltaTime;
        _interruptThrowMask = interruptMask;
        _triggerInterruptLayerMask = triggerMask;
        _groundMask = groundMask;

        if (redirectionLineRenderer == null || playerToProjectileLineRenderer == null)
        {
            Debug.LogError("Essential visual references (LineRenderers) are missing in ShootVisualsManager! Please assign them in the Inspector!", this);
            enabled = false;
            return;
        }

        // Initially hide all visuals
        HideAllVisuals();
    }

    /// <summary>
    /// Updates and displays all aiming visuals for the redirection.
    /// Returns detailed information about the trajectory's visual outcome.
    /// </summary>
    /// <param name="redirectionSpawnPoint">The position of the frozen projectile (start of the redirection arc).</param>
    /// <param name="redirectionTargetPoint">The computed target point for the redirected arc.</param>
    /// <param name="redirectionVelocity">The calculated initial velocity for the redirected projectile.</param>
    /// <param name="playerOriginPoint">The player's position (for the line to the frozen projectile).</param>
    /// <returns>A ShootData struct containing details about the trajectory's path and interruptions.</returns>
    public TrajectoryData ShowAimingVisuals(Vector3 redirectionSpawnPoint, Vector3 redirectionTargetPoint, Vector3 redirectionVelocity, Vector3 playerOriginPoint)
    {
        // Ensure line renderers are active when visuals are shown
        redirectionLineRenderer.enabled = true;


        TrajectoryData currentRedirection = new TrajectoryData
        {
            StartPosition = redirectionSpawnPoint,
            EndPosition = redirectionSpawnPoint + redirectionVelocity * (_trajectorySteps * _trajectoryStepDeltaTime) + 0.5f * Physics.gravity * (_trajectorySteps * _trajectoryStepDeltaTime) * (_trajectorySteps * _trajectoryStepDeltaTime), // Default end
            WasInterrupted = false,
            InterruptPoint = Vector3.zero,
            InterruptedByTable = false,
            LastHit = default
        };

        // Draw the redirection trajectory line and get interruption data
        TrajectoryCalculator.DrawTrajectory(
            redirectionLineRenderer,
            redirectionSpawnPoint,
            redirectionVelocity,
            _trajectorySteps,
            _trajectoryStepDeltaTime,
            _interruptThrowMask,
            _triggerInterruptLayerMask,
            _groundMask,
            out currentRedirection.WasInterrupted,
            out currentRedirection.InterruptPoint,
            out currentRedirection.InterruptedByTable,
            out currentRedirection.LastHit
        );

        if (currentRedirection.WasInterrupted)
        {
            currentRedirection.EndPosition = currentRedirection.InterruptPoint;
        }
        else
        {
            // If not interrupted, the EndPosition should be the last point drawn by the line renderer
            currentRedirection.EndPosition = redirectionLineRenderer.GetPosition(redirectionLineRenderer.positionCount - 1);
        }

        // Determine validity for visual coloring based on interruption
        bool currentIsRedirectionValid = !currentRedirection.InterruptedByTable; // Invalid if it hit a table

        // If it hit ground but not a table, it's valid
        if (currentRedirection.WasInterrupted && ((1 << currentRedirection.LastHit.collider.gameObject.layer) & _groundMask) != 0 && !currentRedirection.InterruptedByTable)
        {
            currentIsRedirectionValid = true;
        }
        // If it hit something else (like a wall), it's also valid, unless it's a table
        else if (currentRedirection.WasInterrupted && !currentRedirection.InterruptedByTable)
        {
            currentIsRedirectionValid = true;
        }
        // If no interruption, and a target point exists, it's generally valid for visuals
        else if (!currentRedirection.WasInterrupted && redirectionTargetPoint != Vector3.zero) // Check if target is meaningful
        {
            currentIsRedirectionValid = true;
        }
        else // No valid target or other invalid conditions
        {
            currentIsRedirectionValid = false;
        }

        // Update colors for all visuals
        SetAimingColors(currentIsRedirectionValid);


        // --- Ghost Indicator Management ---
        if (redirectionTargetPoint != Vector3.zero && redirectionLineRenderer.enabled)
        {
            if (_ghostInstance == null && ghostIndicatorPrefab != null)
            {
                _ghostInstance = Instantiate(ghostIndicatorPrefab);
            }
            if (_ghostInstance != null)
            {
                _ghostInstance.SetActive(true);
                Vector3 ghostPosition = redirectionTargetPoint;
                ghostPosition.y += _aimOffset; // Use aimOffset for vertical offset
                _ghostInstance.transform.position = ghostPosition; // Ghost is at target point
            }
        }
        else
        {
            if (_ghostInstance != null) _ghostInstance.SetActive(false);
        }

        // --- Interrupt Marker Management ---
        bool showInterruptMarker = false;
        if (currentRedirection.WasInterrupted)
        {
            // If it was interrupted by a table, always show the marker
            if (currentRedirection.InterruptedByTable)
            {
                showInterruptMarker = true;
            }
            // If it was interrupted, AND it was NOT by the ground layer, show the marker (i.e., a wall)
            else if (((1 << currentRedirection.LastHit.collider.gameObject.layer) & _groundMask) == 0)
            {
                showInterruptMarker = true;
            }
        }

        if (showInterruptMarker)
        {
            if (_interruptMarkerInstance == null && interruptMarkerPrefab != null)
            {
                _interruptMarkerInstance = Instantiate(interruptMarkerPrefab);
            }
            if (_interruptMarkerInstance != null)
            {
                _interruptMarkerInstance.SetActive(true);
                var rotator = _interruptMarkerInstance.GetComponent<RotateOnWall>();
                if (rotator != null)
                    rotator.SetPositionAndOrientation(currentRedirection.InterruptPoint, currentRedirection.LastHit.normal);
            }
        }
        else // Hide marker if it should not be active
        {
            if (_interruptMarkerInstance != null) _interruptMarkerInstance.SetActive(false);
        }

        return currentRedirection;
    }

    public ShotData DrawShot()
    {
        playerToProjectileLineRenderer.enabled = true;
        Vector3 targetPos = Controller.currentTargetProjectile.transform.position;
        ShotData currentShotData = new ShotData
        {
            StartPosition = Controller.gunPoint.position,
            EndPosition = targetPos,
            WasInterrupted = false,
            LastHit = default
        };
        

        bool isInterrupted = TrajectoryCalculator.CheckLineInterruption(
            GunPoint.position,
            targetPos,
            GameLayers.InterruptShotMask,
            out RaycastHit hitInfo
        );
        currentShotData.EndPosition = hitInfo.point;
        currentShotData.LastHit = hitInfo;

        currentShotData.WasInterrupted = (GameLayers.TriggerInterruptShotLayerMask & (1 << hitInfo.collider.gameObject.layer)) != 0;


        playerToProjectileLineRenderer.positionCount = 2;
        playerToProjectileLineRenderer.SetPosition(0, currentShotData.StartPosition);
        playerToProjectileLineRenderer.SetPosition(1, currentShotData.EndPosition);
        // Check if interrupted

        if (currentShotData.WasInterrupted)
        {
            if (_interruptMarkerInstance == null && interruptMarkerPrefab != null)
            {
                _interruptMarkerInstance = Instantiate(interruptMarkerPrefab);
            }
            if (_interruptMarkerInstance != null)
            {
                _interruptMarkerInstance.SetActive(true);
                var rotator = _interruptMarkerInstance.GetComponent<RotateOnWall>();
                if (rotator != null)
                    rotator.SetPositionAndOrientation(currentShotData.EndPosition, currentShotData.LastHit.normal);
            }
            SetAimingColors(false);
        }
        else
        {
            if (_interruptMarkerInstance != null && _interruptMarkerInstance.active) { _interruptMarkerInstance.SetActive(false); }
            SetAimingColors(true);
        }
        if(currentShotData.EndPosition != null)
        {
            if (_aimLaserImpactPointInstance == null && AimLaserImpactPointPrefab != null)
            {
                _aimLaserImpactPointInstance = Instantiate(AimLaserImpactPointPrefab);
            }
            if (_aimLaserImpactPointInstance != null)
            {
                _aimLaserImpactPointInstance.SetActive(true);
                var rotator = _aimLaserImpactPointInstance.GetComponent<RotateOnWall>();
                if (rotator != null)
                    rotator.SetPositionAndOrientation(currentShotData.EndPosition, currentShotData.LastHit.normal);
            }
        }

        return currentShotData;
       

    }
    public void ShowRedirectionRangeCircles(Projectile targetProjectile, float minRange, float maxRange, float yLevel)
    {
        if (targetProjectile == null)
        {
            Debug.LogWarning("ShowRedirectionRangeCircles called with null targetProjectile.");
            return;
        }

        // Find the RangeCircleRenderer components as children of the targetProjectile
        Transform minCircleTransform = targetProjectile.transform.Find(minRedirectionCircleName);
        Transform maxCircleTransform = targetProjectile.transform.Find(maxRedirectionCircleName);

        _activeMinRedirectionCircle = (minCircleTransform != null) ? minCircleTransform.GetComponent<RangeCircleRenderer>() : null;
        _activeMaxRedirectionCircle = (maxCircleTransform != null) ? maxCircleTransform.GetComponent<RangeCircleRenderer>() : null;

        if (_activeMinRedirectionCircle == null)
        {
            Debug.LogError($"ShootVisualsManager: Min redirection circle '{minRedirectionCircleName}' not found on projectile '{targetProjectile.name}' or missing RangeCircleRenderer!");
        }
        if (_activeMaxRedirectionCircle == null)
        {
            Debug.LogError($"ShootVisualsManager: Max redirection circle '{maxRedirectionCircleName}' not found on projectile '{targetProjectile.name}' or missing RangeCircleRenderer!");
        }

        // Set radius and position for the circles
        if (_activeMinRedirectionCircle != null)
        {
            _activeMinRedirectionCircle.SetRadius(minRange - 2);
            // Set the local position relative to the projectile, at the desired Y-level
            _activeMinRedirectionCircle.transform.localPosition = new Vector3(0f, yLevel - targetProjectile.transform.position.y, 0f);
            _activeMinRedirectionCircle.ToggleCircle(true);
        }
        if (_activeMaxRedirectionCircle != null)
        {
            _activeMaxRedirectionCircle.SetRadius(maxRange +2);
            // Set the local position relative to the projectile, at the desired Y-level
            _activeMaxRedirectionCircle.transform.localPosition = new Vector3(0f, yLevel - targetProjectile.transform.position.y, 0f);
            _activeMaxRedirectionCircle.ToggleCircle(true);
        }
    }

    /// <summary>
    /// Hides all visual elements related to redirection aiming, including the range circles.
    /// </summary>
    public void HideAllVisuals()
    {
        redirectionLineRenderer.enabled = false;
        playerToProjectileLineRenderer.enabled = false;

        // Hide range circles
        if (_activeMinRedirectionCircle != null) _activeMinRedirectionCircle.ToggleCircle(false);
        if (_activeMaxRedirectionCircle != null) _activeMaxRedirectionCircle.ToggleCircle(false);

        if (_ghostInstance != null) _ghostInstance.SetActive(false);
        if (_interruptMarkerInstance != null) _interruptMarkerInstance.SetActive(false);
        if (_aimLaserImpactPointInstance !=null) _aimLaserImpactPointInstance.SetActive(false);
    }

    /// <summary>
    /// Sets the material and color of all aiming visuals based on redirection validity.
    /// </summary>
    /// <param name="isValid">True for correct (green) materials/colors, false for wrong (red).</param>
    private void SetAimingColors(bool isValid)
    {
        Material currentMaterial = isValid ? CorrectRedirectionMaterial : WrongRedirectionMaterial;
        Color currentColor = isValid ? CorrectRedirectionColor : WrongRedirectionColor;

        if (redirectionLineRenderer != null) redirectionLineRenderer.material = currentMaterial;

        // Update RangeCircleRenderer materials
        if (_activeMinRedirectionCircle != null && _activeMinRedirectionCircle.lineRenderer != null)
            _activeMinRedirectionCircle.lineRenderer.material = currentMaterial;
        if (_activeMaxRedirectionCircle != null && _activeMaxRedirectionCircle.lineRenderer != null)
            _activeMaxRedirectionCircle.lineRenderer.material = currentMaterial;

        if (_ghostInstance != null)
        {
            var sr = _ghostInstance.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = currentColor;
        }

        if (_interruptMarkerInstance != null)
        {
            var rotator = _interruptMarkerInstance.GetComponent<RotateOnWall>();
            if (rotator != null && rotator.Sprite != null)
                rotator.Sprite.color = currentColor;
        }
    }

    void OnDestroy()
    {
        // Clean up instantiated objects if this manager is destroyed
        if (_ghostInstance != null) Destroy(_ghostInstance);
        if (_interruptMarkerInstance != null) Destroy(_interruptMarkerInstance);
    }
}
