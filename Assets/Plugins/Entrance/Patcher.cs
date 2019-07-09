using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Patcher : MonoBehaviour {
    public delegate void OnPatchFinished(int code);

    public Slider progress;
    public OnPatchFinished onPatchFinished;

    void Awake() {
        progress.value = 0.0f;
        StartCoroutine(FakeLoading());
    }

    // 模拟更新
    IEnumerator FakeLoading() {
        while (progress.value < 1f) {
            progress.value += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (onPatchFinished != null) {
            onPatchFinished(0);
        }
    }
}
