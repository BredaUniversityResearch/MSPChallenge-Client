using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioMain : MonoBehaviour
{
    // Audio IDs are stored as strings so references don't break if sounds are removed or new sounds are added

    // NOTE: New sounds should be added to the AudioIDs list below before they can be used!
    public const string VOLUME_TEST = "Volume Test";
    public const string MESSAGE_RECEIVED = "Message Received";
    public const string NOTIFICATION_RECEIVED = "Notification Received";
    public const string ITEM_PLACED = "Item Placed";
    public const string ITEM_MOVED = "Item Moved";

    public static List<string> AudioIDs = new List<string>()
    {
        VOLUME_TEST,
        MESSAGE_RECEIVED,
        NOTIFICATION_RECEIVED,
        ITEM_PLACED,
        ITEM_MOVED
    };

    private static Dictionary<string, AudioSource> audioSources;

    public static void PlaySound(AudioSource audioSource)
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    public static void PlaySound(string audioID)
    {
        AudioSource audioSource = GetAudioSource(audioID);
        audioSource.Play();
    }

    public static AudioSource GetAudioSource(string audioID)
    {
        if (audioSources != null && !audioSources.ContainsKey(audioID))
        {
            Debug.LogError("Unknown Audio ID: '" + audioID + "'");
            return null;
        }
        else
        {
            return audioSources[audioID];
        }
    }

    void Start()
    {
		audioSources = new Dictionary<string, AudioSource>();
		foreach (string audioID in AudioIDs)
        {
            Transform t = transform.Find(audioID);
            if (t == null)
            {
                Debug.LogError("Missing Audio Source: Please create an Audio Source with name '" + audioID + "'");
                continue;
            }

            AudioSource audioSource = t.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("Missing Audio Source: GameObject with name '" + audioID + "' does not have an Audio Source component");
                continue;
            }

            audioSources[audioID] =  audioSource;
        }
    }
}
