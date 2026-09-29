using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChasePlayer : MonoBehaviour
{
    enum State
    {
        Idle,
        Chase,
        Investigate
    }

    [Header("Detection")]
    [SerializeField] float repathInterval = 0.2f;
    [SerializeField] float maxChaseDistance = 80f;
    [SerializeField] float eyeHeight = 1.2f;
    [SerializeField] float playerEyeHeight = 1.0f;
    [SerializeField] LayerMask losMask = ~0;

    [Header("Catch")]
    [SerializeField] float catchDistance = 1.6f;

    [Header("Lost Sight")]
    [SerializeField] float arriveThreshold = 1.25f;
    [SerializeField] float giveUpDelay = 3f;
    [SerializeField] float investigatePause = 1.25f;

    [Header("Feel")]
    [SerializeField] float walkSpeed = 2.2f;
    [SerializeField] float chaseSpeed = 4.5f;
    [SerializeField] float speedLerp = 6f;
    [SerializeField] float idleAngularSpeed = 120f;
    [SerializeField] float chaseAngularSpeed = 720f;
    [SerializeField] float chaseAcceleration = 18f;
    [SerializeField] float idleAcceleration = 8f;

    NavMeshAgent agent;
    Transform player;
    PlayerDeath playerDeath;
    State state = State.Idle;
    Vector3 lastKnownPosition;
    bool hasLastKnown;
    float nextRepathTime;
    float giveUpAt;
    float investigatePauseUntil;
    float targetSpeed;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = Mathf.Min(agent.stoppingDistance, Mathf.Max(0.2f, catchDistance * 0.35f));
        targetSpeed = walkSpeed;
        ApplyFeel(false);
    }

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            player = playerGo.transform;
            playerDeath = playerGo.GetComponent<PlayerDeath>();
        }
    }

    void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
            return;

        if (playerDeath != null && playerDeath.IsDead)
        {
            EnterIdle();
            return;
        }

        bool seesPlayer = IsPlayerInRange() && CanSeePlayer();
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (seesPlayer)
        {
            lastKnownPosition = player.position;
            hasLastKnown = true;

            if (distToPlayer <= catchDistance)
            {
                CatchPlayer();
                return;
            }

            if (state != State.Chase)
                EnterChase();
        }
        else if (state == State.Chase)
        {
            EnterInvestigate();
        }

        switch (state)
        {
            case State.Idle:
                TickIdle();
                break;
            case State.Chase:
                TickChase();
                break;
            case State.Investigate:
                TickInvestigate();
                break;
        }

        agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, speedLerp * Time.deltaTime);
    }

    void TickIdle()
    {
        targetSpeed = walkSpeed;
        ApplyFeel(false);

        if (agent.hasPath)
            agent.ResetPath();
    }

    void TickChase()
    {
        targetSpeed = chaseSpeed;
        ApplyFeel(true);

        if (Time.time < nextRepathTime)
            return;

        nextRepathTime = Time.time + repathInterval;
        agent.SetDestination(player.position);
    }

    void TickInvestigate()
    {
        targetSpeed = walkSpeed;
        ApplyFeel(false);

        if (Time.time >= giveUpAt)
        {
            EnterIdle();
            return;
        }

        if (!hasLastKnown)
        {
            EnterIdle();
            return;
        }

        if (Time.time < nextRepathTime)
            return;

        nextRepathTime = Time.time + repathInterval;

        float distToLast = Vector3.Distance(transform.position, lastKnownPosition);
        if (distToLast > arriveThreshold)
        {
            agent.SetDestination(lastKnownPosition);
            investigatePauseUntil = Time.time + investigatePause;
            return;
        }

        if (agent.hasPath)
            agent.ResetPath();

        if (Time.time >= investigatePauseUntil)
            EnterIdle();
    }

    void EnterChase()
    {
        state = State.Chase;
        nextRepathTime = 0f;
        ApplyFeel(true);
    }

    void EnterInvestigate()
    {
        state = State.Investigate;
        giveUpAt = Time.time + giveUpDelay;
        investigatePauseUntil = Time.time + investigatePause;
        nextRepathTime = 0f;

        if (hasLastKnown)
            agent.SetDestination(lastKnownPosition);
    }

    void EnterIdle()
    {
        state = State.Idle;
        hasLastKnown = false;
        if (agent.hasPath)
            agent.ResetPath();
        ApplyFeel(false);
        targetSpeed = walkSpeed;
    }

    void CatchPlayer()
    {
        if (playerDeath == null || playerDeath.IsDead)
            return;

        agent.ResetPath();
        EnterIdle();
        playerDeath.Kill();
    }

    void ApplyFeel(bool chasing)
    {
        agent.angularSpeed = chasing ? chaseAngularSpeed : idleAngularSpeed;
        agent.acceleration = chasing ? chaseAcceleration : idleAcceleration;
    }

    bool IsPlayerInRange()
    {
        return Vector3.Distance(transform.position, player.position) <= maxChaseDistance;
    }

    bool CanSeePlayer()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * playerEyeHeight;
        Vector3 delta = target - origin;
        float dist = delta.magnitude;
        if (dist < 0.01f)
            return true;

        var hits = Physics.RaycastAll(origin, delta / dist, dist, losMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            if (t == transform || t.IsChildOf(transform))
                continue;

            return t == player || t.IsChildOf(player);
        }

        return true;
    }
}
