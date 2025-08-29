using UnityEngine;

public class UnitAudio : MonoBehaviour
{
    AudioSource source;

    public AudioClip BattleSound;
    public AudioClip HitSound;

    private void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public void PlayBattle()
    {
        if(BattleSound && source)
            source.PlayOneShot(BattleSound);
    }
}
