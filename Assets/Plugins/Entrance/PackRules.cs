using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

// 定义资源打包的规则
public class PackRules : MonoBehaviour {

    public string   AndroidPackName;        // 安卓包名

    public string[] StartupScene;           // 第一个加载的 Scene

    public string   LuaPath;                // 脚本文件目录
    public string   LuaBytecodePath;        // 编译后的 Lua 路径

    public string[] PackPaths;              // 需要打包的目录

    public string[] IgnorePaths;            // 忽略的路径
    public string[] IgnoreExterns;          // 忽略的扩展名
    public string[] IgnoreFilenames;        // 忽略的文件名

    public string[] SingleFileAB;           // 单独打包的文件集合
    public string[] AllFile2SingleAB;       // 路径下的每一个文件都单独打包
    public string[] Path2SingleAB;          // 路径下所有打成一个包
    public string[] AllPath2SingleAB;       // 路径下的每个目录单独打包

    const string ABExtern = ".ab";

    // 根据资源名找到对应的AB文件
    public string FindAssetBundlePath(string assetName) {

        string filename = Path.GetFileName(assetName);
        string path = assetName.Substring(0, assetName.Length - filename.Length - 1);

        StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;

        // lua 文件
        string luaprefix = "assets/" + LuaBytecodePath;
        if (path.StartsWith(luaprefix, IgnoreCase)) {

            string fullpath = "assets/" + LuaPath;

            int pos = assetName.IndexOf('/', luaprefix.Length + 1);
            if (pos == -1) {
                return (fullpath + ABExtern).ToLower();
            } else {
                return (assetName.Substring(0, pos).Replace(luaprefix, fullpath) + ABExtern).ToLower();
            }
        }


        // 单个文件 -> ab 文件
        if (SingleFileAB != null && SingleFileAB.Length > 0 ) {

            foreach (string rulefile in SingleFileAB) {
                if (string.Equals(assetName, rulefile, IgnoreCase)) {
                    return assetName + ABExtern;
                }
            }
        }

        // 目录下单个文件 -> ab 文件
        if (AllFile2SingleAB != null && AllFile2SingleAB.Length > 0) {
            foreach (string rule in AllFile2SingleAB) {
                if (string.Equals(path, rule, IgnoreCase)){
                    return assetName + ABExtern;
                }
            }
        }

        // 目录下所有文件 -> ab 文件
        if (Path2SingleAB != null && Path2SingleAB.Length > 0) {
            foreach (string rule in Path2SingleAB) {
                if (assetName.StartsWith(rule, IgnoreCase)) {
                    return (rule + ABExtern).ToLower();
                }
            }
        }

        // 目录下单个目录 -> ab 文件，不包括二级目录
        if (AllPath2SingleAB != null && AllPath2SingleAB.Length > 0) {
            foreach (string rule in AllPath2SingleAB) {

                if (!string.Equals(path, rule, IgnoreCase)
                    && assetName.StartsWith(rule, IgnoreCase)) {
                        int pos = assetName.IndexOf('/', rule.Length + 1);
                        return assetName.Substring(0, pos) + ABExtern;
                    }
            }
        }

        // 默认按目录打包
        return path + ABExtern;
    }
}
