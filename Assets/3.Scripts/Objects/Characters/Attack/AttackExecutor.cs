using UnityEngine;

public abstract class AttackExecutor : MonoBehaviour
{
    [SerializeField] string executionStateTag = "Attack";

    public string ExecutionStateTag => executionStateTag;

    public abstract void Execute(in AttackInfo info);

    public virtual void Prepare() { }
}
