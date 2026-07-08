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
        if (controller != null)
            controller.NotifyTriggerEnter(other);
    }
}
