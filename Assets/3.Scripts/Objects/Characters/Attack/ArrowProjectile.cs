using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [SerializeField, Min(0f)] float speed = 20f;
    [SerializeField, Min(0f)] float lifeTime = 5f;

    Rigidbody rigid;
    AttackInfo attackInfo;
    CharacterBase attacker;
    bool isInitialized;
    bool hasHit;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    public void Initialize(in AttackInfo info, Vector3 direction)
    {
        attackInfo = info;
        attacker = info.instigator ? info.instigator.Character : null;
        transform.forward = direction.normalized;

        isInitialized = true;
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (!isInitialized || hasHit) return;

        Vector3 nextPosition = rigid.position + transform.forward * speed * Time.deltaTime;
        rigid.MovePosition(nextPosition);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized || hasHit) return;

        CharacterBase hitCharacter = other.GetComponent<CharacterBase>();
        if (!hitCharacter) return;
        if (!hitCharacter == attacker) return; //발사자 자신은 무시해야함
        if (attacker && hitCharacter.Team == attacker.Team) return; //아군도 무시

        HitPointModule targetHP = hitCharacter.GetModule<HitPointModule>();
        if (!targetHP) return;
        hasHit = true;

        targetHP.TakeDamage(new DamageStruct
        {
            from = attacker ? attacker.gameObject : gameObject,
            instigator = attackInfo.instigator,
            damageAmount = attackInfo.damageAmount
        });
        Destroy(gameObject);
    }
}
