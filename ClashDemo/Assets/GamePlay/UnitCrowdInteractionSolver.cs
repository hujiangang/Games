using System.Collections.Generic;
using UnityEngine;

internal readonly struct UnitCrowdPriorityResult
{
    public readonly Vector3 Offset;
    public readonly float LaneHoldPressure;

    public UnitCrowdPriorityResult(Vector3 offset, float laneHoldPressure)
    {
        Offset = offset;
        LaneHoldPressure = laneHoldPressure;
    }
}

internal static class UnitCrowdInteractionTuning
{
    public const float PushContactSlack = 0.02f;
    public const float PushIntentFloor = 0.35f;
    public const float PushOffsetLerp = 14f;
    public const float PushOffsetRecoveryLerp = 10f;
    public const float MaxPushOffsetFactor = 0.75f;
    public const float PushStrengthScale = 2.25f;

    public const float PriorityOffsetLerp = 12f;
    public const float PriorityOffsetRecoveryLerp = 9f;
    public const float MaxPriorityOffsetFactor = 0.6f;
    public const float YieldBackoffFactor = 0.45f;
    public const float YieldSideFactor = 0.8f;
    public const float HeavyLaneHoldFactor = 1.25f;
    public const float FacingLaneHoldFactor = 1.6f;
}

internal static class UnitCrowdInteractionSolver
{
    public static Vector3 ComputeMassPushOffset(UnitCrowdSnapshot self, IReadOnlyList<UnitAgent> activeAgents)
    {
        Vector3 accumulatedPush = Vector3.zero;
        float accumulatedStrength = 0f;
        float strongestOverlap = 0f;

        for (int i = 0; i < activeAgents.Count; i++)
        {
            if (!TryGetOtherSnapshot(self.Agent, activeAgents[i], out UnitCrowdSnapshot other))
                continue;

            Vector3 delta = self.PushSamplePosition - other.PushSamplePosition;
            delta.y = 0f;

            float distance = delta.magnitude;
            if (distance <= 0.0001f)
            {
                delta = GetFallbackDirection(self.Intent) * 0.01f;
                distance = delta.magnitude;
            }

            float minDistance = self.Radius + other.Radius - UnitCrowdInteractionTuning.PushContactSlack;
            float overlap = minDistance - distance;
            if (overlap <= 0f)
                continue;

            Vector3 normal = delta / distance;
            float otherIntentTowardSelf = Mathf.Max(0f, Vector3.Dot(other.Intent, normal));
            float selfIntentTowardOther = Mathf.Max(0f, Vector3.Dot(self.Intent, -normal));
            float contactIntent = Mathf.Max(otherIntentTowardSelf, selfIntentTowardOther * 0.75f);

            float pressure = other.Mass * Mathf.Max(UnitCrowdInteractionTuning.PushIntentFloor, contactIntent) * overlap;
            accumulatedPush += normal * pressure;
            accumulatedStrength += pressure;
            strongestOverlap = Mathf.Max(strongestOverlap, overlap);
        }

        if (accumulatedPush.sqrMagnitude <= 0.0001f || strongestOverlap <= 0f)
            return Vector3.zero;

        float resistance = self.Mass * strongestOverlap;
        float netStrength = accumulatedStrength - resistance;
        if (netStrength <= 0f)
            return Vector3.zero;

        float maxOffset = self.Radius * UnitCrowdInteractionTuning.MaxPushOffsetFactor;
        float offsetMagnitude = Mathf.Min(maxOffset, netStrength * UnitCrowdInteractionTuning.PushStrengthScale);
        Vector3 desiredOffset = accumulatedPush.normalized * offsetMagnitude;
        desiredOffset.y = 0f;
        return desiredOffset;
    }

    public static UnitCrowdPriorityResult ComputePriority(UnitCrowdSnapshot self, Vector3 lastCrowdPosition, bool hasLastCrowdPosition, IReadOnlyList<UnitAgent> activeAgents)
    {
        if (self.Intent.sqrMagnitude <= 0.0001f)
            return new UnitCrowdPriorityResult(Vector3.zero, 0f);

        Vector3 accumulatedYield = Vector3.zero;
        float yieldStrength = 0f;
        float laneHoldPressure = 0f;

        for (int i = 0; i < activeAgents.Count; i++)
        {
            if (!TryGetOtherSnapshot(self.Agent, activeAgents[i], out UnitCrowdSnapshot other))
                continue;

            Vector3 delta = self.PushSamplePosition - other.PushSamplePosition;
            delta.y = 0f;

            float distance = delta.magnitude;
            if (distance <= 0.0001f)
            {
                delta = GetFallbackDirection(self.Intent) * 0.01f;
                distance = delta.magnitude;
            }

            float minDistance = self.Radius + other.Radius - UnitCrowdInteractionTuning.PushContactSlack;
            float interactionDistance = minDistance + Mathf.Max(0.08f, Mathf.Min(self.Radius, other.Radius) * 0.5f);
            if (distance > interactionDistance)
                continue;

            Vector3 normal = delta / distance;
            float otherApproach = Mathf.Max(0f, Vector3.Dot(other.Intent, normal));
            float selfApproach = Mathf.Max(0f, Vector3.Dot(self.Intent, -normal));
            float contactIntent = Mathf.Max(otherApproach, selfApproach);
            if (contactIntent <= 0.05f)
                continue;

            float proximity = 1f - Mathf.InverseLerp(interactionDistance, minDistance, distance);
            if (other.Mass > self.Mass * 1.05f)
            {
                float massGap = Mathf.Clamp01((other.Mass - self.Mass) / other.Mass);
                Vector3 side = Vector3.Cross(Vector3.up, self.Intent);
                if (side.sqrMagnitude <= 0.0001f)
                    side = Vector3.Cross(Vector3.up, normal);

                side.Normalize();
                if (Vector3.Dot(side, normal) < 0f)
                    side = -side;

                Vector3 yieldDirection = (normal * UnitCrowdInteractionTuning.YieldBackoffFactor + side * UnitCrowdInteractionTuning.YieldSideFactor).normalized;
                float strength = proximity * contactIntent * massGap;
                accumulatedYield += yieldDirection * strength;
                yieldStrength += strength;
            }
            else if (self.Mass > other.Mass * 1.1f)
            {
                float dominance = Mathf.Clamp01((self.Mass - other.Mass) / self.Mass);
                laneHoldPressure += proximity * Mathf.Max(otherApproach, selfApproach * 0.5f) * dominance;
            }
        }

        Vector3 desiredOffset = Vector3.zero;
        if (yieldStrength > 0.0001f)
        {
            float yieldMagnitude = Mathf.Min(self.Radius * UnitCrowdInteractionTuning.MaxPriorityOffsetFactor, yieldStrength * self.Radius);
            desiredOffset += accumulatedYield.normalized * yieldMagnitude;
        }

        if (laneHoldPressure > 0.0001f && hasLastCrowdPosition)
        {
            Vector3 crowdDrift = self.CrowdPosition - lastCrowdPosition;
            crowdDrift.y = 0f;

            float forwardDrift = Vector3.Dot(crowdDrift, self.Intent);
            Vector3 lateralDrift = crowdDrift - self.Intent * forwardDrift;
            float laneHoldStrength = Mathf.Clamp01(laneHoldPressure * UnitCrowdInteractionTuning.HeavyLaneHoldFactor);

            desiredOffset -= lateralDrift * laneHoldStrength;
            if (forwardDrift < 0f)
                desiredOffset += self.Intent * (-forwardDrift) * laneHoldStrength;
        }

        float maxOffset = self.Radius * UnitCrowdInteractionTuning.MaxPriorityOffsetFactor;
        if (desiredOffset.sqrMagnitude > maxOffset * maxOffset)
            desiredOffset = desiredOffset.normalized * maxOffset;

        desiredOffset.y = 0f;
        return new UnitCrowdPriorityResult(desiredOffset, laneHoldPressure);
    }

    public static Vector3 ResolveFacingDirection(UnitCrowdSnapshot self, Vector3 crowdVelocity, float laneHoldPressure)
    {
        crowdVelocity.y = 0f;
        Vector3 intent = self.Intent;

        if (crowdVelocity.sqrMagnitude <= 0.0001f)
            return intent;

        if (laneHoldPressure <= 0.0001f || intent.sqrMagnitude <= 0.0001f)
            return crowdVelocity.normalized;

        float facingHold = Mathf.Clamp01(laneHoldPressure * UnitCrowdInteractionTuning.FacingLaneHoldFactor);
        Vector3 stableFacing = Vector3.Slerp(crowdVelocity.normalized, intent, facingHold);
        return stableFacing.sqrMagnitude > 0.0001f ? stableFacing.normalized : intent;
    }

    private static bool TryGetOtherSnapshot(UnitAgent self, UnitAgent otherAgent, out UnitCrowdSnapshot other)
    {
        other = default;
        if (otherAgent == null || otherAgent == self)
            return false;

        return otherAgent.TryBuildCrowdSnapshot(out other);
    }

    private static Vector3 GetFallbackDirection(Vector3 intent)
    {
        if (intent.sqrMagnitude > 0.0001f)
            return Vector3.Cross(Vector3.up, intent).normalized;

        return new Vector3(1f, 0f, 0f);
    }
}
