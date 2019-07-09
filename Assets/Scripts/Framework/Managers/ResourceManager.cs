using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;

// Bundle 引用
public class BundleRef{

    public AssetBundle bundle;
    public int refNum;
    public AssetBundleCreateRequest request;
    public Coroutine loadCoroutine;
    public int waitNum = 0;

    public BundleRef() {
        bundle = null;
        refNum = 0;
        request = null;
        loadCoroutine = null;
        waitNum = 0;
    }

    public void Clear() {
        bundle = null;
        refNum = 0;
        request = null;
        loadCoroutine = null;
        waitNum = 0;
    }
}


public class ResourceManager : MN.SingletonMono<ResourceManager>, IManager {

    PackRules packRules;
    bool editorUseAssetBundle = true;
    Dictionary<string, string> file2AssetBundleMap = new Dictionary<string, string>();
    Dictionary<string, BundleRef> bundles = new Dictionary<string, BundleRef>();

    string baseDownloaderURL;
    AssetBundleManifest assetBundleManifest;

    public IEnumerator Init(GameObject parent) {

        this.gameObject.transform.SetParent(parent.transform);

#if UNITY_EDITOR
        editorUseAssetBundle = false;
        if (!editorUseAssetBundle) {
            yield break;
        }
#endif

        baseDownloaderURL = Application.streamingAssetsPath + "/";
        if (Application.platform == RuntimePlatform.Android){
            baseDownloaderURL = Application.dataPath + "!assets/";
        }

        string manifestPath;

        if (Application.platform == RuntimePlatform.Android) {
            manifestPath = baseDownloaderURL + "Android";
        } else if(Application.platform == RuntimePlatform.IPhonePlayer) {
            manifestPath = baseDownloaderURL + "iOS";
        } else {
            manifestPath = baseDownloaderURL + "StandaloneWindows";
        }

        // TODO: 处理更新补丁

        AssetBundle ab = AssetBundle.LoadFromFile(manifestPath);
        if (ab == null) {
            Debug.LogError("manifest not found!");
            yield break;
        }

        assetBundleManifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        ab.Unload(false);

        // 加载打包规则
        GameObject go = Resources.Load<GameObject>("Entrance/PackRules");
        packRules = go.GetComponent<PackRules>();

        yield return null;
    }

    public void Free() {
        GameObject.Destroy(this.gameObject);
    }

    //
    // 同步加载资源
    //
    public T LoadAsset<T>(string assetName) where T : UnityEngine.Object {

#if UNITY_EDITOR
        if (!editorUseAssetBundle) {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetName);
        }
#endif

        string bundleName = LookupAssetBundleName(assetName);
        AssetBundle bundle = LoadAssetBundle(bundleName);
        if (bundle == null) {
            Debug.LogError("Load asset from bundle failed, asset: " + assetName);
            return null;
        }

        return bundle.LoadAsset<T>(assetName);
    }

    //
    // 同步加载资源，结果通过 onFinished 返回
    //
    public void LoadAsset<T>(
                string assetName,
                Action<UnityEngine.Object> onFinished) where T : UnityEngine.Object {

        T asset = null;
#if UNITY_EDITOR
        if (!editorUseAssetBundle) {
            asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetName);
            if (onFinished != null){
                onFinished(asset);
            }

            return;
        }
#endif
        string bundleName = LookupAssetBundleName(assetName);
        AssetBundle bundle = LoadAssetBundle(bundleName);
        if (bundle != null) {
            asset = bundle.LoadAsset<T>(assetName);
        }

        if (onFinished != null) {
            onFinished(asset);
            onFinished = null;
        }
    }

    public void LoadAssetAsync<T>(
                    string assetName,
                    Action<UnityEngine.Object> onFinished) where T : UnityEngine.Object {

#if UNITY_EDITOR
        if (!editorUseAssetBundle) {
            LoadAsset<T>(assetName, onFinished);
            return;
        }
#endif

        string bundleName = LookupAssetBundleName(assetName);
        LoadAssetBundleAsync(bundleName, delegate (AssetBundle assetBundle) {
            StartCoroutine(LoadAssetCoroutine<T>(assetBundle, assetName, onFinished));
        });
    }

    // 协程载入Asset
    IEnumerator LoadAssetCoroutine<T>(
                AssetBundle assetBundle,
                string assetName,
                Action<UnityEngine.Object> onFinished) where T : UnityEngine.Object {

        if (assetBundle == null) {
            onFinished = null;
            yield break;
        }

        AssetBundleRequest request = assetBundle.LoadAssetAsync<T>(assetName);
        yield return request;

        if (onFinished != null){
            onFinished(request.asset);
            onFinished = null;
        }
    }

    public IEnumerator LoadSceneAsync(
            string path, string name,
            Action<AsyncOperation> onStarted,
            Action<float> onLoading,
            Action<AsyncOperation> onFinished,
            UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Single) {

        Debug.Log("Async load scene " + name);
        AsyncOperation ao;

#if UNITY_EDITOR
        if (editorUseAssetBundle) {
#endif
            AssetBundle bundle = LoadAssetBundle(string.Format("{0}.ab", path.ToLower()));
#if UNITY_EDITOR
        }
#endif
        ao = SceneManager.LoadSceneAsync(name, mode);
        if (onStarted != null) {
            onStarted(ao);
        }

        while (!ao.isDone && ao.progress < 0.85) {
            if (onLoading != null) {
                onLoading(ao.progress);
            }

            yield return null;
        }

        if (onFinished != null) {
            onFinished(ao);
        }

        Debug.Log("Load scene " + name + " end");
    }

    // 同步加载资源
    public GameObject LoadAsset(string assetName){
        return LoadAsset<GameObject>(assetName);
    }

    // 异步加载资源
    public void LoadAssetAsync(string assetName, Action<UnityEngine.Object> onFinished) {
        LoadAssetAsync<GameObject>(assetName, onFinished);
    }

    public void LoadAsset(string assetName, Action<UnityEngine.Object> onFinished){
        LoadAsset<GameObject>(assetName, onFinished);
    }


    // 根据资源名获取AssetBundle的名字
    public string LookupAssetBundleName(string assetName) {

        string normalizeName = assetName.ToLower();
        string bundlePath;

        if (!file2AssetBundleMap.TryGetValue(normalizeName, out bundlePath)) {
            bundlePath = packRules.FindAssetBundlePath(normalizeName);
            file2AssetBundleMap[normalizeName] = bundlePath;
        }

        return bundlePath;
    }

    // 同步载入AssetBundle
    public AssetBundle LoadAssetBundle(string bundleName) {

        BundleRef br = null;

        if (bundles.TryGetValue(bundleName, out br) && br.bundle != null) {
            br.refNum += 1;
            return br.bundle;
        }

        // 获取相关依赖，并载入
        string[] dependences = assetBundleManifest.GetAllDependencies(bundleName);
        for (int i = 0; i < dependences.Length; ++i) {
            br = null;

            if (string.IsNullOrEmpty(dependences[i])
                || string.IsNullOrEmpty(dependences[i].Trim())) {
                continue;
            }

            // 已经载入的依赖包，增加引用计数
            if (bundles.TryGetValue(dependences[i], out br) && br.bundle != null) {
                br.refNum += 1;
                continue;
            }

            // 优先获取的Patched路径
            string dependPath = GetLatestAssetBundlePath(dependences[i]);

            br = new BundleRef();
            br.bundle = AssetBundle.LoadFromFile(dependPath);
            br.refNum = 1;

            bundles[dependences[i]] = br;
        }

        string abPath = GetLatestAssetBundlePath(bundleName);

        br = new BundleRef();
        br.bundle = AssetBundle.LoadFromFile(abPath);
        br.refNum = 1;

        bundles[bundleName] = br;

        return br.bundle;
    }

    // 异步载入AssetBundle
    void LoadAssetBundleAsync(string bundleName, Action<AssetBundle> onFinished) {

        BundleRef br;
        if (!bundles.TryGetValue(bundleName, out br)) {
            br = new BundleRef();
            bundles[bundleName] = br;
        }

        // 没载入过，则启动异步载入
        if (br.loadCoroutine == null) {
            br.loadCoroutine = StartCoroutine(LoadAssetBundleCoroutine(bundleName, onFinished));
        } else {
            // 等待载入结束
            StartCoroutine(TillLoadAssetBundleEndCoroutine(br.loadCoroutine, bundleName, onFinished));
        }
    }

    // 协程载入AssetBundle
    IEnumerator LoadAssetBundleCoroutine(string bundleName, Action<AssetBundle> onFinished) {

        BundleRef br;

        if (bundles.TryGetValue(bundleName, out br) && br.bundle != null) {

            br.refNum += 1;

            if (onFinished != null) {
                onFinished(br.bundle);
                onFinished = null;
            }

            br.loadCoroutine = null;
            yield break;
        }

        string[] dependences = assetBundleManifest.GetAllDependencies(bundleName);
        List<BundleRef> asyncList = new List<BundleRef>();

        for (int i = 0; i < dependences.Length; ++i) {

            br = null;

            if (string.IsNullOrEmpty(dependences[i]) || string.IsNullOrEmpty(dependences[i].Trim())) {
                continue;
            }

            // 已经载入过了，增加计数
            if (bundles.TryGetValue(dependences[i], out br) && br.bundle != null) {
                br.refNum += 1;
                continue;
            }

            if (br == null) {
                br = new BundleRef();
                bundles[dependences[i]] = br;
            }

            if (br.request == null) {
                string dependPath = GetLatestAssetBundlePath(dependences[i]);
                br.request = AssetBundle.LoadFromFileAsync(dependPath);
            }

            asyncList.Add(br);
        }

        br = null;
        if (bundles.TryGetValue(bundleName, out br) && br.request == null) {
            string abPath = GetLatestAssetBundlePath(bundleName);
            br.request = AssetBundle.LoadFromFileAsync(abPath);
        }

        asyncList.Add(br);

        for (int i = 0; i < asyncList.Count; ++i) {

            br = asyncList[i];
            if (br.bundle != null) {
                continue;
            }

            if (br.request == null) {
                continue;
            }

            if (br.request.isDone) {
                br.bundle = br.request.assetBundle;
                continue;
            }

            br.waitNum += 1;
            if (br.waitNum > 1) {
                while (br.waitNum > 1 && !br.request.isDone) {
                    yield return null;
                }
            } else {
                yield return br.request;
            }

            br.waitNum -= 1;

            AssetBundle loadedAB = (br.bundle == null) ? br.request.assetBundle : br.bundle;
            br.bundle = loadedAB;
            br.refNum += 1;

            // 请求结束
            if (br.waitNum == 0) {
                br.request = null;
            }
        }

        if (onFinished != null) {
            onFinished(br.bundle);
            onFinished = null;
        }

        br.loadCoroutine = null;
        asyncList.Clear();
    }

    // 获取最新AssetBundle路径，如果pachted过，则优先返回patched的路径，否则返回默认路径
    string GetLatestAssetBundlePath(string bundleName) {

        string latestPath = PatchManager.Instance.GetPatchedAssetBundlePath(bundleName);
        if (string.IsNullOrEmpty(latestPath)) {
            latestPath = baseDownloaderURL + bundleName;
        }

        return latestPath;
    }

      // 协程等待载入AssetBundle结束
    IEnumerator TillLoadAssetBundleEndCoroutine(
            Coroutine co,
            string bundleName,
            Action<AssetBundle> onFinished) {

        BundleRef br = bundles[bundleName];
        while (br.loadCoroutine != null) {
            yield return null;
        }

        if (onFinished != null) {
            onFinished(br.bundle);
            onFinished = null;
        }
    }
}