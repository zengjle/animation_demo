using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatchManager : MN.SingletonMono<PatchManager>, IManager {
    XLua.LuaEnv luaEnv;

    public IEnumerator Init(GameObject parent) {

        Debug.Log("Init PatchManager");

        this.gameObject.transform.SetParent(parent.transform);

        yield return null;
    }

    public void Free() {

        Debug.Log("Free PatchManager");
        GameObject.Destroy(this.gameObject);
    }

    public string PatchAssetBundleManifestPath {
        get {
#if UNITY_ANDROID
            return Application.persistentDataPath + "/patch/Android";
#elif UNITY_IPHONE
            return Application.persistentDataPath + "/patch/iOS";
#else
            return Application.persistentDataPath + "/patch/StandaloneWindows";
#endif
        }
    }

    public string GetPatchedAssetBundlePath(string bundleName) {
        return null;
    }
}