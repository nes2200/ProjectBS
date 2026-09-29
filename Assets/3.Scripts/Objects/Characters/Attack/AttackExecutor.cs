using UnityEngine;

public abstract class AttackExecutor : MonoBehaviour
{
    [SerializeField] string executionStateTag = "Attack";
    [SerializeField, Range(0f, 1f)] float executeNormalizedTime;

    public string ExecutionStateTag => executionStateTag;
    public float ExecuteNormalizedTime => executeNormalizedTime;

    public abstract void Execute(in AttackInfo info);

    public virtual void Prepare() { }
}
