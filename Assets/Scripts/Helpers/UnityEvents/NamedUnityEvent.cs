using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Helpers
{
    [System.Serializable]
    public class NamedUnityEvent
    {
        public string name;
        public UnityEvent unityEvent = new UnityEvent();
    }
}
