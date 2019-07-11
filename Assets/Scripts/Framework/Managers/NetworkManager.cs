using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;

//
// 网络消息收发
//
public class NetworkManager : MN.SingletonMono<NetworkManager>, IManager {

    public IEnumerator Init(GameObject parent) {

        Debug.Log("Init NetworkManager");

        this.gameObject.transform.SetParent(parent.transform);

        yield return null;
    }

    public void Free() {

        Debug.Log("Free NetworkManager");
        GameObject.Destroy(this.gameObject);
    }

    void ProcessRequestResult(
                UnityWebRequest request,
                Action<string> onSuccess,
                Action<string> onFailed ) {

        if (request.isHttpError) {
            onFailed( "Http Error, code: " + request.error);
        } else if (request.isNetworkError) {
            onFailed(request.error);
        } else {
            onSuccess(request.downloadHandler.text);
        }
    }

    // 异步 Http Get 请求
    IEnumerator HttpGet(
                string url,
                Action<string> onSuccess,
                Action<string> onFailed) {

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        ProcessRequestResult(request, onSuccess, onFailed);
    }

    // 异步 Http Post 请求
    IEnumerator HttpPost(
                string url,
                string data,
                Action<string> onSuccess,
                Action<string> onFailed) {

        UnityWebRequest request = UnityWebRequest.Post(url, data);

        yield return request.SendWebRequest();

        ProcessRequestResult(request, onSuccess, onFailed);
    }

    // 提供给脚本的接口
    public void WebGet( string url,
                Action<string> onSuccess,
                Action<string> onFailed) {

        StartCoroutine(HttpGet(url, onSuccess, onFailed));
    }

    // 提供给脚本的接口
    public void WebPost( string url,
                string postData,
                Action<string> onSuccess,
                Action<string> onFailed) {

        StartCoroutine(HttpPost(url, postData, onSuccess, onFailed));
    }

}