using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System;

public class BiteFrequencyRecorder : MonoBehaviour
{
    public Button startButton;
    public TMPro.TMP_Text instructionText;
    public TMPro.TMP_Text timerText;
    public AudioSource audioSource;
    public Dropdown micDropdown;  // 追加: マイク選択用のDropdown
    public string outputFileName = "BiteFrequencyData.csv";
    public int sampleLength = 1024;
    public FFTWindow fftWindow = FFTWindow.Hamming;

    private bool isRecording = false;
    private float elapsedTime = 0f;
    private float recordDuration = 5f;

    private StringBuilder csvContent;
    private float[] spectrum;
    private string selectedMic = null; // 追加: 選択されたマイク名

    void Start()
    {
        startButton.onClick.AddListener(StartRecording);
        instructionText.SetText("準備完了。録音を開始してください。");
        timerText.SetText("経過時間: 0.0 秒");
        spectrum = new float[sampleLength];
        csvContent = new StringBuilder();

        // マイク一覧を取得
        PopulateMicrophoneDropdown();

        csvContent.Append("times(s)");
        for (int i = 0; i < sampleLength; i++)
        {
            float frequency = i * AudioSettings.outputSampleRate / 2 / sampleLength;
            csvContent.Append($",{frequency:F0}");
        }
        csvContent.AppendLine();
    }

    // 追加: マイク一覧を取得し、Dropdown にセット
    void PopulateMicrophoneDropdown()
    {
        micDropdown.ClearOptions();
        string[] devices = Microphone.devices;
        if (devices.Length == 0)
        {
            micDropdown.options.Add(new Dropdown.OptionData("マイクが見つかりません"));
            micDropdown.interactable = false;
            return;
        }

        foreach (string device in devices)
        {
            micDropdown.options.Add(new Dropdown.OptionData(device));
        }
        micDropdown.interactable = true;
        micDropdown.onValueChanged.AddListener(delegate { selectedMic = micDropdown.options[micDropdown.value].text; });

        // 初期選択
        selectedMic = devices[0];
    }

    void StartRecording()
    {
        if (string.IsNullOrEmpty(selectedMic))
        {
            Debug.LogWarning("使用可能なマイクがありません。");
            return;
        }

        // 選択したマイクで録音
        audioSource.clip = Microphone.Start(selectedMic, true, Mathf.CeilToInt(recordDuration), 44100);
        if (audioSource.clip == null)
        {
            Debug.LogError("マイク録音の初期化に失敗しました。");
            return;
        }

        audioSource.loop = true;
        audioSource.mute = false;
        audioSource.Play();

        isRecording = true;
        elapsedTime = 0f;
        instructionText.text = "録音中...";
    }
}
