using UnityEngine;
using UnityEngine.AI;

/// <summary>Connects the visual's animation to the existing enemy's navigation.</summary>
[DisallowMultipleComponent]
public sealed class TombSoulAnimationDriver : MonoBehaviour
{
    static readonly int Speed = Animator.StringToHash("Speed");
    static readonly int CrawlRate = Animator.StringToHash("CrawlRate");
    [Tooltip("该移动速度对应正常爬行动画速度。")]
    [Min(0.1f)] [SerializeField] float referenceMoveSpeed = 2.2f;
    [SerializeField] Vector2 playbackRateRange = new Vector2(0.65f, 1.8f);
    Animator animator;
    NavMeshAgent agent;

    void OnEnable()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponentInParent<NavMeshAgent>();
    }

    void OnTransformParentChanged()
    {
        agent = GetComponentInParent<NavMeshAgent>();
    }

    void LateUpdate()
    {
        if (animator == null) return;
        float speed = agent != null && agent.enabled && agent.isOnNavMesh
            ? new Vector2(agent.velocity.x, agent.velocity.z).magnitude : 0f;
        animator.SetFloat(Speed, speed, 0.12f, Time.deltaTime);
        float rate = Mathf.Clamp(speed / Mathf.Max(0.1f, referenceMoveSpeed), playbackRateRange.x, playbackRateRange.y);
        animator.SetFloat(CrawlRate, rate, 0.15f, Time.deltaTime);
    }
}
