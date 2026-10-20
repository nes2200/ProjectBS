using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MovementModule : CharacterModule, IRunnable
{
    [Header("이동 속도")]
    [SerializeField] float moveSpeed = 2.0f;
    [SerializeField] float rotateSpeed = 2.0f;

    [Header("필수 부속품들")]
    [SerializeField] NavMeshAgent navAgent;

    [Header("아군 겹침 보정")]
    [SerializeField, Min(0f)] float separationSearchRadius = 2f; //주변 유닛 검색 범위
    [SerializeField, Range(0.5f, 1.2f)] float separationDistanceRatio = 1f; //허용할 겹침 정도
    [SerializeField, Min(0f)] float separationStrenth = 5f; //겸침을 해소하는 힘
    [SerializeField, Min(0f)] float maxSeparationSpeed = 1f; //갑자기 튕기는것을 막는 보정속도

    readonly Collider[] separationHits = new Collider[32];
    readonly HashSet<CharacterBase> separationNeighbors = new();

    int characterLayerMask;

    //회전용
    Vector3? targetRotationDirection;


    public sealed override System.Type RegistrationType => typeof(MovementModule);

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);

        GameManager.OnPhysicsCharacter -= MovementUpdate;
        GameManager.OnPhysicsCharacter += MovementUpdate;
        SetNavmeshAgent();


        newOwner.OnFaint -= StopAllMovementByFaint;
        newOwner.OnFaint += StopAllMovementByFaint;
    }

    public override void OnUnregistration(CharacterBase oldOwner)
    {
        base.OnUnregistration(oldOwner);

        GameManager.OnPhysicsCharacter -= MovementUpdate;
        oldOwner.OnFaint -= StopAllMovementByFaint;
    }

    public void SetNavmeshAgent()
    {
        if (!navAgent) navAgent = GetComponent<NavMeshAgent>();

        // NavMeshAgent는 경로와 위치만 담당하고, 회전은 UpdateRotation에서 직접 제어한다.
        navAgent.updateRotation = false;
        navAgent.speed = Owner.finalStatus.moveSpeed;
        navAgent.autoBraking = false;

        //우선순위 변경으로 양보하기 
        int priorityOffset = (Owner.GetInstanceID() & int.MaxValue) % 11;

        characterLayerMask = LayerMask.GetMask("Character");
        navAgent.avoidancePriority = 45 + priorityOffset;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
    }

    // 실제 목적지는 TargetingModule이 스캔한 위치로 계속 갱신한다.
    public void SetDestination(Vector3 targetPosition)
    {
        if (!navAgent || !navAgent.isOnNavMesh) return;

        navAgent.SetDestination(targetPosition);
    }

    public void MovementUpdate(float deltaTime)
    {
        if (!navAgent || !navAgent.isOnNavMesh) return;

        Vector3 origin = transform.position + Vector3.up;

        Vector3 pathDirection = navAgent.steeringTarget - transform.position;
        pathDirection.y = 0f;

        UpdateRotation(deltaTime);

        if(!navAgent.isStopped && !navAgent.pathPending && navAgent.hasPath)
        {
            if(navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                StopMovement();
            }
        }
        ApplyFriendlySeparation(deltaTime);

        Vector3 positionDelta = navAgent.velocity * deltaTime;
        Owner.MovementNotify(positionDelta);
    }
 
    // destination은 TargetingModule이 담당하고, 이 명령은 정지 허용 거리만 설정한다.
    public void MoveToDestination(Vector3 destination, float tolerance)
    {
        if (!navAgent || !navAgent.isOnNavMesh) return;

        // 새 이동이 시작되면 전투용 회전 명령을 해제하고 경로 진행 방향을 바라본다.
        targetRotationDirection = null;

        navAgent.isStopped = false;
        navAgent.stoppingDistance = Mathf.Max(0f, tolerance);
        navAgent.SetDestination(destination);
    }

    //회전 관련
    public void Rotate(Vector3 direction)
    {
        direction.y = 0f;
        targetRotationDirection = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : null;
    }
    private void UpdateRotation(float deltaTime)
    {
        Vector3 direction;

        if (targetRotationDirection.HasValue)
        {
            // 공격 등 컨트롤러가 명시한 방향을 우선한다.
            direction = targetRotationDirection.Value;
        }
        else
        {
            // 회피 속도가 아니라 NavMesh 경로의 다음 진행 지점을 바라본다.
            // 경로가 없거나 아직 계산 중이면 현재 방향을 유지한다.
            if (navAgent.isStopped || navAgent.pathPending || !navAgent.hasPath) return;

            direction = navAgent.steeringTarget - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;

            direction.Normalize();
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * deltaTime);
    }

    private void ApplyFriendlySeparation(float deltaTime)
    {
        //공격 중이거나 목적지에 도착해 정지한 유닛은 움직이지 않음
        if (!navAgent.isOnNavMesh) return;

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position, separationSearchRadius, separationHits, characterLayerMask, QueryTriggerInteraction.Ignore);

        separationNeighbors.Clear();

        Vector3 pathDirection = navAgent.steeringTarget - transform.position;
        pathDirection.y = 0f;
        if(pathDirection.sqrMagnitude <= 0.001f)
        {
            
            pathDirection = transform.forward;
            pathDirection.y = 0;
        }
        pathDirection.Normalize();

        Vector3 rightDirection = Vector3.Cross(Vector3.up, pathDirection).normalized;
        Vector3 separation = Vector3.zero;

        for(int i = 0; i < hitCount; i++)
        {
            Collider hit = separationHits[i];
            if (!hit) continue;

            //래그돌처럼 한 캐릭터에 여러 콜라이더가 있을 수 있음
            CharacterBase other = hit.GetComponentInParent<CharacterBase>();
            if (!other || other == Owner) continue;
            if (!other.IsAlive) continue;

            //적은 겹침 보정 대상에서 제거
            if (other.Team != Owner.Team) continue;

            //같은 캐릭터의 여러 콜라이더 중복 처리 방지
            if (!separationNeighbors.Add(other)) continue;

            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;

            float wantDistance = (Owner.Status.colliderRadius + other.Status.colliderRadius) * separationDistanceRatio;
            float distanceSqr = away.sqrMagnitude;

            if (distanceSqr >= wantDistance * wantDistance) continue;

            //중심이 거의 동일하면 방향을 계산하기 어려워 InstanceID를 이용해 갈라지게 만든다
            if(distanceSqr <= 0.001f)
            {
                float sideSign = Owner.GetInstanceID() < other.GetInstanceID() ? 1f : -1f;
                separation += rightDirection * sideSign * wantDistance;
                continue;
            }

            float distance = Mathf.Sqrt(distanceSqr);
            float overlap = wantDistance - distance;
            away /= distance;

            //앞뒤 성분 제거하고 좌우 성분만 사용
            Vector3 lateral = away - Vector3.Project(away, pathDirection);
            if(lateral.sqrMagnitude <= 0.001f)
            {
                // 유닛이 정확히 일렬로 겹친 경우 좌우를 결정한다.
                float sideSign = Owner.GetInstanceID() < other.GetInstanceID() ? -1f : 1f;
                lateral = rightDirection * sideSign;
            }
            else
            {
                lateral.Normalize();
            }
            separation += lateral * overlap;
        }

        if (separation.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float separationSpeedLimit = navAgent.isStopped ? maxSeparationSpeed * 0.25f : maxSeparationSpeed;
        Vector3 separationVelocity = Vector3.ClampMagnitude(separation * separationStrenth, separationSpeedLimit);

        //Transform을 통해 움직이는게 아닌 NavmeshAgent에 상대 이동을 적용
        navAgent.Move(separationVelocity * deltaTime);
        Debug.DrawRay(transform.position + Vector3.up, separationVelocity, Color.cyan);

    }

    // 방향 기반 물리 이동은 LegacyPhysicsMovementModule.cs.txt에 보관한다.
    public void MoveToDirection(Vector3 direction) { }

    public void StopMovement()
    {
        if (!navAgent || !navAgent.isOnNavMesh) return;
        if (navAgent.isStopped) return;

        navAgent.isStopped = true;
        navAgent.ResetPath();
    }

    public void Move(Vector3 direction) { }

    public void StopAllMovementByFaint()
    {
        StopMovement();
        GameManager.OnPhysicsCharacter -= MovementUpdate;

        if (navAgent) navAgent.enabled = false;
    }
}
