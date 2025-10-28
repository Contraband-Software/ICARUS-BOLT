using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helpers
{
    public class Drawing : MonoBehaviour
    {
        public static void DebugDrawBox(Vector3 origin, Vector3 size, Vector3 direction, float distance, Quaternion orientation, Color color)
        {
            Vector3 halfExtents = size / 2f;

            // Calculate the end position
            Vector3 endPos = origin + direction.normalized * distance;

            // Get the vertices for both start and end boxes
            Vector3[] vertices = GetBoxVertices(origin, halfExtents, orientation);

            // Top
            Debug.DrawLine(vertices[0], vertices[1], color);
            Debug.DrawLine(vertices[0], vertices[2], color);
            Debug.DrawLine(vertices[1], vertices[3], color);
            Debug.DrawLine(vertices[2], vertices[3], color);

            //Bottom
            Debug.DrawLine(vertices[4], vertices[5], color);
            Debug.DrawLine(vertices[4], vertices[6], color);
            Debug.DrawLine(vertices[5], vertices[7], color);
            Debug.DrawLine(vertices[6], vertices[7], color);

            //Fill In
            Debug.DrawLine(vertices[0], vertices[4], color);
            Debug.DrawLine(vertices[1], vertices[5], color);
            Debug.DrawLine(vertices[2], vertices[6], color);
            Debug.DrawLine(vertices[3], vertices[7], color);
        }
        static Vector3[] GetBoxVertices(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            Vector3[] vertices = new Vector3[8];

            // Top vertices
            vertices[0] = center + orientation * new Vector3(halfExtents.x, halfExtents.y, halfExtents.z);
            vertices[1] = center + orientation * new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
            vertices[2] = center + orientation * new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z);
            vertices[3] = center + orientation * new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);

            // Bottom vertices
            vertices[4] = center + orientation * new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z);
            vertices[5] = center + orientation * new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);
            vertices[6] = center + orientation * new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z);
            vertices[7] = center + orientation * new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);

            return vertices;
        }

        public static void DrawLineFromPoints(List<Vector3> points, Color color)
        {
            if (points.Count < 2) return; // Not enough points to draw lines

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[i + 1];

                Debug.DrawLine(start, end, color);
            }
        }

        public static void DrawCrossOnXZPlane(Vector3 center, float size, Color color)
        {
            Vector3 offset = new Vector3(size / 2f, 0f, size / 2f);

            // Diagonal 1:
            Debug.DrawLine(center - offset, center + offset, color);

            // Diagonal 2:
            Debug.DrawLine(center + new Vector3(-offset.x, 0f, offset.z), center + new Vector3(offset.x, 0f, -offset.z), color);
        }

        /// <summary>
        /// Draws a rough approximation of a capsule cast/overlap. Assumes that bottom and top point have same X & Z.
        /// </summary>
        /// <param name="bottomPoint">The bottom of the cylinder part of the capsule</param>
        /// <param name="topPoint">The top of the cylinder part of the capsule</param>
        /// <param name=""></param>
        public static void DebugDrawCapsuleApprox(Vector3 bottomPoint, Vector3 topPoint, float radius, Quaternion orientation, Color color)
        {
            Quaternion rot = orientation * Quaternion.Euler(0f, 45f, 0f);
            Vector3 center = (bottomPoint + topPoint) / 2f;
            Vector3 size = new Vector3(radius * 2f / 1.414f, (topPoint.y - bottomPoint.y) + 2 * radius, radius * 2f / 1.414f);

            DebugDrawBox(center, size, Vector3.zero, 0f, rot, color);

        }
    }
}
