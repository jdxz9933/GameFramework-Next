using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.HotUpdate {
    public class DynamicUtils : MonoBehaviour {
        public static Dictionary<string, DynamicUtils> Dynamics => dynamics;
        private static Dictionary<string, DynamicUtils> dynamics;
        [SerializeField] private List<Transform> ObjReferences = new List<Transform>();

        public Dictionary<string, Transform> ObjMap;

        private void Awake() {
            dynamics ??= new Dictionary<string, DynamicUtils>();
            var sceneName = gameObject.scene.name;
            if (!dynamics.ContainsKey(sceneName))
                dynamics.Add(sceneName, this);
            else
                Debug.Log("sceneName 已经存在:" + sceneName);

            ObjMap = new Dictionary<string, Transform>();

            foreach (Transform reference in ObjReferences) {
                ObjMap.Add(reference.name, reference);
            }
        }

        private void OnDestroy() {
            var sceneName = gameObject.scene.name;
            if (dynamics.ContainsKey(sceneName))
                dynamics.Remove(sceneName);
        }

        [Button("收集引用")]
        private void CollectPoints() {
            ObjReferences.Clear();
            for (int i = 0; i < transform.childCount; i++) {
                ObjReferences.Add(transform.GetChild(i));
            }
        }
    }
}