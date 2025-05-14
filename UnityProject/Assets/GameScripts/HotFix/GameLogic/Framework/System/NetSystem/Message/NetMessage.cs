using System.Collections;
using System.Collections.Generic;
using Loxodon.Framework.Messaging;
using UnityEngine;

namespace GameLogic {

    public class ReConnectMessage : MessageBase {
        public ReConnectMessage(object sender) : base(sender) { }
    }

}