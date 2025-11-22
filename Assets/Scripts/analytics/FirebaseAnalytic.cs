using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseAnalytic : MonoBehaviour {
    private string measurementId;
    private string apiSecret;
    private string clientId;
    private string sessionId;

    public static FirebaseAnalytic Instance { get; private set; }

    private void Awake() {
        var firstOpen = !PlayerPrefs.HasKey("analytic_clientId");
        clientId = PlayerPrefs.GetString("analytic_clientId", Guid.NewGuid().ToString());
        PlayerPrefs.SetString("analytic_clientId", clientId);

        sessionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        measurementId = FirebaseConfig.measurementId;
        apiSecret = FirebaseConfig.apiSecret;

        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }

        SendEvent("session_start", new Dictionary<string, object> {
            { "session_number", 1 },
        });
        if (firstOpen) {
            SendEvent("first_visit");
        }
    }

    public void SendEvent(string eventName, Dictionary<string, object> parameters = null) {
        StartCoroutine(SendEventCoroutine(eventName, parameters ?? new Dictionary<string, object>()));
    }

    private IEnumerator SendEventCoroutine(string eventName, Dictionary<string, object> parameters) {
        string url =
            $"https://www.google-analytics.com/mp/collect?measurement_id={measurementId}&api_secret={apiSecret}";

        parameters["session_id"] = sessionId;
        parameters["engagement_time_msec"] = 1000;

        var payload = new Dictionary<string, object> {
            { "client_id", clientId }, {
                "events", new List<object> {
                    new Dictionary<string, object> {
                        { "name", eventName },
                        { "params", parameters }
                    }
                }
            }
        };

        string json = MiniJson.Serialize(payload);

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) {
            Debug.Log($"GA4 OK: {eventName}");
        } else {
            Debug.LogError($"GA4 ERROR: {req.error}\n{req.downloadHandler.text}");
        }
    }
}