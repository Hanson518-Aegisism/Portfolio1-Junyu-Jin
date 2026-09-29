using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChasePlayer : MonoBehaviour
{
    [SerializeField] float repathInterval = 0.25f;
    [SerializeField] float maxChaseDistance = 80f;
    [SerializeField] float eyeHeight = 1.2f;
    [SerializeField] float playerEyeHeight = 1.0f;
    [SerializeField] LayerMask losMask = ~0;

    NavMeshAgent agent;
    Transform player;
    float nextRepathTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
            player = playerGo.transform;
    }

    void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
            return;

        if (Time.time < nextRepathTime)
            return;

        nextRepathTime = Time.time + repathInterval;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > maxChaseDistance || !CanSeePlayer())
        {
            if (agent.hasPath)
                agent.ResetPath();
            return;
        }

        agent.SetDestination(player.position);
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
