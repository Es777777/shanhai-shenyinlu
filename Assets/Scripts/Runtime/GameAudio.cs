using UnityEngine;

namespace ShanHai
{
    public sealed class GameAudio : MonoBehaviour
    {
        AudioSource music, sfx;
        string track = "";
        public PlayerProfile profile;
        float targetVolume;
        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.playOnAwake = false;
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
        }
        public void PlayMusic(string id, int ending = 0)
        {
            string next = id == "ending" ? "ending" + (ending > 0 ? ending : 3) : id;
            if (track == next) return;
            track = next;
            var clip = Resources.Load<AudioClip>("Audio/" + track);
            if (clip == null) clip = Resources.Load<AudioClip>("Audio/prologue");
            music.Stop(); music.clip = clip; music.volume = 0; music.loop = id != "ending";
            if (clip != null) music.Play();
        }
        public void Sound(string id)
        {
            var clip = Resources.Load<AudioClip>("Audio/" + id);
            if (clip != null) sfx.PlayOneShot(clip, profile == null ? .6f : profile.soundVolume);
        }
        void Update()
        {
            targetVolume = profile == null ? .45f : profile.musicVolume;
            music.volume = Mathf.MoveTowards(music.volume, targetVolume, Time.unscaledDeltaTime * .4f);
        }
        public void Resume() { if (music.clip != null && !music.isPlaying) music.Play(); }
    }
}
