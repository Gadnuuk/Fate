using System;
using UnityEngine;

namespace Fate.Systems.Boot
{
    /// <summary>
    /// Drop into a boot scene to make BootSequenceManager wait before loading the next
    /// scene. Fires Finished on Start. HasFinished lets late subscribers (anything that
    /// looks for this component after Start has already run) know they missed the event.
    /// </summary>
    public class BootFinishedEventComponent : MonoBehaviour
    {
        public event Action Finished;

        public bool HasFinished { get; private set; }

        public void FinishBoot()
        {
            HasFinished = true;
            Finished?.Invoke();
        }
    }
}
