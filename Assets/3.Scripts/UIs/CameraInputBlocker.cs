using UnityEngine;

public class CameraInputBlocker : MonoBehaviour
{
    static int activeCount;
    public static bool IsBlocked => activeCount > 0;

    void OnEnable() => activeCount++;
    void OnDisable() => activeCount--;
}
