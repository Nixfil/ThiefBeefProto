using UnityEngine;

public class ThrowController : MonoBehaviour
{
    public ThrowTrajectoryData trajectoryData;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private TrajectoryPreviewer trajectoryPreviewer;

    public void PreviewThrow(Vector3 start, Vector3 end)
    {
        trajectoryPreviewer.DrawTrajectory(start, end, trajectoryData.arcHeight, trajectoryData.previewResolution);
        UIManager.Instance.HideCursor();
    }

    public void CancelPreview()
    {
        trajectoryPreviewer.Clear();
        UIManager.Instance.ShowCursor();
    }

    public void ExecuteThrow(Vector3 start, Vector3 end)
    {
        GameObject instance = Instantiate(projectilePrefab, start, Quaternion.identity);
        ProjectileThrower thrower = instance.GetComponent<ProjectileThrower>();

        if (thrower != null)
        {
            Debug.Log("Throwing");
            thrower.Throw(start, end, trajectoryData.throwDuration, trajectoryData.arcHeight);
        }

        trajectoryPreviewer.Clear();
        UIManager.Instance.ShowCursor();
    }

}
