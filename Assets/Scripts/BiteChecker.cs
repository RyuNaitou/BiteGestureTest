using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiteChecker : MonoBehaviour
{
    public AudioSource audioSource;
    private float[] audioSamples;

    public int targetFrequency = 7700; // 例えば得られた最適周波数帯
    public float threshold = 0.04f; // しきい値（この値は適宜調整）

    public TMPro.TMP_Text frequencyText;
    public TMPro.TMP_Text threText;

    private float lastBiteTime;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        audioSamples = new float[1024];

        string selectedDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (string.IsNullOrEmpty(selectedDevice))
        {
            Debug.LogWarning("使用可能なマイクがありません。");
            return;
        }

        Debug.Log($"接続されたマイク：{selectedDevice}");

        // 録音開始
        audioSource.clip = Microphone.Start(selectedDevice, true, 10, 44100); // 44.1kHzサンプルレート
        if (audioSource.clip == null)
        {
            Debug.LogError("マイク録音の初期化に失敗しました。");
            return;
        }

        audioSource.loop = true;
        audioSource.mute = false;
        audioSource.Play();



        //// マイクからの入力を取得
        //audioSamples = new float[1024]; // サンプル数は適宜調整

        //if (Microphone.IsRecording(null))
        //{
        //    audioSource.clip = Microphone.Start(null, true, 10, 44100); // 10秒の録音を取得
        //    audioSource.loop = true;
        //    audioSource.Play();
        //}
        //else
        //{
        //    Debug.LogError("マイクが検出されません");
        //}
    }

    void Update()
    {
        // マイクの録音が終了していないかチェック
        if (Microphone.IsRecording(null))
        {
            // マイクからの音声データを取得
            //audioSource.GetOutputData(audioSamples, 0);
            ProcessAudioData(audioSamples);
        }
    }

    void ProcessAudioData(float[] samples)
    {
        // FFTを使用して周波数成分を抽出
        int sampleSize = samples.Length;
        float[] spectrum = new float[sampleSize];

        // FFTの処理
        //audioSource.GetOutputData(spectrum, 0);
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // スペクトルデータを元にエネルギー計算（これにより歯のかみ合わせ音を検出）
        DetectClench(spectrum);
    }

    void DetectClench(float[] spectrum)
    {
        // 設定された周波数帯としきい値を使用して音を検出

        // 周波数インデックスを計算
        int frequencyIndex = Mathf.FloorToInt(targetFrequency / (AudioSettings.outputSampleRate / spectrum.Length));

        // 指定した周波数帯のエネルギーを計算
        float frequencyEnergy = spectrum[frequencyIndex];

        frequencyText.SetText($"周波数({targetFrequency}Hz)のパワー：{frequencyEnergy:F4}");

        // しきい値を超えていれば、かみ合わせ音として検出
        if (frequencyEnergy > threshold)
        {
            Debug.Log("かみ合わせ音を検出しました！");
            threText.SetText("<color=#FF0000>検知");
            lastBiteTime = Time.time;
        }

        if(lastBiteTime + 0.5f < Time.time)
        {
            threText.SetText("<color=#000000>なし");
        }

    }

}
