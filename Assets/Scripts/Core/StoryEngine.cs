using System;
using System.Collections.Generic;
using System.Linq;

namespace ShanHai
{
    // This class has no Unity dependency, so graph and save rules can be tested directly.
    public sealed class StoryEngine
    {
        public readonly StoryData Data;
        public readonly PlayerProfile Profile;
        public SaveState State { get; private set; }
        readonly Dictionary<string, StoryNode> nodes;
        public StoryNode Node { get { return nodes[State.nodeId]; } }
        public bool DialogueFinished { get { return State.line >= Node.lines.Length; } }
        public bool IsEnding { get { return Node.id == "N911" || Node.id == "N912" || Node.id == "N913"; } }
        public bool NeedsMechanic { get { return !string.IsNullOrEmpty(Node.mechanic) && !State.mechanicDone; } }

        public StoryEngine(StoryData data, PlayerProfile profile)
        {
            Data = data; Profile = profile;
            nodes = data.nodes.ToDictionary(n => n.id);
            ValidateData();
            NewGame();
        }
        public void NewGame()
        {
            State = new SaveState();
            State.values = Data.variables.Select(v => new StateValue(v.key, v.initial)).ToList();
            Enter(Data.entry);
        }
        public void Restore(SaveState state)
        {
            if (state == null || state.version != 1 || !nodes.ContainsKey(state.nodeId)) throw new ArgumentException("存档版本或节点无效");
            if (state.values == null || state.completed == null || state.visited == null || state.inspected == null || state.choices == null || state.history == null || state.puzzle == null || state.rubbed == null || state.matched == null) throw new ArgumentException("存档内容不完整");
            if(state.values.Any(v=>v==null)||state.history.Any(h=>h==null||h.text==null||h.speaker==null)||state.history.Count>500||state.completed.Any(id=>id==null||!nodes.ContainsKey(id))||state.visited.Any(id=>id==null||!nodes.ContainsKey(id))||state.choices.Any(x=>x==null))throw new ArgumentException("存档记录无效");
            if(state.miniStep<0||state.miniStep>12||state.matched.Any(x=>x<0||x>3)||state.matched.Distinct().Count()!=state.matched.Count||state.rubbed.Any(x=>x<0||x>=180))throw new ArgumentException("互动记录无效");
            foreach (var def in Data.variables)
                if (state.values.Count(v => v.key == def.key) != 1) throw new ArgumentException("存档数值无效");
            if (state.line < 0 || state.line > nodes[state.nodeId].lines.Length) throw new ArgumentException("存档对白位置无效");
            if (state.puzzle.Count != 0 && (state.puzzle.Count != 6 || state.puzzle.Distinct().Count() != 6 || state.puzzle.Any(v => v < 0 || v > 5))) throw new ArgumentException("拼图存档无效");
            if (state.deductions == null) state.deductions = new List<string>();
            if(state.reasoningOrder==null)state.reasoningOrder=new List<int>();
            if(state.reasoningOrder.Count!=0&&(state.reasoningOrder.Count!=3||state.reasoningOrder.Distinct().Count()!=3||state.reasoningOrder.Any(x=>x<0||x>2)))throw new ArgumentException("推理顺序无效");
            state.reasoningHints=Math.Max(0,Math.Min(3,state.reasoningHints));
            state.mountingRotation = ((state.mountingRotation % 4) + 4) % 4;
            State = state;
            // Older v1 saves have no investigation fields. Keep their story progress.
            if(State.items==null)State.items=new List<string>();
            if(State.evidence==null)State.evidence=new List<string>();
            if(State.investigation==null)State.investigation=new InvestigationState();
            if(State.investigation.dials==null)throw new ArgumentException("机关记录不完整");
            var inv=State.investigation;
            if(inv.progress<0||inv.progress>(inv.node=="N801"?6:inv.node=="N301"?4:7)||inv.dials.Any(x=>x<0||x>4))throw new ArgumentException("机关状态无效");
            foreach (var def in Data.variables) Set(def.key, Get(def.key));
        }
        public int Get(string key) { var v = State.values.Find(x => x.key == key); return v == null ? 0 : v.value; }
        public void Set(string key, int value)
        {
            var def = Data.variables.First(v => v.key == key);
            State.values.Find(v => v.key == key).value = Math.Max(def.min, Math.Min(def.max, value));
        }
        public string CurrentReadKey { get { return Node.id + ":" + State.line; } }
        public bool CurrentLineRead { get { return Profile.readLines.Contains(CurrentReadKey); } }
        public void ReadLine()
        {
            if (DialogueFinished) return;
            var line = Node.lines[State.line];
            if (!Profile.readLines.Contains(CurrentReadKey)) Profile.readLines.Add(CurrentReadKey);
            State.history.Add(new HistoryEntry { node = Node.id, speaker = line.speaker, text = line.text });
            if (State.history.Count > 500) State.history.RemoveAt(0);
            State.line++;
        }
        public StoryChoice[] AvailableChoices()
        {
            if (!DialogueFinished || NeedsMechanic || IsEnding) return new StoryChoice[0];
            if (Node.choices != null && Node.choices.Length > 0) return Node.choices;
            return Outgoing().Where(e => e.kind == "玩家选择" && Satisfies(e)).Select(e => new StoryChoice { id = e.id, text = e.text, to = e.to }).ToArray();
        }
        public void FinishMechanic()
        {
            if (!DialogueFinished) throw new InvalidOperationException("对白尚未结束");
            State.mechanicDone = true;
        }
        public void Complete()
        {
            if (!DialogueFinished || NeedsMechanic) throw new InvalidOperationException("本幕尚未完成");
            if (State.completed.Contains(Node.id)) return;
            foreach (var change in Node.changes)
                Set(change.key, change.op == "set" ? change.value : Get(change.key) + change.value);
            State.completed.Add(Node.id);
            foreach (var entry in Node.unlocks ?? new string[0])
                if (!Profile.unlocked.Contains(entry)) Profile.unlocked.Add(entry);
            if (IsEnding && !Profile.endings.Contains(Get("ending"))) Profile.endings.Add(Get("ending"));
        }
        public void Select(string choiceId)
        {
            var choice = AvailableChoices().FirstOrDefault(c => c.id == choiceId);
            if (choice == null) throw new InvalidOperationException("当前选项不可用");
            Complete();
            var key = Node.id + ":" + choice.id;
            if (!State.choices.Any(c => c.StartsWith(Node.id + ":", StringComparison.Ordinal)))
            {
                int value = Get("mcv");
                Set("mcv", choice.toward ? value + Math.Sign(50 - value) * Math.Min(Math.Abs(50 - value), Math.Abs(choice.delta)) : value + choice.delta);
                State.choices.Add(key);
                State.history.Add(new HistoryEntry { node = Node.id, speaker = "抉择", text = choice.text });
            }
            if (Node.id == "N002") State.route = choice.to == "N101" ? "玄曜" : "灵曦";
            if (!string.IsNullOrEmpty(choice.reply)) State.history.Add(new HistoryEntry { node = Node.id, speaker = "旁白", text = choice.reply });
            Enter(choice.to);
            State.choiceReply = choice.reply ?? "";
        }
        public string Advance()
        {
            if (AvailableChoices().Length > 0) throw new InvalidOperationException("请先选择");
            Complete();
            if (IsEnding) return null;
            var automatic = Outgoing().Where(e => e.kind != "玩家选择").ToArray();
            var edge = automatic.FirstOrDefault(e => e.kind != "兜底" && Satisfies(e)) ?? automatic.FirstOrDefault(e => e.kind == "兜底");
            if (edge == null) throw new InvalidOperationException("当前剧情没有满足条件的后续幕：" + Node.id);
            Enter(edge.to);
            return edge.to;
        }
        IEnumerable<StoryEdge> Outgoing() { return Data.edges.Where(e => e.from == Node.id).OrderBy(e => e.priority).ThenBy(e => e.id); }
        public bool Satisfies(StoryEdge edge)
        {
            if (edge.conditions == null || edge.conditions.Length == 0) return true;
            return edge.any ? edge.conditions.Any(Check) : edge.conditions.All(Check);
        }
        bool Check(Condition c)
        {
            int v = Get(c.key);
            switch (c.op) { case ">=": return v >= c.value; case "<=": return v <= c.value; case ">": return v > c.value; case "<": return v < c.value; case "!=": return v != c.value; case "==": case "=": return v == c.value; default: throw new InvalidOperationException("未知条件比较符 " + c.op); }
        }
        void Enter(string id)
        {
            State.nodeId = id; State.line = 0; State.miniStep = 0; State.mechanicDone = false;
            State.inspected.Clear(); State.puzzle.Clear(); State.rubbed.Clear(); State.matched.Clear(); State.choiceReply = "";
            State.deductions.Clear(); State.mountingRotation = 0; State.mountingSeal = -1;
            State.reasoningOrder.Clear();State.reasoningHints=0;
            State.playerX = .14f; State.playerY = .65f;
            State.investigation=new InvestigationState();
            if (!State.visited.Contains(id)) State.visited.Add(id);
        }
        void ValidateData()
        {
            if (!nodes.ContainsKey(Data.entry)) throw new ArgumentException("入口不存在");
            foreach (var e in Data.edges) if (!nodes.ContainsKey(e.from) || !nodes.ContainsKey(e.to)) throw new ArgumentException("剧情链引用不存在 " + e.id);
            foreach (var n in Data.nodes)
            {
                if (n.lines == null || n.lines.Length == 0) throw new ArgumentException("缺少对白 " + n.id);
                foreach (var c in n.choices ?? new StoryChoice[0]) if (!nodes.ContainsKey(c.to)) throw new ArgumentException("选项目标不存在 " + n.id);
            }
        }
    }
}
