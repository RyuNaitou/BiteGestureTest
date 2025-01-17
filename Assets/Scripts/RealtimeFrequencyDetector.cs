using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class RealtimeFrequencyDetector : MonoBehaviour
{
    public Button startButton;
    public TMP_Text startButtonText;
    public TMP_Text frequencyText;
    public TMP_Text powerText;
    public TMP_Text statusText;
    public TMP_Dropdown micDropdown;
    public AudioSource audioSource;
    public TMP_Text thresholdText;
    public Slider thresholdSlider;
    public Slider frequencySlider;
    public int sampleLength = 1024;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    public float targetFrequency = 800f; // 監視する周波数 (Hz)
    public float threshold = 0.0002f; // 閾値

    private bool isDetecting = false;
    private float[] spectrum;
    private string selectedMic = null;

    private float lastBiteTime;
    private float tempMaxPower;


    void Start()
    {
        //startButton.onClick.AddListener(StartDetection);
        //stopButton.onClick.AddListener(StopDetection);
        startButton.onClick.AddListener(ToggleDetection);
        spectrum = new float[sampleLength];
        PopulateMicrophoneDropdown();
        thresholdSlider.onValueChanged.AddListener(UpdateThreshold);
        frequencySlider.onValueChanged.AddListener(UpdateFrequency);
        UpdateThreshold(thresholdSlider.value);
        UpdateFrequency(frequencySlider.value);
    }

    void PopulateMicrophoneDropdown()
    {
        micDropdown.ClearOptions();
        string[] devices = Microphone.devices;
        if (devices.Length == 0)
        {
            micDropdown.options.Add(new TMP_Dropdown.OptionData("マイクが見つかりません"));
            micDropdown.interactable = false;
            return;
        }

        foreach (string device in devices)
        {
            micDropdown.options.Add(new TMP_Dropdown.OptionData(device));
        }
        micDropdown.interactable = true;
        micDropdown.onValueChanged.AddListener(delegate { selectedMic = micDropdown.options[micDropdown.value].text; });
        selectedMic = devices[0];
    }

    void Update()
    {
        if (isDetecting)
        {
            DetectFrequency();
        }
    }

    public void ToggleDetection()
    {
        if (isDetecting)
        {
            StopDetection();
        }
        else
        {
            StartDetection();
        }
    }

    void StartDetection()
    {
        if (string.IsNullOrEmpty(selectedMic))
        {
            Debug.LogWarning("使用可能なマイクがありません。");
            return;
        }

        audioSource.clip = Microphone.Start(selectedMic, true, 1, 44100);
        if (audioSource.clip == null)
        {
            Debug.LogError("マイク録音の初期化に失敗しました。");
            return;
        }

        audioSource.loop = true;
        audioSource.mute = false;
        audioSource.Play();

        isDetecting = true;
        statusText.text = "検知中...";
        startButtonText.text = "検知停止";
    }

    void StopDetection()
    {
        isDetecting = false;
        audioSource.Stop();
        Microphone.End(selectedMic);
        statusText.text = "検知停止中";
        startButtonText.text = "検知開始";
    }

    void DetectFrequency()
    {
        audioSource.GetSpectrumData(spectrum, 0, fftWindow);

        int index = Mathf.RoundToInt(targetFrequency * sampleLength / (AudioSettings.outputSampleRate / 2));
        float power = spectrum[index];
        
        frequencyText.SetText($"周波数({targetFrequency}Hz)のパワー：");
        powerText.SetText($"{power:F6}");
        
        if (index >= 0 && index < spectrum.Length && spectrum[index] > threshold)
        {
            TriggerAction(power);
        }
        else
        {
            unTriggerAction();
        }
    }

    void TriggerAction(float power)
    {
        Debug.Log("かみ合わせ音を検出しました！");
        Debug.Log($"周波数 {targetFrequency} Hz が閾値 {threshold} を超えました！");
        tempMaxPower = tempMaxPower < power ? power : tempMaxPower;
        statusText.text = $"<color=#FF0000>検知<color=#000000>: {tempMaxPower:F5}";
        lastBiteTime = Time.time;
    }


    void unTriggerAction()
    {
        if (lastBiteTime + 0.5f < Time.time)
        {
            statusText.SetText("<color=#000000>検知中...");
            tempMaxPower = 0;
        }
    }
    void UpdateThreshold(float value)
    {
        threshold = value * 0.00001f;
        thresholdText.text = $"{threshold:F5}";
    }

    void UpdateFrequency(float value)
    {
        targetFrequency = value * 100;
        frequencyText.SetText($"周波数({targetFrequency}Hz)のパワー：");
        //frequencyText.text = $"周波数: {targetFrequency:F0} Hz";
    }
}
