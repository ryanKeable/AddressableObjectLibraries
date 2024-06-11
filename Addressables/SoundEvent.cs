using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

// The base class for sound things
[CreateAssetMenu]
public class SoundEvent : ScriptableObject
{

    [HideInInspector, SerializeField] bool init = false;
    public List<AudioClip> clipList;

    public AnimationCurve pitch;
    public AnimationCurve volume;
    public AnimationCurve pan;
    public AudioMixerGroup mixingGroup;
    public bool loops = false;
    public bool startAtRandomTime = false;

    private void OnEnable()
    {
        if (init == true) return;

        pitch = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        pitch.preWrapMode = WrapMode.PingPong;
        pitch.postWrapMode = WrapMode.PingPong;

        volume = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        volume.preWrapMode = WrapMode.PingPong;
        volume.postWrapMode = WrapMode.PingPong;

        pan = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        pan.preWrapMode = WrapMode.PingPong;
        pan.postWrapMode = WrapMode.PingPong;

        init = true;
    }

    private float pitchValue()
    {
        float value = UnityEngine.Random.value;
        return pitch.Evaluate(value);
    }

    private float volumeValue()
    {
        float value = UnityEngine.Random.value;
        return volume.Evaluate(value);
    }

    private float panValue()
    {
        float value = UnityEngine.Random.value;
        return pan.Evaluate(value);
    }

    private AudioClip clip()
    {
        if (clipList.Count == 0) return null;
        if (clipList.Count == 1) return clipList[0];
        return clipList.PickRandom();
    }
}
