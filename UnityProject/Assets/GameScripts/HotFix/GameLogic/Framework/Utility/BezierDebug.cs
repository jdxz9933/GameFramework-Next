using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.HotUpdate;

public class BezierDebug : MonoBehaviour {
    public List<GameObject> Points = new List<GameObject>();

    public float radius = 1;

    // Start is called before the first frame update
    void Start() {
    }

    // Update is called once per frame
    void Update() {
    }

    private void OnDrawGizmos() {
        if (Points.Count < 2) {
            return;
        }

        for (int i = 0; i < Points.Count - 1; i++) {
            Gizmos.DrawLine(Points[i].transform.position, Points[i + 1].transform.position);
        }

        Vector3[] paths = new Vector3[Points.Count];
        for (int i = 0; i < Points.Count; i++) {
            paths[i] = Points[i].transform.position;
        }
        for (float t = 0; t < 1; t += 0.01f) {
            Gizmos.DrawSphere(BezierUtility.GetPoint(paths, t), radius);
        }

        // if (Points.Count == 3) {
        //     for (float t = 0; t < 1; t += 0.01f) {
        //         Gizmos.DrawSphere(
        //             BezierUtility.GetBezierPoint(Points[0].transform.position, Points[1].transform.position,
        //                 Points[2].transform.position, t), 0.1f);
        //     }
        // } else if (Points.Count == 4) {
        //     for (float t = 0; t < 1; t += 0.01f) {
        //         Gizmos.DrawSphere(
        //             BezierUtility.GetBezierPoint(Points[0].transform.position, Points[1].transform.position,
        //                 Points[2].transform.position, Points[3].transform.position, t), 0.1f);
        //     }
        // }
    }
}