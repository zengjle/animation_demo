using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// TODO: 支持打包后的资源读取

public class Entrance : MonoBehaviour {
    public Transform uiRoot;
    public GameObject splashRoot;
    public RawImage[] splashes;
    public GameObject welcomePrefab;

    bool switchSplashEnd = false;

#if UNITY_EDITOR
    bool useAssetBundle = false;         // 是否使用 Asset Bundle
#else
    bool useAssetBundle = true;
#endif

    GameObject gameLoader;              // 更新完成后的游戏加载器

    // GameLoader 是游戏真正的入口，在此之前是显示欢迎画面和更新客户端
    const string GAMELOADER_PATH = "assets/mnres/prefabs/gameloader.prefab";
    const string GAMELOADER_AB_PATH = GAMELOADER_PATH + ".ab";


    WWW www;
#if UNITY_EDITOR_WIN || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
    const string WWW_PREFIX = "file:///";
#elif UNITY_EDITOR || !UNITY_ANDROID
    const string WWW_PREFIX = "file://";
#else
    const string WWW_PREFIX = "";
#endif

    void Awake() {

        if (splashRoot != null ) {
            splashRoot.SetActive(true);
        }

        if (splashes != null ) {
            for ( int i = 0; i < splashes.Length; i++ ) {
                if (splashes[i] != null ) {
                    splashes[i].gameObject.SetActive( i == 0 );                 // 隐藏除第一个以外的其它 splashes
                }
            }

            StartCoroutine(SwitchSplashs());

        } else {
            switchSplashEnd = true;
        }

        StartCoroutine(ShowWelcome());
    }

    // 轮流播放启动画面
    IEnumerator SwitchSplashs() {
        int index = 0;
        while (index < splashes.Length) {
            splashes[index].gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);

            Color clr = splashes[index].color;
            while (clr.a > 0.05f) {
                clr.a -= 0.05f;
                splashes[index].color = clr;

                yield return new WaitForSeconds(0.05f);
            }

            splashes[index].gameObject.SetActive(false);
            index++;
        }

        switchSplashEnd = true;
    }

    IEnumerator ShowWelcome() {
        yield return null;

        while (!switchSplashEnd) {
            yield return null;
        }

        if (splashRoot != null) {
            splashRoot.gameObject.SetActive(false);
        }

        // 显示 welcome 画面
        if (welcomePrefab != null && uiRoot != null) {
            GameObject welcome = GameObject.Instantiate(welcomePrefab);

            welcome.transform.SetParent(uiRoot, false);

            // welcome 中会按规则更新当前客户端
            Patcher patcher = welcome.GetComponent<Patcher>();
            if ( patcher != null ) {
                patcher.onPatchFinished += OnPatchEnd;
            }

        } else {
            Debug.LogError("welcome prefab and/or uiRoot invalid");
            yield break;
        }
    }

    void OnPatchEnd(int code) {
        // Debug.Log("OnPatchEnd");

        StartCoroutine(LoadGameLoader());
    }

    IEnumerator LoadGameLoader() {

        GameObject prefab = null;

        if (useAssetBundle) {

            //TODO:
            www = new WWW(WWW_PREFIX + Application.streamingAssetsPath + "/" + GAMELOADER_AB_PATH);
            yield return www;
            yield return null;

            AssetBundle bundle = www.assetBundle;
            if (bundle == null ) {
                bundle = AssetBundle.LoadFromMemory(www.bytes);
            }

            if (bundle != null) {
                prefab = bundle.LoadAsset<GameObject>(GAMELOADER_PATH);
                bundle.Unload(false);
            }

        } else {

#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(GAMELOADER_PATH);
#endif
        }

        if (prefab == null) {
            Debug.LogError("Load GameLoader prefab failed");
            yield break;
        }

        gameLoader = GameObject.Instantiate(prefab);

        yield return null;
    }

    private void OnDestory() {
        GameObject.Destroy(gameLoader);
    }
}
