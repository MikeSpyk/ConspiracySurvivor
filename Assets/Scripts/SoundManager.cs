using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public enum SoundCategory { beach, test2, itemUsage, GUI }
    public enum SoundBiome { unknown, beach }

    public static SoundManager singleton;

    [SerializeField] private GameObject soundPrefab;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] m_AudioClips;
    [SerializeField] private float[] m_AudioClipsVolumes;
    [SerializeField] private float[] m_AudioClipsRange;
    [SerializeField] private AnimationCurve[] m_AudioClipsFalloff;
    [SerializeField] private int[] m_AudioClipsWarmUpCount;
    [Header("Ambient Sounds")]
    [SerializeField] private float m_oceanCostAmbientMaxVolume = 0.05f;
    [SerializeField] private float m_oceanAmbientHearDistance = 390; // distance from water the sound should be played

    [SerializeField] private float m_seagulSoundHeight = 20;
    [SerializeField] private float m_seagulSoundProbability = 2f;
    [SerializeField] private float m_seagulSoundDistance = 50f;

    [Header("Debug")]
    [SerializeField] private bool m_hideSoundsInHierarchy = true;
    [SerializeField] private bool DEBUG_noCache = false;

    private bool m_lastHideSoundsInHierarchy;
    private int m_randomWorldSoundsCounter = 0;
    private SoundBiome m_currentBiome = SoundBiome.unknown; //  the player is in
    private float m_playerDistanceToWaterCurrent;
    private Dictionary<int, List<Sound>> m_soundIndex_freeSounds = null;
    private List<Sound> m_activeSounds = new List<Sound>();
    private bool m_worldBuildingDone = false;
    private Sound m_oceanCostAmbientSound = null;
    private bool m_oceanCostAmbientIsPlaying = false;
    private bool m_ambientSoundActive = false;

    private void Awake()
    {
        m_lastHideSoundsInHierarchy = m_hideSoundsInHierarchy;

        singleton = this;

        if (m_AudioClips.Length != m_AudioClipsVolumes.Length)
        {
            Debug.LogError("SoundManager: Awake: \"m_AudioClipsVolumes.Length\" differs from m_AudioClips.Length !");
        }

        if (m_AudioClips.Length != m_AudioClipsRange.Length)
        {
            Debug.LogError("SoundManager: Awake: \"m_AudioClipsRange.Length\" differs from m_AudioClips.Length !");
        }

        if (m_AudioClips.Length != m_AudioClipsFalloff.Length)
        {
            Debug.LogError("SoundManager: Awake: \"m_AudioClipsFalloff.Length\" differs from m_AudioClips.Length !");
        }

        if (m_AudioClips.Length != m_AudioClipsWarmUpCount.Length)
        {
            Debug.LogError("SoundManager: Awake: \"m_AudioClipsWarmUpCount.Length\" differs from m_AudioClips.Length !");
        }

        if (DEBUG_noCache)
        {
            Debug.LogWarning("SoundManager: Awake: sound cache is deactivated !");
        }

        m_soundIndex_freeSounds = new Dictionary<int, List<Sound>>();

        for (int i = 0; i < m_AudioClips.Length; i++)
        {
            m_soundIndex_freeSounds.Add(i, new List<Sound>());
        }
    }

    private void Start()
    {
        soundsWarmUp();
    }

    private void Update()
    {
        if (m_worldBuildingDone && EntityManager.singleton.getLocalPlayer() != null)
        {
            ambientSoundUpdate();
        }

        updateHideInHierarchy();
    }

    private void updateHideInHierarchy()
    {
        if (m_lastHideSoundsInHierarchy != m_hideSoundsInHierarchy)
        {
            m_lastHideSoundsInHierarchy = m_hideSoundsInHierarchy;

            for (int i = 0; i < m_activeSounds.Count; i++)
            {
                if (m_hideSoundsInHierarchy)
                {
                    m_activeSounds[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
                }
                else
                {
                    m_activeSounds[i].gameObject.hideFlags = HideFlags.None;
                }
            }

            foreach (KeyValuePair<int, List<Sound>> pair in m_soundIndex_freeSounds)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    if (m_hideSoundsInHierarchy)
                    {
                        pair.Value[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
                    }
                    else
                    {
                        pair.Value[i].gameObject.hideFlags = HideFlags.None;
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        randomWorldSoundsUpdate();
    }

    private void soundsWarmUp()
    {
        for (int i = 0; i < m_AudioClipsWarmUpCount.Length; i++)
        {
            for (int j = 0; j < m_AudioClipsWarmUpCount[i]; j++)
            {
                Sound tempSound = getSound(i);

                tempSound.setVolume(0);
                tempSound.playOnce();
            }
        }
    }

    private void ambientSoundUpdate()
    {
        if (m_ambientSoundActive)
        {
            if (!m_oceanCostAmbientIsPlaying)
            {
                m_oceanCostAmbientIsPlaying = true;
                m_oceanCostAmbientSound = playGlobalSound(15, Sound.SoundPlaystyle.loop);
                m_oceanCostAmbientSound.setVolume(m_oceanCostAmbientMaxVolume);
            }

            m_playerDistanceToWaterCurrent = WorldManager.singleton.getLastPlayerDistanceToWater().magnitude;

            if (m_playerDistanceToWaterCurrent < m_oceanAmbientHearDistance)
            {
                m_oceanCostAmbientSound.fadeVolumeTo((1f - m_playerDistanceToWaterCurrent / m_oceanAmbientHearDistance) * m_oceanCostAmbientMaxVolume);
                m_currentBiome = SoundBiome.beach;
            }
            else
            {
                m_oceanCostAmbientSound.fadeVolumeTo(0.0f);
                m_currentBiome = SoundBiome.unknown; // should probably be moved
            }
        }
    }

    private void randomWorldSoundsUpdate()
    {
        switch (m_currentBiome)
        {
            case SoundBiome.unknown:
                {
                    break;
                }
            case SoundBiome.beach:
                {
                    if (RandomValuesSeed.getRandomBoolProbability(Time.realtimeSinceStartup, m_randomWorldSoundsCounter, m_seagulSoundProbability))
                    {
                        int randomIndex;
                        float randomValue = RandomValuesSeed.getRandomValueSeed((float)m_randomWorldSoundsCounter, Time.realtimeSinceStartup);

                        if (randomValue < .33f)
                        {
                            randomIndex = 18;
                        }
                        else if (randomValue < .66f)
                        {
                            randomIndex = 17;
                        }
                        else
                        {
                            randomIndex = 16;
                        }

                        Vector2 randomPosXZ = new Vector2((RandomValuesSeed.getRandomValueSeed(Time.realtimeSinceStartup, (float)m_randomWorldSoundsCounter + 4) - 0.5f) * 2 * m_seagulSoundDistance,
                                                                                    (RandomValuesSeed.getRandomValueSeed(Time.realtimeSinceStartup, (float)m_randomWorldSoundsCounter + 22) - 0.5f) * 2 * m_seagulSoundDistance);

                        RaycastHit hit;

                        Physics.Raycast(EntityManager.singleton.getLocalPlayerPosition() + new Vector3(randomPosXZ.x, 100f, randomPosXZ.y), Vector3.down, out hit);

                        if (hit.transform == null)
                        {
                            Debug.LogWarning("soundmanager couldnt find ground near player");
                        }

                        playSoundAt(randomIndex, hit.point + new Vector3(0, m_seagulSoundHeight, 0), Sound.SoundPlaystyle.Once);
                    }
                    break;
                }
            default:
                {
                    Debug.LogWarning("unknown biome: " + m_currentBiome);
                    break;
                }
        }
        if (m_randomWorldSoundsCounter > 100000)
        {
            m_randomWorldSoundsCounter = 0;
        }
        m_randomWorldSoundsCounter++;
    }

    public void onWorldBuildDone(float newWorldSize)
    {
        m_worldBuildingDone = true;
    }

    public void server_playGlobalSound(int index)
    {
        NetworkingManager.singleton.server_sendGlobalSoundToAllTCP(index);
    }

    public Sound playGlobalSound(int soundIndex, Sound.SoundPlaystyle playStyle)
    {
        if (soundIndex >= m_AudioClips.Length || soundIndex < 0)
        {
            Debug.LogError("SoundManager: soundIndex out of range: " + soundIndex);
            return null;
        }

        Sound tempSound = getSound(soundIndex);
        tempSound.setGlobalLocal(true);

        if (playStyle == Sound.SoundPlaystyle.loop)
        {
            tempSound.playLooping();
        }
        else if (playStyle == Sound.SoundPlaystyle.Once)
        {
            tempSound.playOnce();
        }

        return tempSound;
    }

    public Sound playSoundAt(int soundIndex, Vector3 position, Sound.SoundPlaystyle playStyle)
    {
        if (soundIndex >= m_AudioClips.Length || soundIndex < 0)
        {
            Debug.LogError("SoundManager: soundIndex out of range: " + soundIndex);
            return null;
        }

        Sound tempSound = getSound(soundIndex);

        tempSound.transform.position = position;
        tempSound.setGlobalLocal(false);

        if (playStyle == Sound.SoundPlaystyle.loop)
        {
            tempSound.playLooping();
        }
        else if (playStyle == Sound.SoundPlaystyle.Once)
        {
            tempSound.playOnce();
        }

        return tempSound;
    }

    private Sound getSound(int soundIndex)
    {
        Sound returnValue = null;

        if (m_soundIndex_freeSounds[soundIndex].Count < 1 || DEBUG_noCache)
        {
            returnValue = (Instantiate(soundPrefab) as GameObject).GetComponent<Sound>();
            returnValue.initialize(soundIndex, m_AudioClips[soundIndex], m_AudioClipsVolumes[soundIndex], false, m_AudioClipsFalloff[soundIndex]);
            if (m_hideSoundsInHierarchy)
            {
                returnValue.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
        }
        else
        {
            returnValue = m_soundIndex_freeSounds[soundIndex][0];
            m_soundIndex_freeSounds[soundIndex].RemoveAt(0);
            returnValue.setDefaultVolume();
            returnValue.gameObject.SetActive(true);
        }

        m_activeSounds.Add(returnValue);

        return returnValue;
    }

    public void recyleSound(Sound sound)
    {
        sound.transform.SetParent(null);
        sound.stopPlaying();
        sound.gameObject.SetActive(false);
        m_activeSounds.Remove(sound);
        m_soundIndex_freeSounds[sound.SoundIndex].Add(sound);
    }

    public float getMaxHearableDistance(int soundIndex)
    {
        if (soundIndex < 0 || soundIndex >= m_AudioClipsRange.Length)
        {
            Debug.LogWarning("SoundManager: getMaxHearableDistance: index out of range");
            return 0;
        }
        else
        {
            return m_AudioClipsRange[soundIndex];
        }
    }

    public void setAmbientSoundActivity(bool active)
    {
        m_ambientSoundActive = active;

        if (active == false)
        {
            if (m_oceanCostAmbientSound != null)
            {
                m_oceanCostAmbientSound.setVolume(0.0f);
            }
        }
    }
}
