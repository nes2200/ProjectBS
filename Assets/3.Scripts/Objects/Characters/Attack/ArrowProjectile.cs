using System.Collections;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Time")]
    [SerializeField, Min(0f)] float speed = 20f;
    [SerializeField, Min(0f)] float lifeTime = 5f;
    [SerializeField, Min(0f)] float hitRemainTime = 0.5f;
    [Header("Component")]
    [SerializeField] Rigidbody rigid;
    [SerializeField] TrailRenderer trail;
    
    //공격 정보
    AttackInfo attackInfo;
    CharacterBase attacker;
    bool isInitialized;
    bool hasHit;

    //화살 위치 정보
    Vector3 moveDirection;
    Transform attachedTarget;
    Vector3 attachedLocalPosition;
    Quaternion attachedLocalRotation;

    private Coroutine lifeCoroutine;

    public void Initialize(in AttackInfo info, Vector3 spawnPosition, Vector3 direction)
    {
        attackInfo = info;
        attacker = info.instigator ? info.instigator.Character : null;

        moveDirection = direction.normalized;
        Quaternion spawnRotation = Quaternion.LookRotation(moveDirection);

        // 풀에서 사용하던 이전 Rigidbody 상태를 현재 발사 위치로 초기화
        rigid.position = spawnPosition;
        rigid.rotation = spawnRotation;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        trail.Clear();
        trail.emitting = true;

        isInitialized = true;

        GameManager.OnPhysicsObject -= PhysicsUpdate;
        GameManager.OnPhysicsObject += PhysicsUpdate;

        lifeCoroutine = StartCoroutine(ReturnAfterLifetime());
    }

    private IEnumerator ReturnAfterLifetime()
    {
        yield return new WaitForSeconds(lifeTime);
        lifeCoroutine = null;
        ObjectManager.DestroyObject(gameObject);
    }
    private IEnumerator ReturnAfterHit()
    {
        yield return new WaitForSeconds(hitRemainTime);
        lifeCoroutine = null;
        ObjectManager.DestroyObject(gameObject);
    }

    private void OnDisable()
    {
        GameManager.OnPhysicsObject -= PhysicsUpdate;
        GameManager.OnLateObject -= FollowTarget;

        StopAllCoroutines();

        trail.emitting = false;
        trail.Clear();

        attackInfo = default;
        attacker = null;
        isInitialized = false;
        hasHit = false;
        attachedTarget = null;
    }

    private void PhysicsUpdate(float deltaTime)
    {
        if (!isInitialized || hasHit) return;

        Vector3 nextPosition = rigid.position + moveDirection * speed * deltaTime;
        rigid.MovePosition(nextPosition);
    }
    private void FollowTarget(float deltaTime)
    {
        if (!hasHit) return;
        if (!attachedTarget)
        {
            ObjectManager.DestroyObject(gameObject);
            return;
        }

        Vector3 worldPosition = attachedTarget.TransformPoint(attachedLocalPosition);
        Quaternion worldRotation = attachedTarget.rotation * attachedLocalRotation;
        transform.SetPositionAndRotation(worldPosition, worldRotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized || hasHit) return;

        CharacterBase hitCharacter = other.GetComponent<CharacterBase>();
        if (!hitCharacter) return;
        if (hitCharacter == attacker) return; //발사자 자신은 무시해야함
        if (attacker && hitCharacter.Team == attacker.Team) return; //아군도 무시

        HitPointModule targetHP = hitCharacter.GetModule<HitPointModule>();
        if (!targetHP) return;

        hasHit = true;

        GameManager.OnPhysicsObject -= PhysicsUpdate;

        GameManager.OnLateObject -= FollowTarget;
        GameManager.OnLateObject += FollowTarget;

        attachedTarget = other.transform;
        attachedLocalPosition = attachedTarget.InverseTransformPoint(transform.position);
        attachedLocalRotation = Quaternion.Inverse(attachedTarget.rotation) * transform.rotation;

        trail.emitting = false;

        targetHP.TakeDamage(new DamageStruct
        {
            from = attacker ? attacker.gameObject : gameObject,
            instigator = attackInfo.instigator,
            damageAmount = attackInfo.damageAmount
        });

        if(lifeCoroutine != null)
        {
            StopCoroutine(lifeCoroutine);
        }
        lifeCoroutine = StartCoroutine(ReturnAfterHit());
    }
}
