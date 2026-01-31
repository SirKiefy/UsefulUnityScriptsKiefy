using System;
using System.Collections.Generic;

namespace UsefulScripts.Events
{
    /// <summary>
    /// Type-safe event system for decoupled communication between game components.
    /// </summary>
    public static class EventManager
    {
        private static Dictionary<Type, Delegate> eventTable = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Subscribe to an event
        /// </summary>
        public static void Subscribe<T>(Action<T> listener) where T : struct
        {
            Type eventType = typeof(T);
            
            if (eventTable.TryGetValue(eventType, out var existingDelegate))
            {
                eventTable[eventType] = Delegate.Combine(existingDelegate, listener);
            }
            else
            {
                eventTable[eventType] = listener;
            }
        }

        /// <summary>
        /// Unsubscribe from an event
        /// </summary>
        public static void Unsubscribe<T>(Action<T> listener) where T : struct
        {
            Type eventType = typeof(T);
            
            if (eventTable.TryGetValue(eventType, out var existingDelegate))
            {
                var newDelegate = Delegate.Remove(existingDelegate, listener);
                if (newDelegate == null)
                {
                    eventTable.Remove(eventType);
                }
                else
                {
                    eventTable[eventType] = newDelegate;
                }
            }
        }

        /// <summary>
        /// Trigger an event
        /// </summary>
        public static void Trigger<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);
            
            if (eventTable.TryGetValue(eventType, out var existingDelegate))
            {
                (existingDelegate as Action<T>)?.Invoke(eventData);
            }
        }

        /// <summary>
        /// Trigger an event with default data
        /// </summary>
        public static void Trigger<T>() where T : struct
        {
            Trigger(default(T));
        }

        /// <summary>
        /// Clear all subscribers for a specific event
        /// </summary>
        public static void ClearEvent<T>() where T : struct
        {
            eventTable.Remove(typeof(T));
        }

        /// <summary>
        /// Clear all events
        /// </summary>
        public static void ClearAll()
        {
            eventTable.Clear();
        }

        /// <summary>
        /// Check if an event has subscribers
        /// </summary>
        public static bool HasSubscribers<T>() where T : struct
        {
            return eventTable.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Get subscriber count for an event
        /// </summary>
        public static int GetSubscriberCount<T>() where T : struct
        {
            if (eventTable.TryGetValue(typeof(T), out var existingDelegate))
            {
                return existingDelegate.GetInvocationList().Length;
            }
            return 0;
        }
    }

    /// <summary>
    /// Simple event manager using string keys
    /// </summary>
    public static class SimpleEventManager
    {
        private static Dictionary<string, Action> events = new Dictionary<string, Action>();
        private static Dictionary<string, Action<object>> eventsWithData = new Dictionary<string, Action<object>>();

        /// <summary>
        /// Subscribe to an event
        /// </summary>
        public static void Subscribe(string eventName, Action listener)
        {
            if (events.TryGetValue(eventName, out var existingAction))
            {
                events[eventName] = existingAction + listener;
            }
            else
            {
                events[eventName] = listener;
            }
        }

        /// <summary>
        /// Subscribe to an event with data
        /// </summary>
        public static void Subscribe(string eventName, Action<object> listener)
        {
            if (eventsWithData.TryGetValue(eventName, out var existingAction))
            {
                eventsWithData[eventName] = existingAction + listener;
            }
            else
            {
                eventsWithData[eventName] = listener;
            }
        }

        /// <summary>
        /// Unsubscribe from an event
        /// </summary>
        public static void Unsubscribe(string eventName, Action listener)
        {
            if (events.TryGetValue(eventName, out var existingAction))
            {
                events[eventName] = existingAction - listener;
            }
        }

        /// <summary>
        /// Unsubscribe from an event with data
        /// </summary>
        public static void Unsubscribe(string eventName, Action<object> listener)
        {
            if (eventsWithData.TryGetValue(eventName, out var existingAction))
            {
                eventsWithData[eventName] = existingAction - listener;
            }
        }

        /// <summary>
        /// Trigger an event
        /// </summary>
        public static void Trigger(string eventName)
        {
            if (events.TryGetValue(eventName, out var action))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Trigger an event with data
        /// </summary>
        public static void Trigger(string eventName, object data)
        {
            if (eventsWithData.TryGetValue(eventName, out var action))
            {
                action?.Invoke(data);
            }
        }

        /// <summary>
        /// Clear all events
        /// </summary>
        public static void ClearAll()
        {
            events.Clear();
            eventsWithData.Clear();
        }
    }
}
