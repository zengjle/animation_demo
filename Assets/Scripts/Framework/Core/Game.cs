using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 运行游戏，初始化游戏运行过程中的各个 Managers
public class Game : MonoBehaviour {
        public delegate void OnGameInited();
        public OnGameInited onGameInited;

        List<IManager> managers = new List<IManager>();

        void Awake() {
            DontDestroyOnLoad(this);
        }

        IEnumerator Start() {

            RegisterManagers();

            yield return InitManagers();
            yield return null;

            if (onGameInited != null) {
                onGameInited();
            }
        }

        void OnDestroy() {
            FreeManagers();
        }

        void RegisterManagers() {

            managers.Add(ResourceManager.Instance);
            managers.Add(CoroutineManager.Instance);
            managers.Add(PatchManager.Instance);
            managers.Add(Comon.Instance);
                        // 最后添加 LuaManager
            managers.Add(LuaManager.Instance);
        }

        IEnumerator InitManagers() {
            Debug.Log("Init Managers");

            for (int i = 0; i < managers.Count; i++) {
                yield return managers[i].Init(this.gameObject);
            }
        }

        void FreeManagers() {
            for (int i = managers.Count - 1; i >= 0; i--) {
                managers[i].Free();
            }

            Debug.Log("Free Managers");
        }
}
