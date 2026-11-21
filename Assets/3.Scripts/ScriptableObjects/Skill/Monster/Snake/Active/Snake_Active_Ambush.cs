using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "Snake_Active_Ambush", menuName = "Scriptable Objects/Skill/Monster/Snake/Snake_Active_Ambush")]
public class Snake_Active_Ambush : ActiveSkillContainer
{
    [SerializeField, Min(0f)] float emergeDelay = 0.5f;
    [SerializeField, Min(0f)] float castRange = 3f;
    [SerializeField] Snake_Poisoned poisonEffect;

    const float UndergroundEntryTimeout = 1f;
    const float SpawnTimeout = 5f;
    const float SampleRadius = 0.25f;
    const float Clearance = 0.05f;
    const float MaxFloorHeightDifference = 0.75f;

    static readonly int UndergroundTrigger = Animator.StringToHash("Underground");
    static readonly int SpawnTrigger = Animator.StringToHash("Spawn");
    static readonly int LocomotionState = Animator.StringToHash("Base.Locomotion");
    static readonly int UndergroundState = Animator.StringToHash("Base.Underground");
    static readonly int SpawnState = Animator.StringToHash("Base.Spawn");

    enum Phase { EnteringUnderground, Underground, Spawning }

    public override bool CanUse(CharacterBase user, CharacterBase target)
    {
        if (!base.CanUse(user, target)) return false;

        var stage = GetStage(user);
        return CanStart(user, stage, user.GetComponent<Animator>(), user.GetComponent<NavMeshAgent>()) &&
            FindLowestHpEnemy(user, stage.CharacterRegistry, user.transform.position);
    }

    public override void UseSkill(CharacterBase user, CharacterBase target, Action requestCooldown)
    {
        if (!user)
        {
            requestCooldown?.Invoke();
            return;
        }

        var stage = GetStage(user);
        var animator = user.GetComponent<Animator>();
        var agent = user.GetComponent<NavMeshAgent>();
        if (!CanStart(user, stage, animator, agent))
        {
            requestCooldown?.Invoke();
            return;
        }

        var registry = stage.CharacterRegistry;
        var origin = user.transform.position;
        var victim = FindLowestHpEnemy(user, registry, origin);
        if (!victim)
        {
            requestCooldown?.Invoke();
            return;
        }

        var skills = user.GetModule<SkillModule>();
        var movement = user.GetModule<MovementModule>();
        var phase = Phase.EnteringUnderground;
        float timeRemaining = UndergroundEntryTimeout;
        bool victimRemoved = false;
        bool finished = false;

        void Finish()
        {
            if (finished) return;
            finished = true;

            GameManager.OnUpdateCharacter -= Tick;
            StageManager.OnStageStateChange -= OnStageChanged;
            user.OnFaint -= Finish;
            user.OnCharacterRemoved -= OnUserRemoved;
            registry.OnCharacterRemoved -= OnTargetRemoved;
            if (skills) skills.OnActiveSkillChanged -= OnSkillChanged;

            if (animator && animator.isInitialized)
            {
                animator.ResetTrigger(UndergroundTrigger);
                animator.ResetTrigger(SpawnTrigger);
                if (user && user.IsAlive && animator.isActiveAndEnabled &&
                    (IsInOrEntering(animator, UndergroundState) || IsInOrEntering(animator, SpawnState)))
                    animator.Play(LocomotionState, 0, 0f);
            }

            requestCooldown?.Invoke();
        }

        void OnUserRemoved(CharacterBase removed) => Finish();
        void OnSkillChanged(SkillContainer previous, SkillContainer current) => Finish();
        void OnTargetRemoved(CharacterBase removed)
        {
            if (removed == victim) victimRemoved = true;
        }

        void OnStageChanged(StageState previous, StageState current)
        {
            if (!stage || stage.CurrentState != StageState.Battle) Finish();
        }

        void Emerge()
        {
            CharacterBase landingTarget = null;
            if (!victimRemoved && IsEnemy(user, victim))
                landingTarget = FindLandingTarget(user, registry, agent, origin, victim);

            if (landingTarget)
            {
                Vector3 facing = landingTarget.transform.position - user.transform.position;
                facing.y = 0f;
                if (facing.sqrMagnitude > 0f)
                    user.transform.rotation = Quaternion.LookRotation(facing);
            }
            else if (agent && agent.isOnNavMesh && (user.transform.position - origin).sqrMagnitude > 0.0001f &&
                IsPositionFree(user, registry, agent, origin))
            {
                agent.Warp(origin);
            }

            phase = Phase.Spawning;
            timeRemaining = SpawnTimeout;
            animator.SetTrigger(SpawnTrigger);
            if (landingTarget && poisonEffect)
                landingTarget.GetModule<EffectModule>()?.ApplyEffect(user, poisonEffect);
        }

        void Tick(float deltaTime)
        {
            if (finished) return;
            if (!user || !user.IsAlive || !user.gameObject.activeInHierarchy ||
                !animator || !animator.isActiveAndEnabled || !stage ||
                stage.CurrentState != StageState.Battle || !registry)
            {
                Finish();
                return;
            }

            switch (phase)
            {
                case Phase.EnteringUnderground:
                    if (IsInOrEntering(animator, UndergroundState))
                    {
                        phase = Phase.Underground;
                        timeRemaining = Mathf.Max(0f, emergeDelay);
                        if (timeRemaining == 0f)
                            Emerge();
                    }
                    else if ((timeRemaining -= deltaTime) <= 0f)
                    {
                        Finish();
                    }
                    break;

                case Phase.Underground:
                    if (!IsInOrEntering(animator, UndergroundState))
                        Finish();
                    else if ((timeRemaining -= deltaTime) <= 0f)
                        Emerge();
                    break;

                case Phase.Spawning:
                    if ((!animator.IsInTransition(0) &&
                        animator.GetCurrentAnimatorStateInfo(0).fullPathHash == LocomotionState) ||
                        (timeRemaining -= deltaTime) <= 0f)
                        Finish();
                    break;
            }
        }

        user.OnFaint += Finish;
        user.OnCharacterRemoved += OnUserRemoved;
        registry.OnCharacterRemoved += OnTargetRemoved;
        if (skills) skills.OnActiveSkillChanged += OnSkillChanged;
        StageManager.OnStageStateChange += OnStageChanged;
        GameManager.OnUpdateCharacter += Tick;

        if (movement)
        {
            movement.StopMovement();
            movement.Rotate(Vector3.zero);
        }
        user.MovementNotify(Vector3.zero);
        animator.SetTrigger(UndergroundTrigger);
    }

    bool CanStart(CharacterBase user, StageManager stage, Animator animator, NavMeshAgent agent)
    {
        if (!user.IsAlive || !user.gameObject.activeInHierarchy || !user.Status || !poisonEffect ||
            !stage || stage.CurrentState != StageState.Battle || !stage.CharacterRegistry ||
            !animator || !animator.isActiveAndEnabled || !animator.isInitialized ||
            !agent || !agent.isOnNavMesh)
            return false;

        var attack = user.GetModule<AttackModule>();
        return (!attack || !attack.IsAttacking) && !animator.IsInTransition(0) &&
            animator.GetCurrentAnimatorStateInfo(0).fullPathHash == LocomotionState;
    }

    static StageManager GetStage(CharacterBase user)
    {
        var team = user.GetComponentInParent<TeamEliminateNotifier>();
        return team ? team.Stage : null;
    }

    static bool IsInOrEntering(Animator animator, int state)
    {
        return animator.GetCurrentAnimatorStateInfo(0).fullPathHash == state ||
            (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).fullPathHash == state);
    }

    static bool IsEnemy(CharacterBase user, CharacterBase other)
    {
        return other && other.IsAlive && other.gameObject.activeInHierarchy && other.Status &&
            other.Team != TeamID.None && other.Team != user.Team;
    }

    bool TryGetTargetHp(CharacterBase user, CharacterBase other, Vector3 origin, out int hp)
    {
        hp = 0;
        if (!IsEnemy(user, other)) return false;

        Vector3 offset = other.transform.position - origin;
        offset.y = 0f;
        float range = Mathf.Max(0f, castRange);
        if (offset.sqrMagnitude > range * range) return false;

        var hitPoints = other.GetModule<HitPointModule>();
        if (!hitPoints || !other.GetModule<AttackSlotModule>()) return false;

        hp = hitPoints.HPCurrent;
        return true;
    }

    CharacterBase FindLowestHpEnemy(CharacterBase user, StageCharacterRegistry registry, Vector3 origin)
    {
        CharacterBase best = null;
        int lowestHp = int.MaxValue;
        foreach (var other in registry.Characters)
        {
            if (!TryGetTargetHp(user, other, origin, out int hp) || (best && hp >= lowestHp)) continue;

            best = other;
            lowestHp = hp;
        }
        return best;
    }

    CharacterBase FindLandingTarget(CharacterBase user, StageCharacterRegistry registry, NavMeshAgent agent,
        Vector3 origin, CharacterBase preferredTarget)
    {
        if (!agent || !agent.isOnNavMesh) return null;
        if (TryGetTargetHp(user, preferredTarget, origin, out _) &&
            TryWarpBehind(user, preferredTarget, registry, agent))
            return preferredTarget;

        var alternatives = new List<(CharacterBase Target, int Hp)>();
        foreach (var other in registry.Characters)
        {
            if (other != preferredTarget && TryGetTargetHp(user, other, origin, out int hp))
                alternatives.Add((other, hp));
        }

        alternatives.Sort((left, right) => left.Hp.CompareTo(right.Hp));
        foreach (var candidate in alternatives)
        {
            if (TryWarpBehind(user, candidate.Target, registry, agent))
                return candidate.Target;
        }

        return null;
    }

    static bool TryWarpBehind(CharacterBase user, CharacterBase victim, StageCharacterRegistry registry,
        NavMeshAgent agent)
    {
        var slots = victim.GetModule<AttackSlotModule>();
        if (!slots) return false;

        Vector3 targetPosition = victim.transform.position;
        Vector3 rear = -victim.transform.forward;
        rear.y = 0f;
        if (rear.sqrMagnitude < 0.001f) return false;
        rear.Normalize();

        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };

        bool TryLand(Vector3 candidate)
        {
            if (!NavMesh.SamplePosition(candidate, out var hit, SampleRadius, filter)) return false;
            if (Vector3.Dot(hit.position - targetPosition, rear) <= 0.0001f ||
                NavMesh.Raycast(targetPosition, hit.position, out _, filter))
                return false;

            return IsPositionFree(user, registry, agent, hit.position) && agent.Warp(hit.position);
        }

        if (!slots.TryReserveRearSlot(user, rear, TryLand)) return false;

        if (user.Controller is HostileAIController controller)
            controller.SetFocusTarget(victim.gameObject);
        else
            slots.Release(user);

        return true;
    }

    static bool IsPositionFree(CharacterBase user, StageCharacterRegistry registry, NavMeshAgent agent,
        Vector3 position)
    {
        float radius = Mathf.Max(user.Status.colliderRadius, agent.radius);
        foreach (var other in registry.Characters)
        {
            if (!other || other == user || !other.IsAlive || !other.gameObject.activeInHierarchy || !other.Status)
                continue;

            Vector3 offset = other.transform.position - position;
            if (Mathf.Abs(offset.y) > MaxFloorHeightDifference) continue;
            offset.y = 0f;
            float minDistance = radius + other.Status.colliderRadius + Clearance;
            if (offset.sqrMagnitude < minDistance * minDistance) return false;
        }

        Vector3 bottom = position + Vector3.up * radius;
        Vector3 top = position + Vector3.up * Mathf.Max(radius, agent.height - radius);
        return !Physics.CheckCapsule(bottom, top, radius, LayerMask.GetMask("Obstacle"),
            QueryTriggerInteraction.Ignore);
    }
}
