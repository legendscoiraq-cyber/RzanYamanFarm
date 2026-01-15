using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("قاعدة البيانات")]
    public List<EducationalBlock> allBlocks;

    [Header("أصوات اللعبة")]
    public AudioClip backgroundMusic;
    public AudioClip levelCompleteSound;
    public AudioClip collectSound;
    public AudioClip buttonClickSound;

    [Header("إعدادات الصوت")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource audioSourcePrefab;
    public int poolSize = 10;
    
    private List<AudioSource> sourcePool;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        InitializePool();
        // PlayBackgroundMusic(); // Uncomment if clip assigned
    }

    private void InitializePool()
    {
        sourcePool = new List<AudioSource>();
        GameObject poolRoot = new GameObject("AudioPool");
        poolRoot.transform.SetParent(transform);

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = Instantiate(audioSourcePrefab, poolRoot.transform);
            source.gameObject.SetActive(false);
            sourcePool.Add(source);
        }
    }

    public void PlayClip(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;
        AudioSource source = GetFreeSource();
        source.transform.position = position;
        source.gameObject.SetActive(true);
        source.clip = clip;
        source.volume = 1f;
        source.Play();
        StartCoroutine(DisableSourceAfterPlay(source, clip.length + 0.1f));
    }

    public void PlayClipLocal(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayButtonClick() => PlayClipLocal(buttonClickSound, 0.5f);
    public void PlayCollect() => PlayClipLocal(collectSound, 0.7f);
    public void PlayLevelComplete() => PlayClipLocal(levelCompleteSound, 1f);

    private System.Collections.IEnumerator DisableSourceAfterPlay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        source.gameObject.SetActive(false);
    }

    private AudioSource GetFreeSource()
    {
        foreach (var s in sourcePool) if (!s.gameObject.activeInHierarchy) return s;
        return sourcePool[0];
    }

    public EducationalBlock GetBlockByName(string name) => allBlocks.Find(b => b.blockName == name);
    public List<EducationalBlock> GetBlocksByCategory(BlockCategory category) => allBlocks.FindAll(b => b.category == category);
}
