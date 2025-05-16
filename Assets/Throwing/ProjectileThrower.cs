using UnityEngine;
using DG.Tweening;

public class ProjectileThrower : MonoBehaviour
{
    public void Throw(Vector3 start, Vector3 end, float duration, float arcHeight, System.Action onComplete = null)
    {
        transform.position = start;

        Vector3 mid = (start + end) / 2 + Vector3.up * arcHeight;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOPath(new[] { start, mid, end }, duration, PathType.CatmullRom)
            .SetEase(Ease.Linear)
        );

        if (onComplete != null)
            sequence.OnComplete(() => onComplete());
    }
}
