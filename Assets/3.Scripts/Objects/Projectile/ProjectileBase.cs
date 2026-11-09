using System.Collections;
using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
    [Header("Time")]
    [SerializeField, Min(0f)] protected float speed = 20f;
    [SerializeField, Min(0f)] protected float lifeTime = 5f;

    [Header("Component")]
    [SerializeField] protected Rigidbody rigid;

    protected AttackInfo attackInfo;
    protected CharacterBase attacker;
    protected bool isInitialized;
    protected bool hasHit;
    protected Vector3 moveDirection;

    private Coroutine returnCoroutine;

    public void Initialize(in AttackInfo info, Vector3 spawnPosition, Vector3 direction)
    {
        attackInfo = info;
        attacker = info.instigator ? info.instigator.Character : null;
        moveDirection = direction.normalized;
        hasHit = false;

        Quaternion spawnRotation = Quaternion.LookRotation(moveDirection);

        // 풀에서 재사용할 때 물리 위치와 Transform 위치를 함께 초기화한다.
        rigid.position = spawnPosition;
        rigid.rotation = spawnRotation;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        isInitialized = true;

        GameManager.OnPhysicsObject -= PhysicsUpdate;
        GameManager.OnPhysicsObject += PhysicsUpdate;

        ReturnAfter(lifeTime);
        OnInitialized();
    }

    private void PhysicsUpdate(float deltaTime)
    {
        if (!isInitialized || hasHit) return;

        Vector3 nextPosition = rigid.position + moveDirection * speed * deltaTime;
        rigid.MovePosition(nextPosition);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized || hasHit) return;

        CharacterBase hitCharacter = other.GetComponent<CharacterBase>();
        if (!hitCharacter) return;
        if (hitCharacter == attacker) return;
        if (attacker && hitCharacter.Team == attacker.Team) return;

        HitPointModule targetHP = hitCharacter.GetModule<HitPointModule>();
        if (!targetHP) return;

        hasHit = true;
        GameManager.OnPhysicsObject -= PhysicsUpdate;
        CancelScheduledReturn();

        targetHP.TakeDamage(new DamageStruct
        {
            from = attacker ? attacker.gameObject : gameObject,
            instigator = attackInfo.instigator,
            damageAmount = attackInfo.damageAmount
        });

        OnHit(hitCharacter, other);
    }

    // 자식도 이 메서드로 명중 후 반환 시간을 지정할 수 있다.
    protected void ReturnAfter(float delay)
    {
        CancelScheduledReturn();
        returnCoroutine = StartCoroutine(ReturnAfterDelay(delay));
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        returnCoroutine = null;
        ObjectManager.DestroyObject(gameObject);
    }

    private void CancelScheduledReturn()
    {
        if (returnCoroutine == null) return;

        StopCoroutine(returnCoroutine);
        returnCoroutine = null;
    }

    private void OnDisable()
    {
        GameManager.OnPhysicsObject -= PhysicsUpdate;
        CancelScheduledReturn();

        OnReleased();

        attackInfo = default;
        attacker = null;
        isInitialized = false;
        hasHit = false;
        moveDirection = Vector3.zero;
    }

    protected virtual void OnInitialized() { }
    protected abstract void OnHit(CharacterBase target, Collider hitCollider);
    protected virtual void OnReleased() { }
}
