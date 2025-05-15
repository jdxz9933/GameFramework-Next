using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.HotUpdate {
    public static class BezierUtility {
        private static float GetBezierPoint(float p0, float p1, float p2, float t) {
            float point = 0;
            point = Mathf.Pow(1 - t, 2) * p0 + 2 * t * (1 - t) * p1 + Mathf.Pow(t, 2) * p2;

            // point = (1 - t) * ((1 - t) * p0 + t * p1) + t * ((1 - t) * p1 + t * p2);
            
            return point;
        }

        private static float GetBezierPoint(float p0, float p1, float p2, float p3, float t) {
            float point = 0;
            point = Mathf.Pow(1 - t, 3) * p0 +
                    3 * t * Mathf.Pow(1 - t, 2) * p1 +
                    3 * Mathf.Pow(t, 2) * (1 - t) * p2 +
                    Mathf.Pow(t, 3) * p3;
            return point;
        }

        //二次贝塞尔曲线
        public static Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t) {
            Vector3 point = Vector3.zero;
            point.x = GetBezierPoint(p0.x, p1.x, p2.x, t);
            point.y = GetBezierPoint(p0.x, p1.x, p2.x, t);
            return point;
        }

        //三次贝塞尔曲线  
        public static Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            Vector3 point = Vector3.zero;
            point.x = GetBezierPoint(p0.x, p1.x, p2.x, p3.x, t);
            point.y = GetBezierPoint(p0.y, p1.y, p2.y, p3.y, t);
            return point;
        }
        
        public static Vector3 GetPoint(Vector3[] points, float t)
        {
            t = Mathf.Clamp01(t);
            int count = points.Length;
            float oneMinusT = 1f - t;
            Vector3[] tempPoints = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                tempPoints[i] = points[i];
            }
            for (int i = 1; i < count; i++)
            {
                for (int j = 0; j < count - i; j++)
                {
                    tempPoints[j] = oneMinusT * tempPoints[j] + t * tempPoints[j + 1];
                }
            }
            return tempPoints[0];
        }
        
    }
}