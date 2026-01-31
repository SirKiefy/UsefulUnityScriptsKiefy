using UnityEngine;
using System.Collections.Generic;

namespace UsefulScripts.Audio
{
    /// <summary>
    /// Central audio manager for playing sounds and music.
    /// </summary>
    public class AudioManager : Core.Singleton<AudioManager>
    {
        [System.Serializable]
        public class Sound
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.1f, 3f)] public float pitch = 1f;
            [Range(0f, 1f)] public float spatialBlend = 0f;
            public bool loop = false;
            public AudioMixerGroup mixerGroup;
            
            [HideInInspector] public AudioSource source;
        }

        [System.Serializable]
        public class AudioMixerGroup
        {
            public string groupName;
            public UnityEngine.Audio.AudioMixerGroup mixerGroup;
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Sound Library")]
        [SerializeField] private List<Sound> sounds = new List<Sound>();

        [Header("Music")]
        [SerializeField] private List<AudioClip> musicTracks = new List<AudioClip>();
        [SerializeField] private float musicCrossfadeDuration = 1f;

        [Header("Volume Settings")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;

        private Dictionary<string, Sound> soundDict = new Dictionary<string, Sound>();
        private Dictionary<string, AudioClip> musicDict = new Dictionary<string, AudioClip>();
        private AudioSource crossfadeSource;
        private int currentTrackIndex = -1;

        // Properties
        public float MasterVolume 
        { 
            get => masterVolume; 
            set { masterVolume = Mathf.Clamp01(value); UpdateVolumes(); } 
        }
        public float MusicVolume 
        { 
            get => musicVolume; 
            set { musicVolume = Mathf.Clamp01(value); UpdateVolumes(); } 
        }
        public float SfxVolume 
        { 
            get => sfxVolume; 
            set { sfxVolume = Mathf.Clamp01(value); UpdateVolumes(); } 
        }
        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;

        protected override void OnSingletonAwake()
        {
            InitializeAudio();
        }

        private void InitializeAudio()
        {
            // Setup music source
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            // Setup SFX source
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            // Setup crossfade source
            GameObject crossfadeObj = new GameObject("CrossfadeSource");
            crossfadeObj.transform.SetParent(transform);
            crossfadeSource = crossfadeObj.AddComponent<AudioSource>();
            crossfadeSource.loop = true;
            crossfadeSource.playOnAwake = false;

            // Build dictionaries
            foreach (var sound in sounds)
            {
                soundDict[sound.name] = sound;
            }

            foreach (var track in musicTracks)
            {
                if (track != null)
                {
                    musicDict[track.name] = track;
                }
            }

            UpdateVolumes();
        }

        /// <summary>
        /// Play a sound effect by name
        /// </summary>
        public void PlaySound(string soundName)
        {
            if (!soundDict.TryGetValue(soundName, out var sound))
            {
                Debug.LogWarning($"Sound '{soundName}' not found!");
                return;
            }

            sfxSource.pitch = sound.pitch;
            sfxSource.PlayOneShot(sound.clip, sound.volume * sfxVolume * masterVolume);
        }

        /// <summary>
        /// Play a sound effect at a position
        /// </summary>
        public void PlaySoundAtPosition(string soundName, Vector3 position)
        {
            if (!soundDict.TryGetValue(soundName, out var sound))
            {
                Debug.LogWarning($"Sound '{soundName}' not found!");
                return;
            }

            AudioSource.PlayClipAtPoint(sound.clip, position, sound.volume * sfxVolume * masterVolume);
        }

        /// <summary>
        /// Play a random sound from a list
        /// </summary>
        public void PlayRandomSound(params string[] soundNames)
        {
            if (soundNames.Length == 0) return;
            string randomSound = soundNames[Random.Range(0, soundNames.Length)];
            PlaySound(randomSound);
        }

        /// <summary>
        /// Play a clip directly
        /// </summary>
        public void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
            }
        }

        /// <summary>
        /// Play music track by name
        /// </summary>
        public void PlayMusic(string trackName, bool crossfade = true)
        {
            if (!musicDict.TryGetValue(trackName, out var clip))
            {
                Debug.LogWarning($"Music track '{trackName}' not found!");
                return;
            }

            if (crossfade && musicSource.isPlaying)
            {
                StartCoroutine(CrossfadeMusic(clip));
            }
            else
            {
                musicSource.clip = clip;
                musicSource.volume = musicVolume * masterVolume;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Play music track by index
        /// </summary>
        public void PlayMusic(int trackIndex, bool crossfade = true)
        {
            if (trackIndex < 0 || trackIndex >= musicTracks.Count)
            {
                Debug.LogWarning($"Music track index {trackIndex} out of range!");
                return;
            }

            currentTrackIndex = trackIndex;
            AudioClip clip = musicTracks[trackIndex];

            if (crossfade && musicSource.isPlaying)
            {
                StartCoroutine(CrossfadeMusic(clip));
            }
            else
            {
                musicSource.clip = clip;
                musicSource.volume = musicVolume * masterVolume;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Play the next music track
        /// </summary>
        public void NextTrack()
        {
            if (musicTracks.Count == 0) return;
            currentTrackIndex = (currentTrackIndex + 1) % musicTracks.Count;
            PlayMusic(currentTrackIndex);
        }

        /// <summary>
        /// Play the previous music track
        /// </summary>
        public void PreviousTrack()
        {
            if (musicTracks.Count == 0) return;
            currentTrackIndex = currentTrackIndex <= 0 ? musicTracks.Count - 1 : currentTrackIndex - 1;
            PlayMusic(currentTrackIndex);
        }

        /// <summary>
        /// Stop music
        /// </summary>
        public void StopMusic(bool fade = true)
        {
            if (fade)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// Pause music
        /// </summary>
        public void PauseMusic()
        {
            musicSource.Pause();
        }

        /// <summary>
        /// Resume music
        /// </summary>
        public void ResumeMusic()
        {
            musicSource.UnPause();
        }

        /// <summary>
        /// Set music time
        /// </summary>
        public void SetMusicTime(float time)
        {
            musicSource.time = time;
        }

        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            crossfadeSource.clip = newClip;
            crossfadeSource.volume = 0;
            crossfadeSource.Play();

            float startVolume = musicSource.volume;
            float elapsed = 0;

            while (elapsed < musicCrossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / musicCrossfadeDuration;
                musicSource.volume = Mathf.Lerp(startVolume, 0, t);
                crossfadeSource.volume = Mathf.Lerp(0, musicVolume * masterVolume, t);
                yield return null;
            }

            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
            crossfadeSource.Stop();
        }

        private System.Collections.IEnumerator FadeOutMusic()
        {
            float startVolume = musicSource.volume;
            float elapsed = 0;

            while (elapsed < musicCrossfadeDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0, elapsed / musicCrossfadeDuration);
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = musicVolume * masterVolume;
        }

        private void UpdateVolumes()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        /// <summary>
        /// Register a sound at runtime
        /// </summary>
        public void RegisterSound(string name, AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            var sound = new Sound
            {
                name = name,
                clip = clip,
                volume = volume,
                pitch = pitch
            };
            soundDict[name] = sound;
            sounds.Add(sound);
        }

        /// <summary>
        /// Check if a sound exists
        /// </summary>
        public bool HasSound(string soundName)
        {
            return soundDict.ContainsKey(soundName);
        }
    }
}
