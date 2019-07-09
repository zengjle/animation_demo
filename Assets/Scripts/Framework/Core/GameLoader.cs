using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 加载游戏的第一个 scene，更新完成后第一个运行的脚本，绑定在 GameLoader prefab 上
public class GameLoader : MonoBehaviour {

    void Awake() {
        Debug.Log("Game Loader Awake");

        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame() {

        Debug.Log("Start to load Game");

        GameObject prefab = Resources.Load<GameObject>("Entrance/Game");
        if (prefab == null) {
            Debug.LogError("Load Game Prefab failed");
            yield break;
        }

        Game game = GameObject.Instantiate(prefab).GetComponent<Game>();
        game.onGameInited += onGameInited;
    }

    void onGameInited() {
        StartCoroutine(ResourceManager.Instance.LoadSceneAsync("Assets/Scenes/Login.unity", "Login", null, null, null));
    }
}
