using UnityEngine;
using UnityEngine.Events;

namespace Fate.Systems.Utilities
{
    public class UnityFunctionRelay : MonoBehaviour
    {
        [SerializeField] private bool relayAwake;
        [SerializeField] private bool relayStart;
        [SerializeField] private bool relayUpdate;
        [SerializeField] private bool relayFixedUpdate;

        [SerializeField] private UnityEvent onAwake;
        [SerializeField] private UnityEvent onStart;
        [SerializeField] private UnityEvent<float> onUpdate;
        [SerializeField] private UnityEvent<float> onFixedUpdate;

        void Awake()
        {
            if (relayAwake)
            {
                onAwake?.Invoke();
            }
        }

        void Start()
        {
            if (relayStart)
            {
                onStart?.Invoke();
                Debug.Log("Start");
            }
        }

        void Update()
        {
            if (relayUpdate)
            {
                onUpdate?.Invoke(Time.deltaTime);
            }
        }

        void FixedUpdate()
        {
            if (relayFixedUpdate)
            {
                onFixedUpdate?.Invoke(Time.deltaTime);
            }
        }
    }
}
