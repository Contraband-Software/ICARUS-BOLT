using System.Collections.Generic;
using UnityEngine;

namespace Level
{
    public class LevelBlock : MonoBehaviour
    {
        [field: SerializeField] public List<Transform> decorationSpawnPoints { get; private set; }
        [field: SerializeField] public Transform derelictLocation { get; private set; }
    }
}