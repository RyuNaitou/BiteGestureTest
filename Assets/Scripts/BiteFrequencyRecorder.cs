using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Text;
using System;

public class BiteFrequencyRecorder : MonoBehaviour
{
    public Button startButton;        // 録音開始ボタン
    public TMPro.TMP_Text instructionText;     // 合図を表示するテキスト
    public TMPro.TMP_Text timerText;           // 経過秒数を表示するテキスト
    public AudioSource audioSource;  // AudioSourceコンポーネント
    public TMP_Dropdown micDropdown; // 追加: マイク選択用のDropdown
    public string outputFileName = "BiteFrequencyData.csv"; // 出力するCSVファイル名
    public int sampleLength = 1024;  // スペクトルデータのサンプル数
    public FFTWindow fftWindow = FFTWindow.Hamming; // FFTウィンドウ関数

    private bool isRecording = false;
    private float elapsedTime = 0f;
    private float recordDuration = 5f; // 録音時間

    int number = 0;

    private StringBuilder csvContent;
    private float[] spectrum;
    private string selectedMic = null; // 追加: 選択されたマイク名

    void Start()
    {
        setupSaveOption();

        startButton.onClick.AddListener(StartRecording);
        instructionText.SetText("準備完了。録音を開始してください。");
        timerText.SetText("経過時間: 0.0 秒");
        spectrum = new float[sampleLength];
        csvContent = new StringBuilder();

        // マイク一覧を取得
        PopulateMicrophoneDropdown();

        // ヘッダー行を作成
        csvContent.Append("times(s)");
        for (int i = 0; i < sampleLength; i++)
        {
            float frequency = i * AudioSettings.outputSampleRate / 2 / sampleLength;
            csvContent.Append($",{frequency:F0}");
        }
        csvContent.AppendLine();
    }

    void setupSaveOption()
    {
        //iCloudバックアップ不要設定
        UnityEngine.iOS.Device.SetNoBackupFlag(Application.persistentDataPath);
        //iOS   : /var/mobile/Containers/Data/Application/<guid>/Documents/Product名/hoge/
        //MacOS : /Users/user名/Library/Application Support/DefaultCompany/Product名/hoge/
    }


    // マイク一覧を取得し、Dropdown にセット
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

        // 初期選択
        //selectedMic = devices[0];
        selectedMic = devices[devices.Length-1];    // 最後の要素(Bluetoothマイクなど)

        // selectedMic を TMP_Dropdown の初期選択に設定
        int selectedIndex = Array.IndexOf(devices, selectedMic);
        if (selectedIndex >= 0)
        {
            micDropdown.value = selectedIndex;
        }

        micDropdown.onValueChanged.AddListener(delegate { selectedMic = micDropdown.options[micDropdown.value].text; });

    }

    void Update()
    {
        if (isRecording)
        {
            elapsedTime += Time.deltaTime;
            timerText.text = $"経過時間: {elapsedTime:F1} 秒";

            // 噛み合わせタイミングの指示
            if (Mathf.FloorToInt(elapsedTime) == 2)
            {
                instructionText.SetText("1回目の噛み合わせ!");
            }
            else if (Mathf.FloorToInt(elapsedTime) == 4)
            {
                instructionText.SetText("2回目の噛み合わせ!");
            }
            else if (elapsedTime >= recordDuration)
            {
                EndRecording();
            }
            SaveFrequencyData(elapsedTime);
        }
    }

    void StartRecording()
    {
        if (string.IsNullOrEmpty(selectedMic))
        {
            Debug.LogWarning("使用可能なマイクがありません。");
            return;
        }

        // 選択したマイクで録音
        audioSource.clip = Microphone.Start(selectedMic, true, Mathf.CeilToInt(recordDuration), 44100); // 44.1kHzサンプルレート
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

    void EndRecording()
    {
        isRecording = false;
        instructionText.SetText("録音終了。解析中...");
        timerText.SetText("経過時間: 0.0 秒");

        // マイク録音停止
        audioSource.Stop();
        Microphone.End(selectedMic);

        // WAV ファイルとして録音データを保存
        SaveRecordingToWAV();

        // CSVデータを保存
        string filePath = Path.Combine(Application.persistentDataPath, outputFileName);
        File.WriteAllText(filePath, csvContent.ToString());
        instructionText.SetText($"解析結果を {filePath} に保存しました。");
    }

    //void SaveFrequencyData(float time)
    //{

    //    // 周波数成分を取得
    //    //audioSource.GetSpectrumData(spectrum, 0, fftWindow);
    //    audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

    //    // 時間とスペクトルデータをCSVに追加
    //    csvContent.Append($"{time}");
    //    foreach (var amplitude in spectrum)
    //    {
    //        csvContent.Append($",{amplitude}");
    //    }
    //    csvContent.AppendLine();
    //}

    void SaveFrequencyData(float currentTime)
    {
        if (audioSource.clip == null)
        {
            Debug.LogError("AudioSource.clip が設定されていません。録音が正しく行われていない可能性があります。");
            return;
        }

        // 録音位置を確認（録音が進行しているか確認）
        int micPosition = Microphone.GetPosition(selectedMic);
        if (micPosition <= 0)
        {
            Debug.LogError("録音データが進行していません。録音位置の確認が必要です。");
            return;
        }

        Debug.Log($"現在の録音位置: {micPosition}");

        // FFTデータ取得
        if (spectrum == null || spectrum.Length != 1024)
        {
            spectrum = new float[1024]; // FFTサイズを1024に設定
        }

        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // スペクトルデータの確認
        if (spectrum.Length > 0)
        {
            Debug.Log($"スペクトルデータ（最初の値）: {spectrum[0]}");
        }
        else
        {
            Debug.LogWarning("スペクトルデータが取得できませんでした。");
        }

        // CSVにデータを保存
        StringBuilder row = new StringBuilder();
        row.Append($"{currentTime}");
        foreach (float amplitude in spectrum)
        {
            row.Append($",{amplitude}");
        }
        csvContent.AppendLine(row.ToString());
    }

    void SaveRecordingToWAV()
    {
        if (audioSource.clip == null)
        {
            Debug.LogWarning("録音データが存在しません。保存をキャンセルします。");
            return;
        }

        string path = Application.persistentDataPath + $"/Recording_{number++}.wav";
        Debug.Log($"録音データを保存中: {path}");

        // AudioClip を WAV ファイルに変換して保存
        SaveWAV(path, audioSource.clip);
        Debug.Log("録音データの保存が完了しました。");
    }

    public static void SaveWAV(string filePath, AudioClip clip)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] wavFile = ConvertToWAV(samples, clip.channels, clip.frequency);

        System.IO.File.WriteAllBytes(filePath, wavFile);
    }

    // WAVファイルへの変換
    private static byte[] ConvertToWAV(float[] samples, int channels, int sampleRate)
    {
        int headerSize = 44; // WAVヘッダーのサイズ
        int byteRate = sampleRate * channels * 2; // サンプルレート * チャンネル数 * 16ビット(2バイト)
        int fileSize = samples.Length * 2 + headerSize;

        using (var memoryStream = new System.IO.MemoryStream(fileSize))
        {
            // WAVヘッダー
            memoryStream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
            memoryStream.Write(BitConverter.GetBytes(fileSize - 8), 0, 4);
            memoryStream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);
            memoryStream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
            memoryStream.Write(BitConverter.GetBytes(16), 0, 4); // Subchunk1Size
            memoryStream.Write(BitConverter.GetBytes((short)1), 0, 2); // AudioFormat
            memoryStream.Write(BitConverter.GetBytes((short)channels), 0, 2);
            memoryStream.Write(BitConverter.GetBytes(sampleRate), 0, 4);
            memoryStream.Write(BitConverter.GetBytes(byteRate), 0, 4);
            memoryStream.Write(BitConverter.GetBytes((short)(channels * 2)), 0, 2); // BlockAlign
            memoryStream.Write(BitConverter.GetBytes((short)16), 0, 2); // BitsPerSample

            // Dataチャンク
            memoryStream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
            memoryStream.Write(BitConverter.GetBytes(samples.Length * 2), 0, 4);

            // PCMデータ
            foreach (var sample in samples)
            {
                short value = (short)(sample * short.MaxValue);
                memoryStream.Write(BitConverter.GetBytes(value), 0, 2);
            }

            return memoryStream.ToArray();
        }
    }
}
