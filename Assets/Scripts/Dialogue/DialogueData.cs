using UnityEngine;
using System;
using System.Collections.Generic;

namespace UsefulScripts.Dialogue
{
    /// <summary>
    /// Dialogue data container
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Useful Scripts/Dialogue")]
    public class DialogueData : ScriptableObject
    {
        public string dialogueId;
        public List<DialogueNode> nodes = new List<DialogueNode>();

        public DialogueNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count)
            {
                return nodes[index];
            }
            return null;
        }

        public DialogueNode GetNodeById(string nodeId)
        {
            return nodes.Find(n => n.nodeId == nodeId);
        }
    }

    [Serializable]
    public class DialogueNode
    {
        public string nodeId;
        public string speakerName;
        public Sprite speakerPortrait;
        [TextArea(3, 10)]
        public string dialogueText;
        public AudioClip voiceClip;
        public float typingSpeed = 0.05f;
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        public int nextNodeIndex = -1; // -1 = end, or use choices
        public bool autoAdvance = false;
        public float autoAdvanceDelay = 2f;
        public DialogueEvent[] events;
    }

    [Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public int nextNodeIndex;
        public string requiredFlag; // Optional: only show if this flag is true
        public bool setFlag; // Set a flag when chosen
        public string flagToSet;
    }

    [Serializable]
    public class DialogueEvent
    {
        public enum EventType
        {
            SetFlag,
            ClearFlag,
            TriggerEvent,
            PlaySound,
            SetVariable
        }

        public EventType type;
        public string eventData;
    }
}
