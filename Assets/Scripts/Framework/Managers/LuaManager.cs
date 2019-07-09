using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class LuaManager : MN.SingletonMono<LuaManager>, IManager {
    XLua.LuaEnv luaEnv;

    // TODO：使用 PackRules 确定 Bytecode 目录

    bool useAssetBundle = true;

    public IEnumerator Init(GameObject parent) {
        Debug.Log("Init LuaManager");

#if UNITY_EDITOR
        useAssetBundle = false;
#endif
        this.gameObject.transform.SetParent(parent.transform);

        initXLua();

        yield return null;
    }

    public void Free() {
        freeXLua();
        Debug.Log("Free LuaManager");
        GameObject.Destroy(this.gameObject);
    }

    void initXLua() {
        Debug.Log("Init XLua");

        luaEnv = new XLua.LuaEnv();
        XLua.Utils.Lua = luaEnv;

        luaEnv.AddBuildin("rapidjson", XLua.LuaDLL.Lua.LoadRapidJson);

        luaEnv.AddLoader((ref string path) => {
            string realpath = path.Replace('.', '/');

            if (useAssetBundle) {

                if (!realpath.Contains("xlua/")) {
                    realpath = ("assets/bytecode/" + realpath + ".bytes").ToLower();
                    string abName = ResourceManager.Instance.LookupAssetBundleName(realpath);
                    AssetBundle bundle = ResourceManager.Instance.LoadAssetBundle(abName);
                    TextAsset ta = bundle.LoadAsset<TextAsset>(realpath);
                    if (ta != null) {
                        return ta.bytes;
                    }
                }

            } else {
                realpath = XLua.LuaEnv.LuaDir + "/" + realpath + ".lua";
                if (File.Exists(realpath)) {
                    return File.ReadAllBytes(realpath);
                }
            }

            return null;

        });
    }

    void freeXLua() {
        luaEnv.FullGc();
        luaEnv = null;
        Debug.Log("free XLua");
    }

    public void RunScriptMain() {
        if (luaEnv != null ) {

            string path;

            if (useAssetBundle){
                path = "assets/bytecode/main.bytes";
            } else {
                path = "Assets/" + XLua.LuaEnv.LuaRelativeDir + "/" + "Main.lua";
            }

            luaEnv.DoString(@"require 'Main'", path);
        }
    }

    public void GetFunction<T>( string tablename, string name, ref T action) {

        if (luaEnv != null) {
            XLua.LuaTable table = null;

            luaEnv.Global.Get(tablename, out table);
            if (table != null) {
                table.Get(name, out action);
            }
        }
    }

    public byte[] LoadScript(string path) {

        if (useAssetBundle){
            path = ("assets/bytecode/" + System.IO.Path.ChangeExtension(path, ".bytes")).ToLower();
            string abName = ResourceManager.Instance.LookupAssetBundleName(path);
            AssetBundle ab = ResourceManager.Instance.LoadAssetBundle(abName);
            TextAsset ta = ab.LoadAsset<TextAsset>(path);
            if (ta != null) {
                return ta.bytes;
            }
        } else {
            string realpath = XLua.LuaEnv.LuaDir + "/" + path;
            if (File.Exists(realpath)) {
                return File.ReadAllBytes(realpath);
            }
        }

        return null;
    }

    public bool CheckScriptFileExisted(string path) {
        string realpath = path.Replace('.', '/');
        if (!useAssetBundle) {
            return File.Exists(XLua.LuaEnv.LuaDir + "/" + realpath + ".lua");
        }

        realpath = ("assets/bytecode/" + realpath + ".bytes").ToLower();

        string abName = ResourceManager.Instance.LookupAssetBundleName(realpath);
        AssetBundle ab = ResourceManager.Instance.LoadAssetBundle(abName);

        return ab != null && ab.Contains(realpath);
    }
}