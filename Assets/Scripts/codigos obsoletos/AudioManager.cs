using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip dashClip;
    public AudioClip inAirLoopClip;
    public AudioClip walkLoopClip;

    private AudioSource sfx;
    private AudioSource airLoop;
    private AudioSource walkLoop;

    void Awake()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        airLoop = gameObject.AddComponent<AudioSource>();
        airLoop.loop = true;
        airLoop.playOnAwake = false;

        walkLoop = gameObject.AddComponent<AudioSource>();
        walkLoop.loop = true;
        walkLoop.playOnAwake = false;
    }

    public void PlayJump() => sfx.PlayOneShot(jumpClip);
    public void PlayLand() => sfx.PlayOneShot(landClip);
    public void PlayDash() => sfx.PlayOneShot(dashClip);

    public void StartAirLoop()
    {
        if (inAirLoopClip == null) return;
        if (!airLoop.isPlaying)
        {
            airLoop.clip = inAirLoopClip;
            airLoop.Play();
        }
    }

    public void StopAirLoop()
    {
        if (airLoop.isPlaying)
            airLoop.Stop();
    }

    public void StartWalkLoop()
    {
        if (walkLoopClip == null) return;
        if (!walkLoop.isPlaying)
        {
            walkLoop.clip = walkLoopClip;
            walkLoop.Play();
        }
    }

    public void StopWalkLoop()
    {
        if (walkLoop.isPlaying)
            walkLoop.Stop();
    }
}
