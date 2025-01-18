using UnityEngine;

public class BiteDetection : MonoBehaviour
{
    public AudioSource audioSource;
    private const int sampleSize = 1024;
    private float[] spectrumData = new float[sampleSize];
    public float targetFrequency = 7700f;
    public float threshold = 0.04f;

    public TMPro.TMP_Text frequencyText;
    public TMPro.TMP_Text threText;

    private float lastBiteTime;


    void Start()
    {
        StartMicrophone();
    }

    void Update()
    {
        DetectBite();
    }

    void StartMicrophone()
    {
        audioSource = GetComponent<AudioSource>();
        string selectedDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (string.IsNullOrEmpty(selectedDevice))
        {
            Debug.LogWarning("使用可能なマイクがありません。");
            return;
        }

        Debug.Log($"接続されたマイク：{selectedDevice}");

        // 録音開始
        audioSource.clip = Microphone.Start(selectedDevice, true, 1, AudioSettings.outputSampleRate); // 44.1kHzサンプルレート
        Debug.Log($"サンプルレート：{AudioSettings.outputSampleRate}");
        if (audioSource.clip == null)
        {
            Debug.LogError("マイク録音の初期化に失敗しました。");
            return;
        }

        //audioSource.clip = Microphone.Start(null, true, 1, AudioSettings.outputSampleRate);
        audioSource.loop = true;
        while (!(Microphone.GetPosition(null) > 0)) { } // 待機
        audioSource.Play();
    }

    void DetectBite()
    {
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        int sampleRate = AudioSettings.outputSampleRate;
        float freqResolution = (sampleRate / 2f) / sampleSize;
        int targetIndex = Mathf.RoundToInt(targetFrequency / freqResolution);

        if (targetIndex >= 0 && targetIndex < sampleSize)
        {
            float power = spectrumData[targetIndex];
            //float power = spectrumData[targetIndex] * spectrumData[targetIndex];

            frequencyText.SetText($"周波数({targetFrequency}Hz)のパワー：{power:F6}");


            if (power >= threshold)
            {
                Debug.Log("かみ合わせ音を検出しました！");
                threText.SetText("<color=#FF0000>検知");
                lastBiteTime = Time.time;

            }

            if (lastBiteTime + 0.5f < Time.time)
            {
                threText.SetText("<color=#000000>なし");
            }

        }
    }
}
