using UnityEngine;

[AddComponentMenu("PCB/Bone Collider")]
public class DynamicBoneCollider : MonoBehaviour
{
    public enum ColliderType { Sphere, Capsule, Plane }
    public ColliderType m_ColliderType = ColliderType.Sphere;

    public Vector3 m_Center = Vector3.zero;
    public float m_Radius = 0.5f;
    public float m_Height = 0;

    public float m_PlaneWidth = 1f;
    public float m_PlaneLength = 1f;

    public enum Direction { X, Y, Z }
    public Direction m_Direction = Direction.X;

    public enum Bound { Outside, Inside }
    public Bound m_Bound = Bound.Outside;

    void OnValidate()
    {
        m_Radius = Mathf.Max(m_Radius, 0);
        m_Height = Mathf.Max(m_Height, 0);
        m_PlaneWidth = Mathf.Max(m_PlaneWidth, 0.01f);
        m_PlaneLength = Mathf.Max(m_PlaneLength, 0.01f);
    }

    public void Collide(ref Vector3 particlePosition, float particleRadius)
    {
        switch (m_ColliderType)
        {
            case ColliderType.Sphere:
                HandleSphereCollision(ref particlePosition, particleRadius);
                break;
            case ColliderType.Capsule:
                HandleCapsuleCollision(ref particlePosition, particleRadius);
                break;
            case ColliderType.Plane:
                HandlePlaneCollision(ref particlePosition, particleRadius);
                break;
        }
    }

    private void HandleSphereCollision(ref Vector3 particlePosition, float particleRadius)
    {
        float radius = m_Radius * Mathf.Abs(transform.lossyScale.x);
        Vector3 center = transform.TransformPoint(m_Center);
        if (m_Bound == Bound.Outside)
            OutsideSphere(ref particlePosition, particleRadius, center, radius);
        else
            InsideSphere(ref particlePosition, particleRadius, center, radius);
    }

    private void HandleCapsuleCollision(ref Vector3 particlePosition, float particleRadius)
    {
        float radius = m_Radius * Mathf.Abs(transform.lossyScale.x);
        float h = m_Height * 0.5f - radius;
        Vector3 c0 = m_Center, c1 = m_Center;

        switch (m_Direction)
        {
            case Direction.X: c0.x -= h; c1.x += h; break;
            case Direction.Y: c0.y -= h; c1.y += h; break;
            case Direction.Z: c0.z -= h; c1.z += h; break;
        }

        Vector3 p0 = transform.TransformPoint(c0);
        Vector3 p1 = transform.TransformPoint(c1);

        if (m_Bound == Bound.Outside)
            OutsideCapsule(ref particlePosition, particleRadius, p0, p1, radius);
        else
            InsideCapsule(ref particlePosition, particleRadius, p0, p1, radius);
    }

    private void HandlePlaneCollision(ref Vector3 particlePosition, float particleRadius)
    {
        Vector3 center = transform.TransformPoint(m_Center);
        Vector3 normal = GetDirectionVector();

        Vector3 toParticle = particlePosition - center;
        float distance = Vector3.Dot(toParticle, normal);

        // Project onto tangent space to check bounds
        Vector3 tangent = GetTangent(normal);
        Vector3 bitangent = Vector3.Cross(normal, tangent);

        float xProj = Vector3.Dot(toParticle, tangent);
        float yProj = Vector3.Dot(toParticle, bitangent);

        float halfWidth = m_PlaneWidth * 0.5f;
        float halfLength = m_PlaneLength * 0.5f;

        if (Mathf.Abs(xProj) <= halfWidth && Mathf.Abs(yProj) <= halfLength)
        {
            if (m_Bound == Bound.Outside && distance < particleRadius)
                particlePosition += normal * (particleRadius - distance);
            else if (m_Bound == Bound.Inside && distance > -particleRadius)
                particlePosition -= normal * (distance + particleRadius);
        }
    }

    private Vector3 GetDirectionVector()
    {
        switch (m_Direction)
        {
            case Direction.X: return transform.right;
            case Direction.Y: return transform.up;
            case Direction.Z: return transform.forward;
            default: return Vector3.up;
        }
    }

    private Vector3 GetTangent(Vector3 normal)
    {
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.001f)
            tangent = Vector3.Cross(normal, Vector3.right);
        return tangent.normalized;
    }

    // --- Collision Handling Functions (Sphere & Capsule) ---

    static void OutsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
    {
        float r = sphereRadius + particleRadius;
        Vector3 d = particlePosition - sphereCenter;
        float len2 = d.sqrMagnitude;
        if (len2 > 0 && len2 < r * r)
            particlePosition = sphereCenter + d * (r / Mathf.Sqrt(len2));
    }

    static void InsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
    {
        float r = sphereRadius + particleRadius;
        Vector3 d = particlePosition - sphereCenter;
        float len2 = d.sqrMagnitude;
        if (len2 > r * r)
            particlePosition = sphereCenter + d * (r / Mathf.Sqrt(len2));
    }

    static void OutsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius)
    {
        float r = capsuleRadius + particleRadius;
        Vector3 dir = capsuleP1 - capsuleP0;
        float t = Vector3.Dot(particlePosition - capsuleP0, dir.normalized);
        t = Mathf.Clamp(t, 0, dir.magnitude);
        Vector3 closest = capsuleP0 + dir.normalized * t;
        Vector3 d = particlePosition - closest;
        float len2 = d.sqrMagnitude;
        if (len2 > 0 && len2 < r * r)
            particlePosition = closest + d * (r / Mathf.Sqrt(len2));
    }

    static void InsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius)
    {
        float r = capsuleRadius + particleRadius;
        Vector3 dir = capsuleP1 - capsuleP0;
        float t = Vector3.Dot(particlePosition - capsuleP0, dir.normalized);
        t = Mathf.Clamp(t, 0, dir.magnitude);
        Vector3 closest = capsuleP0 + dir.normalized * t;
        Vector3 d = particlePosition - closest;
        float len2 = d.sqrMagnitude;
        if (len2 > r * r)
            particlePosition = closest + d * (r / Mathf.Sqrt(len2));
    }

    // --- Gizmo Drawing for Visualization ---

    void OnDrawGizmosSelected()
    {
        if (!enabled) return;

        Gizmos.color = (m_Bound == Bound.Outside) ? Color.yellow : Color.magenta;
        float radius = m_Radius * Mathf.Abs(transform.lossyScale.x);
        float h = m_Height * 0.5f - radius;

        switch (m_ColliderType)
        {
            case ColliderType.Sphere:
                Gizmos.DrawWireSphere(transform.TransformPoint(m_Center), radius);
                break;

            case ColliderType.Capsule:
                Vector3 c0 = m_Center, c1 = m_Center;
                switch (m_Direction)
                {
                    case Direction.X: c0.x -= h; c1.x += h; break;
                    case Direction.Y: c0.y -= h; c1.y += h; break;
                    case Direction.Z: c0.z -= h; c1.z += h; break;
                }
                Gizmos.DrawWireSphere(transform.TransformPoint(c0), radius);
                Gizmos.DrawWireSphere(transform.TransformPoint(c1), radius);
                break;

            case ColliderType.Plane:
                Vector3 center = transform.TransformPoint(m_Center);
                Vector3 normal = GetDirectionVector();
                Vector3 tangent = GetTangent(normal);
                Vector3 bitangent = Vector3.Cross(normal, tangent);

                float w = m_PlaneWidth * 0.5f;
                float l = m_PlaneLength * 0.5f;

                Vector3 corner = center - tangent * w - bitangent * l;
                Vector3 right = tangent * m_PlaneWidth;
                Vector3 up = bitangent * m_PlaneLength;

                Gizmos.DrawLine(corner, corner + right);
                Gizmos.DrawLine(corner, corner + up);
                Gizmos.DrawLine(corner + right, corner + right + up);
                Gizmos.DrawLine(corner + up, corner + up + right);
                break;
        }
    }
}
