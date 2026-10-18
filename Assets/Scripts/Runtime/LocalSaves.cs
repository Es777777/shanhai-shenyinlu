using System;
using UnityEngine;

namespace ShanHai
{
    public static class LocalSaves
    {
        const string Prefix = "shanhai.v1.";
        public static string LastError = "";
        static void Store(string key, string json)
        {
            try { PlayerPrefs.SetString(Prefix + key, json); PlayerPrefs.Save(); LastError = ""; }
            catch (Exception e) { LastError = "本地存储不可用，请检查浏览器存储权限：" + e.Message; Debug.LogWarning(LastError); }
        }
        public static void Save(int slot, SaveState state)
        {
            state.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Store("slot" + slot, JsonUtility.ToJson(state));
        }
        public static SaveState Read(int slot)
        {
            try
            {
                string json = PlayerPrefs.GetString(Prefix + "slot" + slot, "");
                return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveState>(json);
            }
            catch (Exception) { LastError = "这份存档已损坏，请选择其他存档。"; return null; }
        }
        public static PlayerProfile LoadProfile()
        {
            try
            {
                string json = PlayerPrefs.GetString(Prefix + "profile", "");
                var p = string.IsNullOrEmpty(json) ? new PlayerProfile() : JsonUtility.FromJson<PlayerProfile>(json);
                if (p == null || p.version != 1 || p.unlocked == null || p.readLines == null || p.endings == null || p.checkpoints == null) return new PlayerProfile();
                p.musicVolume = Mathf.Clamp01(p.musicVolume); p.soundVolume = Mathf.Clamp01(p.soundVolume);
                p.textSpeed = Mathf.Clamp(p.textSpeed, 10, 80); p.autoDelay = Mathf.Clamp(p.autoDelay, 1, 6);
                return p;
            }
            catch (Exception) { return new PlayerProfile(); }
        }
        public static void SaveProfile(PlayerProfile p) { Store("profile", JsonUtility.ToJson(p)); }
    }
}
