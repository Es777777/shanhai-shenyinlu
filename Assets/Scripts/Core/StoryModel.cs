using System;
using System.Collections.Generic;

namespace ShanHai
{
    [Serializable] public class StoryData
    {
        public int version;
        public string title, entry;
        public VariableDef[] variables;
        public StoryNode[] nodes;
        public StoryEdge[] edges;
        public CodexEntry[] codex;
    }
    [Serializable] public class VariableDef { public string key, name; public int initial, min, max; }
    [Serializable] public class Change { public string key, op; public int value; }
    [Serializable] public class DialogueLine { public string speaker, text; public int expression; }
    [Serializable] public class ExploreTarget { public string id, title, text; public float x, y; }
    [Serializable] public class StoryChoice
    {
        public string id, text, to, reply;
        public int delta;
        public bool toward;
    }
    [Serializable] public class StoryNode
    {
        public string id, title, plot, chapter, kind, summary, background, music, mechanic, effects;
        public string[] characters, unlocks;
        public DialogueLine[] lines;
        public Change[] changes;
        public ExploreTarget[] targets;
        public StoryChoice[] choices;
    }
    [Serializable] public class Condition { public string key, op; public int value; }
    [Serializable] public class StoryEdge
    {
        public string id, from, to, kind, text;
        public int priority;
        public bool any;
        public Condition[] conditions;
    }
    [Serializable] public class CodexEntry { public string id, title, category, art, text; }
    [Serializable] public class StateValue { public string key; public int value; public StateValue() {} public StateValue(string k, int v) { key = k; value = v; } }
    [Serializable] public class HistoryEntry { public string node, speaker, text; }
    [Serializable] public class SaveState
    {
        public int version = 1;
        public string nodeId, route = "", savedAt = "", choiceReply = "";
        public int line, miniStep;
        public bool mechanicDone;
        public float playerX = .14f, playerY = .65f;
        public List<StateValue> values = new List<StateValue>();
        public List<string> completed = new List<string>();
        public List<string> visited = new List<string>();
        public List<string> inspected = new List<string>();
        public List<string> choices = new List<string>();
        public List<int> puzzle = new List<int>();
        public List<int> rubbed = new List<int>();
        public List<int> matched = new List<int>();
        public List<string> deductions = new List<string>();
        public List<int> reasoningOrder = new List<int>();
        public int reasoningHints;
        public int mountingRotation, mountingSeal = -1;
        public List<HistoryEntry> history = new List<HistoryEntry>();
        public List<string> items = new List<string>();
        public List<string> evidence = new List<string>();
        public InvestigationState investigation = new InvestigationState();
    }
    [Serializable] public class InvestigationState
    {
        public string node = "";
        public bool prepared, solved;
        public int progress, hints;
        public List<int> dials = new List<int>();
    }
    [Serializable] public class Checkpoint { public string nodeId, title, json; }
    [Serializable] public class PlayerProfile
    {
        public int version = 1;
        public List<string> unlocked = new List<string>();
        public List<string> readLines = new List<string>();
        public List<int> endings = new List<int>();
        public List<Checkpoint> checkpoints = new List<Checkpoint>();
        public float musicVolume = .45f, soundVolume = .65f, textSpeed = 30f, autoDelay = 2.2f;
        public float textScale = 1f;
        public bool skipUnread = false, reducedMotion = false;
    }
}
