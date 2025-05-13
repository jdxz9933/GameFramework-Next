using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic {
    public class SaveDataAgent {
        private object data;

        public bool IsDirty { get; set; }

        public object Data {
            get => data;
            set { data = value; }
        }

        public SaveDataAgent(object data) {
            this.data = data;
            IsDirty = false;
        }
    }
}