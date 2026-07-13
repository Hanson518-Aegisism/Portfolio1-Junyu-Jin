using UnityEngine;

/// <summary>
/// Forwards trigger events from a child collider to CollapseTrapController on a parent.
/// A kinematic Rigidbody is required so Unity sends trigger messages to this object
/// when a CharacterController walks through the zone.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class CollapseTrapTrigger : MonoBehaviour
{
    public enum TriggerRole
    {
        ApproachWarning,
        Collapse
    }

    [SerializeField] private TriggerRole role = TriggerRole.Collapse;

    private CollapseTrapController controller;

    private void Awake()
    {
        controller = GetComponentInParent<CollapseTrapController>();

        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (controller == null)
            return;

        if (role == TriggerRole.ApproachWarning)
            controller.NotifyApproachWarningEnter(other);
        else
            controller.NotifyTriggerEnter(other);
    }
}
