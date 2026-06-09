using UnityEngine;

internal readonly struct UnitCrowdSnapshot
{
    public readonly UnitAgent Agent;
    public readonly Vector3 CrowdPosition;
    public readonly Vector3 PushSamplePosition;
    public readonly Vector3 DisplayPosition;
    public readonly Vector3 Intent;
    public readonly float Radius;
    public readonly float Mass;

    public UnitCrowdSnapshot(
        UnitAgent agent,
        Vector3 crowdPosition,
        Vector3 pushSamplePosition,
        Vector3 displayPosition,
        Vector3 intent,
        float radius,
        float mass)
    {
        Agent = agent;
        CrowdPosition = crowdPosition;
        PushSamplePosition = pushSamplePosition;
        DisplayPosition = displayPosition;
        Intent = intent;
        Radius = radius;
        Mass = mass;
    }
}
