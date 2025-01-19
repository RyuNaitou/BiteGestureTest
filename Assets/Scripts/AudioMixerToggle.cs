using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioMixerToggle : MonoBehaviour
{
    public AudioSource audioSource;  // AudioSource
    public AudioMixerGroup mixer1;   // 1つ目の AudioMixerGroup
    public AudioMixerGroup mixer2;   // 2つ目の AudioMixerGroup
    public Toggle toggleSwitch;      // UIの Toggle
    //public Text toggleLabel;         // Toggle のラベル（ON/OFF表示）

    void Start()
    {
        // 初期状態を設定（Toggle が ON なら mixer1、OFF なら mixer2）
        audioSource.outputAudioMixerGroup = toggleSwitch.isOn ? mixer1 : mixer2;
        //UpdateToggleLabel(toggleSwitch.isOn);

        // Toggle の変更時に ToggleMixer を実行
        toggleSwitch.onValueChanged.AddListener(ToggleMixer);
    }

    void ToggleMixer(bool isOn)
    {
        // ON のとき mixer1、OFF のとき mixer2
        audioSource.outputAudioMixerGroup = isOn ? mixer1 : mixer2;
        //UpdateToggleLabel(isOn);
    }

    //void UpdateToggleLabel(bool isOn)
    //{
    //    // Toggle のラベルを変更
    //    toggleLabel.text = isOn ? "Mixer: 1 (ON)" : "Mixer: 2 (OFF)";
    //}
}
