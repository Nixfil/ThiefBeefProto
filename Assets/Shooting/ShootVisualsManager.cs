// FILE: ShootVisualsManager.cs (Modified - Projectile Children Circles)
using Cinemachine;
using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

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
public enum ShotType
{
    Push,
    Pull
}

/// <summary>
/// Manages the visual feedback for the projectile redirection mechanic.
/// Draws the redirection trajectory, ghost indicator, interrupt marker, and range circles.
/// </summary>
public class ShootVisualsManager : MonoBehaviour
{
    [Header("Controller")]
    public PlayerController PController;
    public ShootController Controller;

    [Header("Visual References")]
    public LineRenderer redirectionLineRenderer; // Dedicated LineRenderer for the redirection trajectory
    public LineRenderer playerToProjectileLineRenderer; // Line from player to the frozen projectile
    public GameObject ghostIndicatorPrefab; // Prefab for the ghost indicator at the redirection target
    public GameObject interruptMarkerPrefab; // Prefab for the X marker at interruption points
    public GameObject AimLaserImpactPointPrefab;
    public GameObject RangeCirclePullPrefab;
    public GameObject RangeCirclePushPrefab;
    public VisualEffect VFX_MuzzleFlashPullShot;
    public VisualEffect VFX_MuzzleFlashPushShot;
    public CinemachineImpulseSource ImpulseSource;

    // REMOVED: Direct references to RangeCircleRenderer as they will now be children of the projectile
    // public RangeCircleRenderer MinRedirectionRangeCircle; 
    // public RangeCircleRenderer MaxRedirectionRangeCircle; 

    [Header("Visual Materials & Colors")]
    public Material Mat_PushGlow;
    public Material Mat_PullGlow;
    public Material Mat_Invalid;
    public Color Col_Push;
    public Color Col_Pull;
    public Color Col_Invalid;
    // RangeCircleMaterial and RangeCircleColor are now managed by RangeCircleRenderer itself

    [Tooltip("Name of the child GameObject on the Projectile prefab containing the Min RangeCircleRenderer.")]
    public string minRedirectionCircleName = "MinRedirectionRangeCircleVisual"; // Name to find child by
    [Tooltip("Name of the child GameObject on the Projectile prefab containing the Max RangeCircleRenderer.")]
    public string maxRedirectionCircleName = "MaxRedirectionRangeCircleVisual"; // Name to find child by

    // Internal instances of prefabs
    private GameObject Visuals;
    private GameObject _ghostInstance;
    private GameObject _interruptMarkerInstance;
    private GameObject _aimLaserImpactPointInstance;

    private RangeCircleRenderer _RangeCirclePushInstance;
    private RangeCircleRenderer _RangeCirclePullInstance;


    // --- Configuration values passed from ShootController ---
    private float _aimOffset;
    private int _trajectorySteps;
    private float _trajectoryStepDeltaTime;
    private LayerMask _interruptThrowMask;
    private LayerMask _triggerInterruptLayerMask;
    private LayerMask _groundMask;


    public void Start()
    {
        Visuals = new GameObject("ShootingVisuals");
        var pullCircle = Instantiate(RangeCirclePullPrefab, Visuals.transform);
        _RangeCirclePullInstance=pullCircle.GetComponent<RangeCircleRenderer>();
        _RangeCirclePullInstance.ToggleCircle(false);
        var pushCircle = Instantiate(RangeCirclePushPrefab, Visuals.transform);
        _RangeCirclePushInstance = pushCircle.GetComponent<RangeCircleRenderer>();
        _RangeCirclePushInstance.ToggleCircle(false);
    }

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
    /// 

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
            SetAimingColors(Mat_Invalid, Col_Invalid);
        }

        // Update colors for all visuals


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
            Controller.gunPoint.position,
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
            SetAimingColors(Mat_Invalid, Col_Invalid);
        }
        else
        {
            if (_interruptMarkerInstance != null && _interruptMarkerInstance.active) { _interruptMarkerInstance.SetActive(false); }
            if (Controller.isAimingPullShot) SetAimingColors(Mat_PullGlow, Col_Pull);
            if (Controller.isAimingPushShot) SetAimingColors(Mat_PushGlow, Col_Push);
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
    public void ShowRedirectionRangeCircles(Projectile targetProjectile, float minRange, float maxRange, float yLevel, ShotType shotType)
    {
        if (targetProjectile == null)
        {
            Debug.LogWarning("ShowRedirectionRangeCircles called with null targetProjectile.");
            return;
        }
        Vector3 newPosition = new Vector3(targetProjectile.transform.position.x, yLevel, targetProjectile.transform.position.z);

        switch (shotType)
            {
            case ShotType.Pull:
            {
                    _RangeCirclePullInstance.ToggleCircle(true);
                    _RangeCirclePullInstance.SetRadius(maxRange);
                    _RangeCirclePullInstance.SetCenter(newPosition);
                    break;
            }
            case ShotType.Push:
                {
                    _RangeCirclePushInstance.ToggleCircle(true);
                    _RangeCirclePushInstance.SetRadius(maxRange);
                    _RangeCirclePushInstance.SetCenter(newPosition);
                    break;
                }
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
        if (_RangeCirclePullInstance != null) _RangeCirclePullInstance.ToggleCircle(false);
        if (_RangeCirclePushInstance != null) _RangeCirclePushInstance.ToggleCircle(false);

        if (_ghostInstance != null) _ghostInstance.SetActive(false);
        if (_interruptMarkerInstance != null) _interruptMarkerInstance.SetActive(false);
        if (_aimLaserImpactPointInstance !=null) _aimLaserImpactPointInstance.SetActive(false);
    }

    /// <summary>
    /// Sets the material and color of all aiming visuals based on redirection validity.
    /// </summary>
    /// <param name="isValid">True for correct (green) materials/colors, false for wrong (red).</param>
    public void SetAimingColors(Material Mat, Color Col)
    {
        Debug.Log(Mat + " " + Col);
        if (Mat != null)
        {

            if (redirectionLineRenderer != null) redirectionLineRenderer.material = Mat;


        }
        if (Col != null)
        {
            if (_ghostInstance != null)
            {
                var sr = _ghostInstance.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Col;
            }

            if (_interruptMarkerInstance != null)
            {
                var rotator = _interruptMarkerInstance.GetComponent<RotateOnWall>();
                if (rotator != null && rotator.Sprite != null)
                    rotator.Sprite.color = Col;
            }
        }
    }

    public IEnumerator PlayMuzzleFlash(float delay, VisualEffect MuzzleFlash)
    {
        yield return new WaitForSeconds(delay);
        MuzzleFlash.enabled = true;
        yield return new WaitForSeconds(3f);
        if(MuzzleFlash.aliveParticleCount == 0) MuzzleFlash.enabled = false;
    }

    public void PlayImpactVFXFromBullet(GameObject bullet)
    {
        var Impact = bullet.GetComponent<BulletController>().Impact;
        Impact.gameObject.transform.SetParent(null);
        Impact.enabled = true;
        QoLScript.DestroyGameObjectIntime(Impact.gameObject, 5f);


       /* var VFX = Instantiate(VFX_ImpactPrefab);
        VFX.transform.position = Location;
        var VFX_Graph = VFX.GetComponent<VisualEffect>();
        if (VFX_Graph == null)
        { Debug.Log("VFX not found"); }
        else 
        { 
        Debug.Log("VFX was found");
    }
        VFX_Graph.Play();*/
        //Destroy(VFX);
    }
    
    
    public void ShakeCamera()
    {
        ImpulseSource.GenerateImpulse();
    }
    void OnDestroy()
    {
        // Clean up instantiated objects if this manager is destroyed
        if (_ghostInstance != null) Destroy(_ghostInstance);
        if (_interruptMarkerInstance != null) Destroy(_interruptMarkerInstance);
    }
}
