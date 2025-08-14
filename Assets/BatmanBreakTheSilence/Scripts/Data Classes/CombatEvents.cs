using System;
using UnityEngine;
using UnityEngine.Events;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Конфигурация событий для боевой системы.
    /// Определяет события, связанные с отслеживанием и управлением руками в Batman: Break The Silence.
    /// </summary>
    [Serializable]
    public class CombatEvents
    {
        #region Serialized Fields

        [Header("События системы")]
        [SerializeField, Tooltip("Событие, вызываемое при начале отслеживания")]
        public UnityEvent<HandType> OnTrackingStarted = new UnityEvent<HandType>(); // Событие для старта отслеживания курсора

        [SerializeField, Tooltip("Событие, вызываемое при остановке отслеживания")]
        public UnityEvent OnTrackingStopped = new UnityEvent(); // Событие для остановки отслеживания курсора

        [SerializeField, Tooltip("Событие, вызываемое при активации руки")]
        public UnityEvent<HandType> OnHandActivated = new UnityEvent<HandType>(); // Событие для активации руки

        [SerializeField, Tooltip("Событие, вызываемое при деактивации руки")]
        public UnityEvent<HandType> OnHandDeactivated = new UnityEvent<HandType>(); // Событие для деактивации руки

        #endregion
    }
}