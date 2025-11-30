using UnityEngine;

public class TTSManager : MonoBehaviour {
    public void Speak(string text) {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
            var currentActivity = activity.GetStatic<AndroidJavaObject>("currentActivity");
            var plugin = new AndroidJavaObject("com.example.ttsplugin.TTSPlugin"); // if you build plugin
            plugin.Call("speak", text);
        }
#else
        Debug.Log("TTS: " + text);
#endif
    }
}
