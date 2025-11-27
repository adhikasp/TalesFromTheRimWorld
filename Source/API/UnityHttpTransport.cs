using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Unity-based HTTP transport using UnityWebRequest and coroutines.
    /// This transport is non-blocking and runs on Unity's main thread.
    /// </summary>
    public class UnityHttpTransport : IHttpTransport
    {
        /// <summary>
        /// Send a POST request with JSON body using Unity's coroutine system.
        /// The callback is invoked on the main thread when the request completes.
        /// </summary>
        public void PostJson(string jsonBody, HttpTransportConfig config, Action<HttpTransportResult> onComplete)
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                Current.Root.StartCoroutine(PostJsonCoroutine(jsonBody, config, onComplete));
            });
        }
        
        private IEnumerator PostJsonCoroutine(string jsonBody, HttpTransportConfig config, Action<HttpTransportResult> onComplete)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            
            using (UnityWebRequest www = new UnityWebRequest(config.ApiUrl, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {config.ApiKey}");
                www.SetRequestHeader("HTTP-Referer", config.Referer);
                www.SetRequestHeader("X-Title", config.Title);
                www.timeout = config.TimeoutSeconds;
                
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    onComplete?.Invoke(HttpTransportResult.Fail($"Connection failed: {www.error}", 0));
                    yield break;
                }
                
                if (www.result == UnityWebRequest.Result.ProtocolError)
                {
                    onComplete?.Invoke(HttpTransportResult.Fail(www.error, (int)www.responseCode));
                    yield break;
                }
                
                onComplete?.Invoke(HttpTransportResult.Ok(www.downloadHandler.text));
            }
        }
    }
}





