using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.Timeline;


public class ModelDetector : MonoBehaviour
{
    public Button startButton;
    public TMP_Text startButtonText;
    public TMP_Text probabilityText;
    public TMP_Text statusText;
    public TMP_Dropdown micDropdown;
    public AudioSource audioSource;
    public TMP_Text probabilityThresholdText;
    public Slider probabilityThresholdSlider;

    public int sampleLength = 1024;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    public float probabilityThreshold = 0.5f; // 確率の閾値

    private bool isDetecting = false;
    private float[] spectrum;
    private string selectedMic = null;

    private float lastBiteTime;

    public NNModel modelAsset;
    private IWorker worker;


    void Start()
    {
        //startButton.onClick.AddListener(StartDetection);
        //stopButton.onClick.AddListener(StopDetection);
        startButton.onClick.AddListener(ToggleDetection);
        spectrum = new float[sampleLength];
        PopulateMicrophoneDropdown();
        probabilityThresholdSlider.onValueChanged.AddListener(UpdateProbabilityThreshold);
        UpdateProbabilityThreshold(probabilityThresholdSlider.value);

        // モデルをロード
        var model = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
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
            ModelDetect();
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

    void ModelDetect()
    {
        audioSource.GetSpectrumData(spectrum, 0, fftWindow);
        Debug.Log("Input Tensor Length: " + spectrum.Length); // サイズを確認

        // 正しいサイズであることを確認
        if (spectrum.Length != 32 * 32)
        {
            Debug.LogError("Error: Tensor size mismatch. Expected 1024 elements, got " + spectrum.Length);
        }

        // モデルから推論
        Tensor input = new Tensor(1, 32, 32, 1, spectrum);
        worker.Execute(input);
        Tensor output = worker.PeekOutput();

        float probability = output[0];  // かみ合わせ音の確率

        probabilityText.SetText($"噛み合わせ音の確率：{probability:F3}");

        if (probability > probabilityThreshold)
        {
            TriggerAction();
        }
        else
        {
            unTriggerAction();
        }

        input.Dispose();
        output.Dispose();




        //int index = Mathf.RoundToInt(targetFrequency * sampleLength / (AudioSettings.outputSampleRate / 2));
        //float power = spectrum[index];

        //frequencyText.SetText($"周波数({targetFrequency}Hz)のパワー：{power:F6}");

        //if (index >= 0 && index < spectrum.Length && spectrum[index] > threshold)
        //{
        //    TriggerAction(power);
        //}
        //else
        //{
        //    unTriggerAction();
        //}
    }

    void OnDestroy()
    {
        worker.Dispose();
    }

    void TriggerAction()
    {
        Debug.Log("かみ合わせ音を検出しました！");
        statusText.text = $"<color=#FF0000>検知<color=#000000>";
        lastBiteTime = Time.time;
    }


    void unTriggerAction()
    {
        if (lastBiteTime + 0.5f < Time.time)
        {
            statusText.SetText("<color=#000000>検知中...");
        }
    }
    void UpdateProbabilityThreshold(float value)
    {
        probabilityThreshold = value * 0.001f;
        probabilityThresholdText.text = $"{probabilityThreshold:F3}";
    }

}
