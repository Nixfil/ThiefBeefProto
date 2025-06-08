// FILE: ShootController.cs (Modified - Event-based Redirection)
using UnityEngine;
using System.Linq; // Required for LINQ operations like OrderBy

/// <summary>
/// Manages the player's projectile redirection mechanic.
/// Handles RMB input, time freezing, detecting thrown projectiles,
/// calculating redirection trajectories, and directly initiating redirection
/// of the frozen projectile.
/// </summary>
public class ShootController : MonoBehaviour
{
    public float Delay;
    [Header("References")]
    public PlayerController PController;
    public ShootVisualsManager VisualsManager; // Reference to the visuals manager for redirection
    public ProjectileLauncher ProjectileLauncher; // Reference to the ProjectileLauncher

    [Header("Shooting")]
    public bool canShoot;
    public GameObject PushShotPrefab;
    public GameObject PullShotPrefab;// Assign in inspector
    private GameObject currentBullet; // Reference to bullet instance
    public Transform gunPoint;
    public LineRenderer trajectoryLineRenderer; // Your existing line renderer
    public GameObject Fist;

    [Header("Redirection Settings")]
    public float redirectionAimOffset = 0.5f; // Small offset for the ghost indicator at the redirection target

    [Tooltip("Minimum horizontal distance for redirection target.")]
    public float minRedirectionRange = 5f; // Minimum range for the redirection throw
    [Tooltip("Maximum horizontal distance for redirection target.")]
    public float maxRedirectionRange = 30f; // Maximum range for the redirection throw

    [Tooltip("Minimum time (seconds) to hold R for a redirection shot.")]
    public float minChargeTime = 0.2f; // Minimum hold time for charge (now for R button)
    [Tooltip("Maximum time (seconds) to hold R for a redirection shot.")]
    public float maxChargeTime = 2.0f; // Maximum hold time for charge (now for R button)

    [Tooltip("Minimum launch angle in degrees for redirection.")]
    public float minRedirectionLaunchAngle = 1f; // Min angle for calculated redirection arc (very low)
    [Tooltip("Maximum launch angle in degrees for redirection.")]
    public float maxRedirectionLaunchAngle = 89f; // Max angle for calculated redirection arc (very high)
    [Tooltip("Animation curve to determine angle based on horizontal distance for redirection.")]
    public AnimationCurve redirectionAngleByDistance; // Curve for calculated redirection arc

    public int trajectorySteps = 30; // Number of points for the redirection trajectory line
    public float trajectoryStepDeltaTime = 0.1f; // Time increment between each step in trajectory calculation

    [Tooltip("The desired Y-coordinate for the redirection target point (e.g., ground level).")]
    public float targetRedirectionY = 0.0f; // Forces the target Y to a specific value (e.g., ground level)

    private Camera cam;
    private bool isAimingPushShot;
    private bool isAimingPullShot;
    public Projectile currentTargetProjectile; // The projectile currently being aimed at at for redirection (exposed for debugging)
    private Vector3 cachedRedirectionVelocity; // Stores the calculated velocity for the redirected projectile
    private Vector3 cachedPushShot; // Stores the calculated target point for the redirected arc
    private Vector3 cachedPullShot;
    private bool isRedirectionValid = false; // Is the current redirection valid?

    private float rButtonChargeStartTime; // Time when R button was pressed down
    private float currentRButtonChargeDuration; // How long R button has been held down

    // New private field to store the ShootData from the last visual update
    private TrajectoryData _lastVisualShootData;
    private ShotData _lastShotData;

    // NEW: Fields to store projectile and velocity for event-based redirection
    private Projectile _projectileToRedirectOnResume;
    private Vector3 _newVelocityOnResume;

    void OnEnable()
    {
        // Subscribe to the OnTimeResumed event when this script is enabled
        GameTimeManager.OnTimeResumed += OnGameTimeResumed;
    }

    void OnDisable()
    {
        // Unsubscribe from the OnTimeResumed event when this script is disabled
        GameTimeManager.OnTimeResumed -= OnGameTimeResumed;
    }

    void Start()
    {
        cam = Camera.main;

        if (VisualsManager == null)
        {
            Debug.LogError("ShootVisualsManager reference is missing in ShootController. Please assign it in the Inspector!", this);
            enabled = false;
            return;
        }
        if (gunPoint.position == null)
        {
            Debug.LogError("Player Shoot Point Transform is missing in ShootController. Please assign it in the Inspector!", this);
            enabled = false;
            return;
        }
        if (ProjectileLauncher == null)
        {
            Debug.LogError("ProjectileLauncher reference is missing in ShootController. Please assign it in the Inspector!", this);
            enabled = false;
            return;
        }

        VisualsManager.Initialize(
            redirectionAimOffset,
            trajectorySteps,
            trajectoryStepDeltaTime,
            GameLayers.InterruptThrowMask,
            GameLayers.TriggerInterruptLayerMask,
            GameLayers.GroundMask
        );

        if (GameLayers.InterruptThrowMask.value == 0 || GameLayers.TriggerInterruptLayerMask.value == 0)
        {
            Debug.LogWarning("GameLayers not fully configured for redirection. Please check 'GameSettings' GameObject layers.", this);
        }
        Debug.Log("ShootController initialized.");
    }

    void Update()
    {
        // Start aiming for redirection (RMB Down)
        if (Input.GetMouseButtonDown(1)) // Right Mouse Button
        {
            // Find the closest active projectile in the air immediately
            currentTargetProjectile = FindClosestActiveProjectile();
            if (currentTargetProjectile != null)
            {
                isAimingPushShot = true;
                GameTimeManager.Instance.FreezeTime(); // Freeze time by pausing rigidbodies
                Debug.Log("RMB Down: Attempting to freeze rigidbodies and find projectile. Redirection aiming active.");

                // Initialize charge duration to min when RMB is first pressed
                currentRButtonChargeDuration = minChargeTime;

            }


            // Show initial range circles and ghost at min range
            
        }

        // While aiming for redirection (RMB Held)
        if (Input.GetMouseButton(1) && isAimingPushShot)
        {
            CheckForBulletInterruption();
            PController.RotatePlayerOverTime(currentTargetProjectile.gameObject, PController.rotationSpeed);
            RotateFist(currentTargetProjectile.gameObject, 360f);

            if (canShoot)
            {
                VisualsManager.ShowRedirectionRangeCircles(currentTargetProjectile, minRedirectionRange, maxRedirectionRange, targetRedirectionY);
                // Handle R button input for charging the redirection distance
                if (Input.GetKeyDown(KeyCode.R))
                {
                    rButtonChargeStartTime = Time.time; // Start charging
                    Debug.Log("R Key Down: Starting charge for redirection distance.");
                }
                if (Input.GetKey(KeyCode.R))
                {
                    currentRButtonChargeDuration = Time.time - rButtonChargeStartTime;
                    currentRButtonChargeDuration = Mathf.Clamp(currentRButtonChargeDuration, minChargeTime, maxChargeTime);
                }
                // If R is not held, currentRButtonChargeDuration remains at its last value or minChargeTime

                // Map charge duration to redirection range
                float chargeProgress = Mathf.InverseLerp(minChargeTime, maxChargeTime, currentRButtonChargeDuration);
                float currentRedirectionDistance = Mathf.Lerp(minRedirectionRange, maxRedirectionRange, chargeProgress);

                Debug.Log($"Charge Duration: {currentRButtonChargeDuration:F2}s, Progress: {chargeProgress:F2}, Redirection Distance: {currentRedirectionDistance:F2}m");

                // If currentTargetProjectile became null (e.g., destroyed during aiming), find it again.      
                if (currentTargetProjectile == null)
                {
                    currentTargetProjectile = FindClosestActiveProjectile();
                    if (currentTargetProjectile != null)
                    {
                        // If we just found a new projectile, show circles on it
                        VisualsManager.ShowRedirectionRangeCircles(currentTargetProjectile, minRedirectionRange, maxRedirectionRange, targetRedirectionY);
                    }
                } // This might happen if the projectile is destroyed by external means.



                if (currentTargetProjectile != null)
                {
                    Rigidbody targetRb = currentTargetProjectile.GetComponent<Rigidbody>();

                    if (targetRb != null)
                    {
                        Debug.Log($"ShootController: Found target projectile {currentTargetProjectile.name}. Its kinematic state is: {targetRb.isKinematic}");
                    } //Check for RigidBody
                    else
                    {
                        Debug.LogWarning($"ShootController: Found target projectile {currentTargetProjectile.name} but it has no Rigidbody!");
                    }

                    // Calculate the horizontal direction away from the player
                    var playerPosition = PController.capsuleCollider.transform.position;
                    Vector3 horizontalDirectionAwayFromPlayer = currentTargetProjectile.transform.position - playerPosition;
                    horizontalDirectionAwayFromPlayer.y = 0; // Zero out the Y component to make it purely horizontal

                    // Handle case where projectile is directly above player (horizontal distance is zero)
                    if (horizontalDirectionAwayFromPlayer.magnitude < 0.001f)
                    {
                        // Default to player's forward direction if no horizontal offset
                        horizontalDirectionAwayFromPlayer = cam.transform.forward;
                        horizontalDirectionAwayFromPlayer.y = 0; // Ensure it's still horizontal
                    }
                    horizontalDirectionAwayFromPlayer.Normalize();

                    // Set the target point: current horizontal position + horizontal distance, and a fixed Y
                    cachedPushShot = currentTargetProjectile.transform.position + horizontalDirectionAwayFromPlayer * currentRedirectionDistance;
                    cachedPushShot.y = targetRedirectionY; // Force the target Y to a specific value

                    Debug.Log($"ShootController: Redirection target point calculated: {cachedPushShot}");

                    // Use ComputeVelocityArc for calculated angle, with a fallback
                    if (TrajectoryCalculator.ComputeVelocityArc(
                        currentTargetProjectile.transform.position, // Start from the projectile's frozen position
                        cachedPushShot,
                        minRedirectionLaunchAngle, // Use min angle for calculated arc
                        maxRedirectionLaunchAngle, // Use max angle for calculated arc
                        redirectionAngleByDistance, // Use curve for calculated arc
                        Physics.gravity.magnitude,
                        out cachedRedirectionVelocity))
                    {
                        _lastVisualShootData = VisualsManager.ShowAimingVisuals(
                            currentTargetProjectile.transform.position,
                            cachedPushShot,
                            cachedRedirectionVelocity,
                            gunPoint.position
                        );
                        isRedirectionValid = !_lastVisualShootData.InterruptedByTable;
                        Debug.Log($"ShootController: Redirection trajectory computed. Is Valid: {isRedirectionValid}");
                    }
                    else
                    {
                        // If ComputeVelocityArc fails with the curve, try a very high angle as a fallback to ensure a solution if possible
                        Debug.LogWarning("ShootController: ComputeVelocityArc failed, attempting high angle fallback.");
                        if (TrajectoryCalculator.ComputeVelocityArc(
                            currentTargetProjectile.transform.position,
                            cachedPushShot,
                            70f, // Fallback min angle (adjusted slightly to 70 for wider range)
                            89f, // Fallback max angle
                            null, // No curve for fallback, just straight lerp
                            Physics.gravity.magnitude,
                            out cachedRedirectionVelocity))
                        {
                            _lastVisualShootData = VisualsManager.ShowAimingVisuals(
                               currentTargetProjectile.transform.position,
                               cachedPushShot,
                               cachedRedirectionVelocity,
                               gunPoint.position
                           );
                            isRedirectionValid = !_lastVisualShootData.InterruptedByTable;
                            Debug.Log($"ShootController: Redirection trajectory computed with high angle fallback. Is Valid: {isRedirectionValid}");
                        }
                        else
                        {
                            VisualsManager.HideAllVisuals(); // Hide all visuals if even fallback fails
                            isRedirectionValid = false;
                            Debug.Log("ShootController: No valid arc found for redirection (even high angle fallback failed).");
                        }
                    }
                }
                else
                {
                    VisualsManager.HideAllVisuals(); // Hide all visuals if no projectile found
                    isRedirectionValid = false;
                    Debug.Log("ShootController: No projectile found to redirect.");
                }
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
                if(canShoot && isRedirectionValid)
                {
                if (VisualsManager.playerToProjectileLineRenderer != null) VisualsManager.playerToProjectileLineRenderer.enabled = false;

                VisualsManager.StartCoroutine(VisualsManager.ShotExplosionVFX(Delay));
                VisualsManager.ShakeCamera();
                float holdRatio = Mathf.Clamp01(currentRButtonChargeDuration / maxChargeTime);
                    float bulletSpeed = Mathf.Lerp(10, 40, holdRatio); // tweak values
                    currentBullet = Instantiate(PushShotPrefab, gunPoint.position, Quaternion.identity);
                    BulletController bulletCtrl = currentBullet.GetComponent<BulletController>();
                    bulletCtrl.controller = this;
                    bulletCtrl.speed = bulletSpeed;
                bulletCtrl.BulletHit += VisualsManager.PlayImpactVFXFromBullet;

                    // Assign the target projectile the bullet should move towards
                    bulletCtrl.targetProjectile = currentTargetProjectile.transform; // assuming currentProjectile is tracked in ShootingController 


            }
            else
                {
                    VisualsManager.HideAllVisuals();
                    GameTimeManager.Instance.ResumeTime();
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.CancelThrow);
                }
            
        }

        // Start aiming for PullShot (LMB Down)
        if (Input.GetMouseButtonDown(0)) // Right Mouse Button
        {
            // Find the closest active projectile in the air immediately
            currentTargetProjectile = FindClosestActiveProjectile();
            if (currentTargetProjectile != null)
            {
                isAimingPullShot = true;
                GameTimeManager.Instance.FreezeTime(); // Freeze time by pausing rigidbodies

                // Initialize charge duration to min when RMB is first pressed
                currentRButtonChargeDuration = minChargeTime;

            }


            // Show initial range circles and ghost at min range

        }

        // While aiming for PullShot (LMB Held)
        if (Input.GetMouseButton(0) && isAimingPullShot)
        {
            CheckForBulletInterruption();
            PController.RotatePlayerOverTime(currentTargetProjectile.gameObject, PController.rotationSpeed);
            RotateFist(currentTargetProjectile.gameObject, 360f);

            if (canShoot)
            {
                VisualsManager.ShowRedirectionRangeCircles(currentTargetProjectile, minRedirectionRange, maxRedirectionRange, targetRedirectionY);
                // Handle R button input for charging the redirection distance
                if (Input.GetKeyDown(KeyCode.R))
                {
                    rButtonChargeStartTime = Time.time; // Start charging
                    Debug.Log("R Key Down: Starting charge for redirection distance.");
                }
                if (Input.GetKey(KeyCode.R))
                {
                    currentRButtonChargeDuration = Time.time - rButtonChargeStartTime;
                    currentRButtonChargeDuration = Mathf.Clamp(currentRButtonChargeDuration, minChargeTime, maxChargeTime);
                }
                // If R is not held, currentRButtonChargeDuration remains at its last value or minChargeTime

                // Map charge duration to redirection range
                float chargeProgress = Mathf.InverseLerp(minChargeTime, maxChargeTime, currentRButtonChargeDuration);
                float currentRedirectionDistance = Mathf.Lerp(minRedirectionRange, maxRedirectionRange, chargeProgress);


                // If currentTargetProjectile became null (e.g., destroyed during aiming), find it again.      
                if (currentTargetProjectile == null)
                {
                    currentTargetProjectile = FindClosestActiveProjectile();
                    if (currentTargetProjectile != null)
                    {
                        // If we just found a new projectile, show circles on it
                        VisualsManager.ShowRedirectionRangeCircles(currentTargetProjectile, minRedirectionRange, maxRedirectionRange, targetRedirectionY);
                    }
                } // This might happen if the projectile is destroyed by external means.



                if (currentTargetProjectile != null)
                {
                    Rigidbody targetRb = currentTargetProjectile.GetComponent<Rigidbody>();

                    if (targetRb != null)
                    {
                        Debug.Log($"ShootController: Found target projectile {currentTargetProjectile.name}. Its kinematic state is: {targetRb.isKinematic}");
                    } //Check for RigidBody
                    else
                    {
                        Debug.LogWarning($"ShootController: Found target projectile {currentTargetProjectile.name} but it has no Rigidbody!");
                    }

                    // Get the horizontal direction from the projectile toward the player
                    var playerPosition = PController.capsuleCollider.transform.position;
                    Vector3 horizontalDirectionToPlayer = gunPoint.position - currentTargetProjectile.transform.position;
                    horizontalDirectionToPlayer.y = 0f; // Flatten to horizontal plane

                    // Handle case where player is directly above or below the projectile
                    if (horizontalDirectionToPlayer.sqrMagnitude < 0.000001f)
                    {
                        // Default to camera's backward direction if no horizontal offset
                        horizontalDirectionToPlayer = cam.transform.forward;
                        horizontalDirectionToPlayer.y = 0f;
                    }

                    horizontalDirectionToPlayer.Normalize();

                    // Compute a point in that direction at a fixed horizontal distance
                    cachedPullShot = currentTargetProjectile.transform.position + horizontalDirectionToPlayer * currentRedirectionDistance;
                    cachedPullShot.y = targetRedirectionY; // Set fixed Y level




                    // Use ComputeVelocityArc for calculated angle, with a fallback
                    if (TrajectoryCalculator.ComputeVelocityArc(
                        currentTargetProjectile.transform.position, // Start from the projectile's frozen position
                        cachedPullShot,
                        minRedirectionLaunchAngle, // Use min angle for calculated arc
                        maxRedirectionLaunchAngle, // Use max angle for calculated arc
                        redirectionAngleByDistance, // Use curve for calculated arc
                        Physics.gravity.magnitude,
                        out cachedRedirectionVelocity))
                    {
                        _lastVisualShootData = VisualsManager.ShowAimingVisuals(
                            currentTargetProjectile.transform.position,
                            cachedPullShot,
                            cachedRedirectionVelocity,
                            gunPoint.position
                        );
                        isRedirectionValid = !_lastVisualShootData.InterruptedByTable;
                        Debug.Log($"ShootController: Redirection trajectory computed. Is Valid: {isRedirectionValid}");
                    }
                    else
                    {
                        // If ComputeVelocityArc fails with the curve, try a very high angle as a fallback to ensure a solution if possible
                        Debug.LogWarning("ShootController: ComputeVelocityArc failed, attempting high angle fallback.");
                        if (TrajectoryCalculator.ComputeVelocityArc(
                            currentTargetProjectile.transform.position,
                            cachedPullShot,
                            70f, // Fallback min angle (adjusted slightly to 70 for wider range)
                            89f, // Fallback max angle
                            null, // No curve for fallback, just straight lerp
                            Physics.gravity.magnitude,
                            out cachedRedirectionVelocity))
                        {
                            _lastVisualShootData = VisualsManager.ShowAimingVisuals(
                               currentTargetProjectile.transform.position,
                               cachedPullShot,
                               cachedRedirectionVelocity,
                               gunPoint.position
                           );
                            isRedirectionValid = !_lastVisualShootData.InterruptedByTable;
                            Debug.Log($"ShootController: Redirection trajectory computed with high angle fallback. Is Valid: {isRedirectionValid}");
                        }
                        else
                        {
                            VisualsManager.HideAllVisuals(); // Hide all visuals if even fallback fails
                            isRedirectionValid = false;
                            Debug.Log("ShootController: No valid arc found for redirection (even high angle fallback failed).");
                        }
                    }
                }
                else
                {
                    VisualsManager.HideAllVisuals(); // Hide all visuals if no projectile found
                    isRedirectionValid = false;
                    Debug.Log("ShootController: No projectile found to redirect.");
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (canShoot && isRedirectionValid)
            {
                if (VisualsManager.playerToProjectileLineRenderer != null) VisualsManager.playerToProjectileLineRenderer.enabled = false;

                VisualsManager.StartCoroutine(VisualsManager.ShotExplosionVFX(Delay));
                VisualsManager.ShakeCamera();
                float holdRatio = Mathf.Clamp01(currentRButtonChargeDuration / maxChargeTime);
                float bulletSpeed = Mathf.Lerp(10, 40, holdRatio); // tweak values
                currentBullet = Instantiate(PullShotPrefab, gunPoint.position, Quaternion.identity);
                BulletController bulletCtrl = currentBullet.GetComponent<BulletController>();
                bulletCtrl.controller = this;
                bulletCtrl.speed = bulletSpeed;
                bulletCtrl.BulletHit += VisualsManager.PlayImpactVFXFromBullet;

                // Assign the target projectile the bullet should move towards
                bulletCtrl.targetProjectile = currentTargetProjectile.transform; // assuming currentProjectile is tracked in ShootingController 


            }
            else
            {
                VisualsManager.HideAllVisuals();
                GameTimeManager.Instance.ResumeTime();
                AudioManager.Instance.PlaySFX(AudioManager.Instance.CancelThrow);
            }

        }
    }

    /// <summary>
    /// Finds the closest active Projectile in the scene that is currently being managed (i.e., launched).
    /// It iterates through the static list maintained by ProjectileLauncher.
    /// </summary>
    /// <returns>The closest Projectile, or null if none are found.</returns>
    private Projectile FindClosestActiveProjectile()
    {
        Projectile closestProjectile = null;
        float minDistance = Mathf.Infinity;

        Debug.Log($"ShootController: FindClosestActiveProjectile: Checking {ProjectileLauncher.ActiveProjectiles.Count} active projectiles.");

        // Iterate through the static list of active projectiles
        foreach (Projectile proj in ProjectileLauncher.ActiveProjectiles)
        {
            // Ensure the projectile object is still valid and active in the hierarchy
            if (proj != null && proj.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(gunPoint.position, proj.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestProjectile = proj;
                }
            }
            else
            {
                // Log if a projectile in the list is null or inactive
                Debug.LogWarning($"ShootController: Projectile in ActiveProjectiles list is null or inactive: {proj?.name ?? "NULL"}");
            }
        }
        if (closestProjectile == null)
        {
            Debug.Log("ShootController: FindClosestActiveProjectile: No valid projectile found in list.");
        }
        return closestProjectile;
    }

    /// <summary>
    /// Event handler for GameTimeManager.OnTimeResumed.
    /// This method is called AFTER GameTimeManager has finished resuming all rigidbodies.
    /// </summary>
    private void OnGameTimeResumed()
    {

        Debug.Log("ShootController: OnGameTimeResumed event received.");
        if (_projectileToRedirectOnResume != null && isRedirectionValid && canShoot)
        {
            Debug.Log($"ShootController: Redirecting projectile {_projectileToRedirectOnResume.name} via OnTimeResumed event.");
            ProjectileLauncher.RedirectExistingProjectile(_projectileToRedirectOnResume, _newVelocityOnResume);
            _projectileToRedirectOnResume = null; // Clear after use
        }
        else
        {
            Debug.Log("ShootController: No valid projectile to redirect on resume, or redirection was invalid.");
        }
        // Ensure isRedirectionValid is reset for the next cycle
        isRedirectionValid = false;
    } //Event for handling time resume
    public void OnBulletHitProjectile()
    {
        isAimingPushShot = false;
        VisualsManager.HideAllVisuals(); // Hide all visuals (including circles)
        Debug.Log("RMB Up: Hiding visuals.");

        // Store projectile and velocity for event-based redirection
        _projectileToRedirectOnResume = currentTargetProjectile;
        _newVelocityOnResume = cachedRedirectionVelocity;

        // NEW: Resume time. The actual redirection will happen via the OnGameTimeResumed event.
        GameTimeManager.Instance.ResumeTime();
        Debug.Log("RMB Up: Resuming time. Redirection will trigger via event.");

        currentTargetProjectile = null; // Clear reference immediately
    }
    private void CheckForBulletInterruption()
    {
        if (currentTargetProjectile == null || gunPoint == null)
            return;

        _lastShotData = VisualsManager.DrawShot();
        canShoot = !_lastShotData.WasInterrupted;
    }

    public void RotateFist(GameObject ObjectToLookAt, float RotationSpeed)
    {
            Vector3 direction = ObjectToLookAt.transform.position - transform.position;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        Rigidbody localrb = Fist.GetComponent<Rigidbody>();

            localrb.rotation = Quaternion.RotateTowards(localrb.rotation, targetRotation, RotationSpeed * Time.deltaTime);
    }


}
