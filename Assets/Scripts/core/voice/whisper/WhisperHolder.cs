// using System;
// using System.IO;
// using System.Linq;
// using System.Net;
// using System.Threading.Tasks;
// using UnityEngine;
// using Voice;
//
// public class WhisperHolder : MonoBehaviour {
//     public bool loadOnStart;
//     public WhisperManager whisper = null;
//     public static WhisperHolder instance;
//
//     [SerializeField] private string downloadUrl =
//         "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.en-q5_1.bin";
//
//     private void Awake() {
//         instance = this;
//         whisper = GetComponent<WhisperManager>();
//         DontDestroyOnLoad(gameObject);
//     }
//
//     private async void Start() {
//         if (!loadOnStart) return;
//
//         if (checkVM()) return;
//
//         whisper.ModelPath = await PrepareModel();
//         try {
//             await whisper.InitModel();
//         } catch {
//             Debug.LogWarning($"[WhisperHolder] Модель не проинициализирована!");
//         }
//     }
//
//     public static bool checkVM() {
//         return File.Exists($"C:\\Users\\Public\\Documents\\vm.detect");
//     }
//
//     private Task<string> PrepareModel() {
//         var fileName = downloadUrl.Split("/").Last();
//
//         var path = Path.Combine(Application.streamingAssetsPath, fileName);
//
//         if (File.Exists(path)) {
//             return Task.Factory.StartNew(() => {
//                 Debug.Log($"[WhisperHolder] Модель уже скачана");
//                 return fileName;
//             });
//         }
//
//         Debug.Log($"[WhisperHolder] Начинаю загрузку модели {downloadUrl}");
//         WebClient client = new WebClient();
//
//         return Task.Factory.StartNew(() => {
//             client.DownloadFileTaskAsync(downloadUrl, path).Wait();
//             Debug.Log($"[WhisperHolder] Модель загружена");
//             return fileName;
//         });
//     }
// }