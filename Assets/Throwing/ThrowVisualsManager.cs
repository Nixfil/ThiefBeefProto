// FILE: ThrowVisualsManager.cs
using UnityEngine;

// Define a struct to return information about the trajectory's visual outcome
public struct ThrowData
{
    public Vector3 StartPosition;
    public Vector3 EndPosition;
    public bool WasInterrupted;
    public Vector3 InterruptPoint;
    public bool InterruptedByTable;
    public RaycastHit LastHit;
}

public class ThrowVisualsManager : MonoBehaviour
{
    [Header("Visual References")]
    public LineRenderer lineRenderer;
    public GameObject RangeCirclePrefab;
    public GameObject ghostIndicatorPrefab;
    public GameObject interruptMarkerPrefab;
    public GameObject Visuals;

    [Header("Visual Materials & Colors")]
    public Material CorrectThrowMaterial;
    public Material WrongThrowMaterial;
    public Color CorrectThrowColor;
    public Color WrongThrowColor;

    // Internal instances of prefabs
    private RangeCircleRenderer _rangeCircleInstance;
    private GameObject _ghostInstance;
    private GameObject _interruptMarkerInstance;

    // --- Configuration values passed from ThrowController ---
    private float _aimOffset;
    private float _minThrowRange;
    private float _maxThrowRange;
    private int _trajectorySteps;
    private float _trajectoryStepDeltaTime;
    private LayerMask _interruptThrowMask;
    private LayerMask _triggerInterruptLayerMask;
    private LayerMask _groundMask;

    private void Awake()
    {
        Visuals = new GameObject("ThrowingVisuals");
        var newCircle = Instantiate(RangeCirclePrefab, Visuals.transform);
        _rangeCircleInstance = newCircle.GetComponent<RangeCircleRenderer>();
    }

    // Call this from ThrowController's Start()
    public void Initialize(float minRange, float maxRange, float aimOffset, int trajectorySteps, float trajectoryStepDeltaTime, LayerMask interruptMask, LayerMask triggerMask, LayerMask groundMask)
    {
        _minThrowRange = minRange;
        _maxThrowRange = maxRange;
        _aimOffset = aimOffset;
        _trajectorySteps = trajectorySteps;
        _trajectoryStepDeltaTime = trajectoryStepDeltaTime;
        _interruptThrowMask = interruptMask;
        _triggerInterruptLayerMask = triggerMask;
        _groundMask = groundMask;

        if (lineRenderer == null || RangeCirclePrefab == null)
        {
            Debug.LogError("Essential visual references are missing in ThrowVisualsManager! Please assign them in the Inspector.", this);
            enabled = false;
            return;
        }

        _rangeCircleInstance.SetRadius(_maxThrowRange-1);
        _rangeCircleInstance.ToggleCircle(false);
        lineRenderer.enabled = false;
    }

    /// <summary>
    /// Updates and displays all aiming visuals based on the current throw state.
    /// Returns detailed information about the trajectory's visual outcome.
    /// </summary>
    /// <param name="spawnPoint">The point from which the projectile is launched.</param>
    /// <param name="targetPoint">The computed target point for the trajectory.</param>
    /// <param name="velocity">The calculated initial velocity.</param>
    /// <returns>A ThrowData struct containing details about the trajectory's path and interruptions.</returns>
    public ThrowData ShowAimingVisuals(Vector3 spawnPoint, Vector3? targetPoint, Vector3 velocity)
    {
        // Ensure line renderer and range circles are active when visuals are shown
        lineRenderer.enabled = true;
        _rangeCircleInstance.ToggleCircle(true);
        _rangeCircleInstance.SetCenter(transform.position);

        ThrowData currentThrowData = new ThrowData
        {
            StartPosition = spawnPoint,
            EndPosition = spawnPoint + velocity * (_trajectorySteps * _trajectoryStepDeltaTime) + 0.5f * Physics.gravity * (_trajectorySteps * _trajectoryStepDeltaTime) * (_trajectorySteps * _trajectoryStepDeltaTime), // Default end
            WasInterrupted = false,
            InterruptPoint = Vector3.zero,
            InterruptedByTable = false,
            LastHit = default
        };

        // Draw the trajectory line and get interruption data
        TrajectoryCalculator.DrawTrajectory(
            lineRenderer,
            spawnPoint,
            velocity,
            _trajectorySteps,
            _trajectoryStepDeltaTime,
            _interruptThrowMask,
            _triggerInterruptLayerMask,
            _groundMask,
            out currentThrowData.WasInterrupted,
            out currentThrowData.InterruptPoint,
            out currentThrowData.InterruptedByTable,
            out currentThrowData.LastHit
        );

        if (currentThrowData.WasInterrupted)
        {
            currentThrowData.EndPosition = currentThrowData.InterruptPoint;
        }
        else
        {
            // If not interrupted, the EndPosition should be the last point drawn by the line renderer
            currentThrowData.EndPosition = lineRenderer.GetPosition(lineRenderer.positionCount - 1);
        }


        // Determine validity for visual coloring based on interruption
        bool currentIsThrowValid = !currentThrowData.InterruptedByTable; // Invalid if it hit a table

        // If it hit ground but not a table, it's valid
        if (currentThrowData.WasInterrupted && ((1 << currentThrowData.LastHit.collider.gameObject.layer) & _groundMask) != 0 && !currentThrowData.InterruptedByTable)
        {
            currentIsThrowValid = true;
        }
        // If it hit something else (like a wall), it's also valid, unless it's a table
        else if (currentThrowData.WasInterrupted && !currentThrowData.InterruptedByTable)
        {
            currentIsThrowValid = true;
        }
        // If no interruption, and a target point exists, it's generally valid for visuals
        else if (!currentThrowData.WasInterrupted && targetPoint.HasValue)
        {
            currentIsThrowValid = true;
        }
        else // No valid target or other invalid conditions
        {
            currentIsThrowValid = false;
        }


        // Update colors for all visuals
        SetAimingColors(currentIsThrowValid);


        // --- Ghost Indicator Management ---
        if (targetPoint.HasValue && lineRenderer.enabled)
        {
            if (_ghostInstance == null && ghostIndicatorPrefab != null)
            {
                _ghostInstance = Instantiate(ghostIndicatorPrefab);
            }
            if (_ghostInstance != null)
            {
                _ghostInstance.SetActive(true);
                Vector3 ghostPosition = targetPoint.Value;
                ghostPosition.y += 0.3f; // Small vertical offset
                Vector3 closerPoint = Vector3.MoveTowards(ghostPosition, spawnPoint, _aimOffset);
                _ghostInstance.transform.position = closerPoint;
            }
        }
        else
        {
            if (_ghostInstance != null) _ghostInstance.SetActive(false);
        }

        // --- Interrupt Marker Management ---
        // New logic to control the X marker's visibility
        bool showInterruptMarker = false;
        if (currentThrowData.WasInterrupted)
        {
            // If it was interrupted by a table, always show the marker
            if (currentThrowData.InterruptedByTable)
            {
                showInterruptMarker = true;
            }
            // If it was interrupted, AND it was NOT by the ground layer, show the marker (i.e., a wall)
            else if (((1 << currentThrowData.LastHit.collider.gameObject.layer) & _groundMask) == 0)
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
                    rotator.SetPositionAndOrientation(currentThrowData.InterruptPoint, currentThrowData.LastHit.normal);
            }
        }
        else // Hide marker if it should not be active
        {
            if (_interruptMarkerInstance != null) _interruptMarkerInstance.SetActive(false);
        }

        return currentThrowData;
    }

    /// <summary>
    /// Hides all visual elements related to aiming.
    /// </summary>
    public void HideAllVisuals()
    {
        lineRenderer.enabled = false;
        _rangeCircleInstance.ToggleCircle(false);
        if (_ghostInstance != null) _ghostInstance.SetActive(false);
        if (_interruptMarkerInstance != null) _interruptMarkerInstance.SetActive(false);
    }

    /// <summary>
    /// Sets the material and color of all aiming visuals based on throw validity.
    /// </summary>
    /// <param name="isValid">True for correct (green) materials/colors, false for wrong (red).</param>
    private void SetAimingColors(bool isValid)
    {
        Material currentMaterial = isValid ? CorrectThrowMaterial : WrongThrowMaterial;
        Color currentColor = isValid ? CorrectThrowColor : WrongThrowColor;


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