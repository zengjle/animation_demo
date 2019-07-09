using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 每个 scene 的默认脚本，如果没有加载 Game，会主动加载 Game
public class Main : MonoBehaviour {

    IEnumerator Start() {

        // 如果当前场景没有 Game，重新加载，保证可以单独运行每一个场景
        Game game = FindObjectOfType<Game>();
        if (game == null) {

            GameObject prefab = Resources.Load<GameObject>("Entrance/Game");
            if (prefab == null) {
                Debug.LogError("Load Game.prefab failed");
                yield break;
            }

            game = GameObject.Instantiate(prefab).GetComponent<Game>();
            game.onGameInited += onInited;
        } else {
            onInited();
        }
    }

    void onInited() {
        LuaManager.Instance.RunScriptMain();
    }

}
