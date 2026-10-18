using System.Collections.Generic;
namespace ShanHai
{
    public static class WorkshopRules
    {
        public static bool CanMount(SaveState state)
        { return state.deductions.Contains("crow-observed") && state.mountingRotation == 2; }
        public static bool CanSeal(SaveState state)
        { return state.miniStep > 0 && state.mountingSeal == 0; }
        public static bool MemoryEvidence(ICollection<string> evidence)
        { return evidence.Contains("red-brush") && evidence.Contains("blue-scroll") && !evidence.Contains("lantern") && !evidence.Contains("leaves"); }
        public static bool MemoryConclusion(SaveState state,int answer)
        { return InteractionRules.IsSolved(state.puzzle) && MemoryEvidence(state.deductions) && answer == 1; }
        public static bool MirrorConclusion(SaveState state,int answer)
        { return state.matched.Count == 4 && MirrorEvidence(state.deductions) && answer == 1; }
        public static bool MirrorEvidence(ICollection<string> evidence)
        { return evidence.Contains("xiangliu-testimony") && evidence.Contains("wangxiang-testimony") && !evidence.Contains("vent-testimony") && !evidence.Contains("boat-testimony"); }
    }
}
