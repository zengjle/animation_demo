using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;


// 资源打包，App打包
public class AppPacker {

    const string AB_TARGET_PATH = "AssetBundles";
    const string AB_GRAPH_FILE = "AssetBundleGraph.txt";
    const string CHECKSUM_ASSET_PATH = "Assets/checksum.asset";
    const string LUA_BYTECODE_EXT = ".bytes";
    const string BUILD_MACROS = "UNITY_POST_PROCESSING_STACK_V1;HOTFIX_ENABLE";

    public static bool DEVELOPMENT = false;
    public static bool USE_LUA_BYTECODE = false;
    public static BuildTargetGroup LastBuildTarget = BuildTargetGroup.Android;

    static List<string> Paths = new List<string>();
    static List<string> Files = new List<string>();

    static PackRules PackRules;

    // [MenuItem("AppPacker/AssetBundle/iOS")]
    [MenuItem("AppPacker/AssetBundle/Windows")]
    static void AssetBundle_Windows() {
        AssetBundle_Common(BuildTarget.StandaloneWindows);
    }

    [MenuItem("AppPacker/AssetBundle/Android")]
    static void AssetBundle_Android() {
        AssetBundle_Common(BuildTarget.Android);
    }

    [MenuItem("AppPacker/Build/Windows")]
    static void Build_Windows() {
        Build_Common(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows);
    }

    [MenuItem("AppPacker/Build/Android")]
    static void Build_Android() {
        Build_Common(BuildTargetGroup.Android, BuildTarget.Android);
    }

    // 统一的资源打包流程
    static void AssetBundle_Common(BuildTarget target){

        // 加载打包规则
        LoadPackRules();

        // 生成版本信息
        UpdateVersionInfo();

        // 打包
        GenerateAB(target);

        // 拷贝资源到目标路径
        CopyAB2TargetPath(target);

        // 刷新资源数据库
        AssetDatabase.Refresh();
    }

    // 统一编译流程
    static void Build_Common(BuildTargetGroup targetGroup, BuildTarget target){

        // 编译前的准备
        PrepareBuild(targetGroup);

        // 资源打包
        AssetBundle_Common(target);

        // 重新生成 XLua 代码
        CSObjectWrapEditor.Generator.ClearAll();
        CSObjectWrapEditor.Generator.GenAll();

        // 编译
        switch (target) {
            case BuildTarget.Android:
                CreateAPK();
            break;

            case BuildTarget.StandaloneWindows:
                CreateStandaloneWindows();
            break;

            default:
                Debug.LogError("Unsupport Build Target: " + target);
            break;
        }
    }

    static void LoadPackRules() {
        GameObject obj = Resources.Load("Entrance/PackRules") as GameObject;
        PackRules = obj.GetComponent<PackRules>();
    }

    static void UpdateVersionInfo() {
        string path = Application.dataPath + "/" + PackRules.LuaPath + "/Game/Constants/Version.lua";
        using (FileStream file = new FileStream(path, FileMode.Create)) {
            using (StreamWriter writer = new StreamWriter(file, new System.Text.UTF8Encoding(false))) {

                writer.WriteLine("--");
                writer.WriteLine("-- 本文件由打包程序自动生成");
                writer.WriteLine("-- 不要手动修改这个文件");
                writer.WriteLine("--");

                writer.WriteLine("");

                writer.WriteLine("BuildVersion = '" + PlayerSettings.bundleVersion + "'");

                writer.Flush();
            }
        }
    }

    static void GenerateAB(BuildTarget target) {

        DateTime startTime = DateTime.Now;
        DateTime stepTime = DateTime.Now;

        string path = GetDirStringByBuildTarget(target);

        Debug.Log("Generate AssetBundle start");


        #region Step 1: Clear AssetBundle Directory
        if (!Directory.Exists(AB_TARGET_PATH)) {
            Directory.CreateDirectory(AB_TARGET_PATH);
        }

        string assetBundlePath = AB_TARGET_PATH + "/" + path;
        if (!Directory.Exists(assetBundlePath)) {
            Directory.CreateDirectory(assetBundlePath);
        }

        Paths.Clear();
        Files.Clear();
        #endregion

        #region Step 2: Collect All Resource Files
        Debug.Log("Collect all files");
        stepTime = DateTime.Now;
        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
        Dictionary<string, List<string>> buildDetails = new Dictionary<string, List<string>>();

        FindAllFiles(Paths, Files, Application.dataPath);
        for (int i = 0; i < Files.Count; ++i) {

            AssetBundleBuild build = new AssetBundleBuild();
            build.assetBundleName = PackRules.FindAssetBundlePath(Files[i].ToLower());
            build.assetNames = new string[] { Files[i] };
            builds.Add(build);

            string abPath = assetBundlePath.ToLower() + "/" + build.assetBundleName;
            if (!buildDetails.ContainsKey(abPath)) {
                List<string> filenames = new List<string>();

                filenames.Add(Files[i]);
                buildDetails.Add(abPath, filenames);
            }
        }
        #endregion

        #region Step 3: Process Lua Scripts
        stepTime = DateTime.Now;
        string bytecodePath = Application.dataPath + "/" + PackRules.LuaBytecodePath;

        // 重新建立 lua bytecode 目录
        if (Directory.Exists(bytecodePath)) {
            Directory.Delete(bytecodePath, true);
        }
        Directory.CreateDirectory(bytecodePath);

        HandleLuaScripts(bytecodePath, builds, buildDetails, assetBundlePath);
        #endregion

        #region Step 4: Build AssetBundle
        stepTime = DateTime.Now;

        BuildAssetBundleOptions options = BuildAssetBundleOptions.DeterministicAssetBundle;
        options |= BuildAssetBundleOptions.ChunkBasedCompression;

        BuildPipeline.BuildAssetBundles(assetBundlePath, builds.ToArray(), options, target);
        #endregion

        #region Step 5: Generate BuildGraph File
        //TODO:
        #endregion

        #region Step 6: Generate Checksum File
        //TODO:
        #endregion

        CleanLuaBytecode(bytecodePath);

        string timeSpent = (DateTime.Now - startTime).ToString();
        Debug.Log("Generate AssetrBundle end. Time spent: " + timeSpent);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void CopyAB2TargetPath(BuildTarget target) {

        string targetpath = GetDirStringByBuildTarget(target);
        string srcpath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        srcpath += "/AssetBundles/" + targetpath;

        CopyFiles2TargetRecursive(srcpath, Application.dataPath + "/StreamingAssets", (string filename) => {
            return Path.GetExtension(filename) != ".manifest";
        });
    }

    // 查找出所有需要被打包的文件
    static void FindAllFiles(List<string> allFoundPathes, List<string> allFoundFiles, string basePath) {

        if (PackRules.PackPaths == null || PackRules.PackPaths.Length == 0) {
            Debug.LogError("Error: Empty PackPath found in PackRules, no files will be packing.");
            return;
        }

        StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;

        for (int i = 0; i < PackRules.PackPaths.Length; ++i)
        {
            string packedPath = basePath + "/" + PackRules.PackPaths[i];
            packedPath = packedPath.Replace('\\', '/');
            packedPath = packedPath.Trim();

            recursive(allFoundPathes, allFoundFiles, packedPath, (string path, bool isDirectory) => {

                // 忽略路径
                if (PackRules.IgnorePaths != null) {
                    for (int p = 0; p < PackRules.IgnorePaths.Length; ++p) {
                        if (path.StartsWith(PackRules.IgnorePaths[p], IgnoreCase)) {
                            return false;
                        }
                    }
                }

                // 按扩展名忽略
                if (!isDirectory && PackRules.IgnoreExterns != null) {

                    string ext = Path.GetExtension(path);
                    for (int p = 0; p < PackRules.IgnoreExterns.Length; ++p) {
                        if (string.Equals(ext, PackRules.IgnoreExterns[p], IgnoreCase)) {
                            return false;
                        }
                    }
                }

                // 按文件名忽略
                if (!isDirectory && PackRules.IgnoreFilenames != null) {
                    string filename = Path.GetFileName(path);
                    for (int p = 0; p < PackRules.IgnoreFilenames.Length; ++p) {
                        if (string.Equals(filename, PackRules.IgnoreFilenames[p], IgnoreCase)) {
                            return false;
                        }
                    }
                }

                return true;
            });
        }

        Debug.Log("Found path num " + allFoundPathes.Count);
        Debug.Log("Found file num " + allFoundFiles.Count);
    }

    // 遍历所有文件
    static void recursive(
            List<string> allFoundPathes,
            List<string> allFoundFiles,
            string basePath,
            Func<string, bool, bool> filter)
    {
        string[] names = Directory.GetFiles(basePath);

        // 遍历文件
        foreach (string filename in names) {
            string normalizeName = filename.Replace('\\', '/');
            normalizeName = normalizeName.Substring(normalizeName.IndexOf("Assets/"));

            if (filter != null && !filter(normalizeName, false)) {
                continue;
            }

            allFoundFiles.Add(normalizeName);
        }

        string[] dirs = Directory.GetDirectories(basePath);

        // 遍历目录
        foreach (string dir in dirs) {

            string relatedDir = dir.Replace('\\', '/');
            relatedDir = relatedDir.Substring(relatedDir.IndexOf("Assets/"));

            if (filter != null && !filter(relatedDir, true)) {
                continue;
            }

            allFoundPathes.Add(relatedDir);
            recursive(allFoundPathes, allFoundFiles, dir, filter);
        }
    }

    static void HandleLuaScripts(
            string bytecodePath,
            List<AssetBundleBuild> builds,
            Dictionary<string, List<string>> buildDetails,
            string assetBundlePath) {

        List<string> allFoundFiles = new List<string>();
        List<string> allFoundPathes = new List<string>();

        StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;
        string luapath = Application.dataPath + "/" + XLua.LuaEnv.LuaRelativeDir;

        // 使用 bytecode 对 Lua 进行编译
        if (USE_LUA_BYTECODE) {

            recursive(allFoundPathes, allFoundFiles, luapath, (string path, bool isDirectory) => {
                if (isDirectory) {
                    return true;
                }

                return path.EndsWith(".lua", IgnoreCase);
            });

            for (int i = 0; i < allFoundFiles.Count; ++i) {

                System.Diagnostics.Process compiler = new System.Diagnostics.Process();

                string targetPath = allFoundFiles[i].Replace(XLua.LuaEnv.LuaRelativeDir, PackRules.LuaBytecodePath);
                targetPath = targetPath.Replace(".lua", LUA_BYTECODE_EXT);

                string targetDir = Path.GetDirectoryName(targetPath).Replace("Assets/", "/");
                targetDir = Application.dataPath + targetDir;

                if (!Directory.Exists(targetDir)) {
                    Directory.CreateDirectory(targetDir);
                }

#if UNITY_EDITOR_OSX
                compiler.StartInfo.FileName = Application.dataPath + "/../Tools/luac";
#else
                compiler.StartInfo.FileName = Application.dataPath + "/../Tools/luac.exe";
#endif
                compiler.StartInfo.Arguments = "-o " + targetPath + " " + allFoundFiles[i];
                compiler.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                compiler.StartInfo.RedirectStandardOutput = true;
                compiler.StartInfo.UseShellExecute = false;
                compiler.StartInfo.CreateNoWindow = true;
                compiler.Start();
                UnityEngine.Debug.Log(compiler.StandardOutput.ReadToEnd());
                compiler.WaitForExit();
            }
        }  else {
            CopyFiles2TargetRecursive(luapath, bytecodePath, (string filename) => {
                return Path.GetExtension(filename) == ".lua";
            },
            (string filename) => {
                string ext = Path.GetExtension(filename);
                filename = filename.Substring(0, filename.LastIndexOf(ext)) + LUA_BYTECODE_EXT;
                return filename;
            });
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        recursive(allFoundPathes, allFoundFiles, bytecodePath, (string path, bool isDirectory) => {
            if (isDirectory) {
                return true;
            }

            return path.EndsWith(LUA_BYTECODE_EXT, IgnoreCase);
        });

        for (int i = 0; i < allFoundFiles.Count; ++i) {
            allFoundFiles[i] = allFoundFiles[i].ToLower();

            AssetBundleBuild build = new AssetBundleBuild();
            build.assetBundleName = PackRules.FindAssetBundlePath(allFoundFiles[i]);
            build.assetNames = new string[] { allFoundFiles[i] };

            builds.Add(build);

            string abPath = assetBundlePath.ToLower() + "/" + build.assetBundleName;
            if (!buildDetails.ContainsKey(abPath)) {
                List<string> filenames = new List<string>();

                filenames.Add(allFoundFiles[i]);
                buildDetails.Add(abPath, filenames);
            }
        }

        Debug.Log("Found Lua file num " + allFoundFiles.Count);

        for (int i = 0; i < allFoundFiles.Count; ++i) {
            Debug.Log("Packaged Lua file " + allFoundFiles[i]);
        }
    }

    // 将所有有效文件拷贝到目标路径
    static void CopyFiles2TargetRecursive(
            string srcPath,
            string destPath,
            Func<string, bool> filter = null,
            Func<string, string> nameProcesser = null) {

        srcPath = srcPath.Replace("\\", "/");
        DirectoryInfo dir = new DirectoryInfo(srcPath);
        FileSystemInfo[] fis = null;
        try {
            fis = dir.GetFileSystemInfos();
        } catch(Exception e) {
            Debug.LogError("Exception: {0}" + e.ToString());
            Debug.LogError("src: " + srcPath);
            Debug.LogError("dest: " + destPath);

        }


        foreach (FileSystemInfo fi in fis) {

            string destFile = destPath + "\\" + fi.Name;
            if (Application.platform == RuntimePlatform.OSXEditor) {
                destFile = destPath + "/" + fi.Name;
            }

            if (fi is DirectoryInfo) {
                if (!Directory.Exists(destFile)) {
                    Directory.CreateDirectory(destFile);
                }

                CopyFiles2TargetRecursive(fi.FullName, destFile, filter, nameProcesser);
            } else {

                if (filter != null && !filter(fi.FullName)) {
                    continue;
                }

                if (nameProcesser != null) {
                    destFile = nameProcesser(destFile);
                }

                File.Copy(fi.FullName, destFile, true);
            }
        }
    }

    static string GetDirStringByBuildTarget(BuildTarget target) {

        string path = "UnknowTarget";

        switch (target) {
            case BuildTarget.iOS:
                path = "iOS";
            break;

            case BuildTarget.Android:
                path = "Android";
            break;

            case BuildTarget.StandaloneWindows:
                path = "StandaloneWindows";
            break;

            default:
                Debug.LogError("Error: unsupport build target");
            break;
        }

        return path;
    }

    static void CleanLuaBytecode(string bytecodePath) {

        if (Directory.Exists(bytecodePath)) {
            Directory.Delete(bytecodePath, true);
        }

        string metafile = bytecodePath + ".meta";
        if (File.Exists(metafile)) {
            File.Delete(metafile);
        }
    }

    // 编译前的准备
    static void PrepareBuild(BuildTargetGroup target) {
        LastBuildTarget = target;

        string[] args = System.Environment.GetCommandLineArgs();
        foreach (string arg in args) {

            if (arg == "-development") {
                DEVELOPMENT = true;

            } else if (arg == "-bytecode") {
                USE_LUA_BYTECODE = true;
            }
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, BUILD_MACROS);
    }

    // 返回第一个启动场景路径
    static string[] GetStartupScenePaths() {
        return PackRules.StartupScene;
    }

    // 准备资源目录
    static string PreparePackageDir(BuildTarget target ) {

        string packagePath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1) + "Publishing";

        if (!Directory.Exists(packagePath)) {
            Directory.CreateDirectory(packagePath);
        }

        packagePath = packagePath + "/" + GetDirStringByBuildTarget(target);

        if (Directory.Exists(packagePath))
        {
            DirectoryInfo di = new DirectoryInfo(packagePath);

            foreach (FileInfo fi in di.GetFiles()) {
                fi.Delete();
            }

            foreach (DirectoryInfo dir in di.GetDirectories()) {
                dir.Delete(true);
            }

        } else {
            Directory.CreateDirectory(packagePath);
        }

        return packagePath;
    }


    // 建立 Android 安装包
    static void CreateAPK() {

        string packagePath = PreparePackageDir(BuildTarget.Android) + "/";

        BuildOptions options = BuildOptions.None;

        if (DEVELOPMENT) {
            options = BuildOptions.Development|BuildOptions.ConnectWithProfiler;
        }

        BuildPipeline.BuildPlayer(
            GetStartupScenePaths(),
            packagePath + PackRules.AndroidPackName,
            BuildTarget.Android,
            options );
    }

    // 建立 Windows 独立运行环境
    static void CreateStandaloneWindows() {

        string packagePath = PreparePackageDir(BuildTarget.StandaloneWindows);

        BuildOptions options = BuildOptions.None;

        if (DEVELOPMENT) {
            options = BuildOptions.Development|BuildOptions.ConnectWithProfiler;
        }

        BuildPipeline.BuildPlayer(
            GetStartupScenePaths(),
            packagePath + "/" + PlayerSettings.productName + ".exe",
            BuildTarget.StandaloneWindows,
            options );
    }

    // 导出 iOS 工程
    static void ExportiOSProject() {}

    // 导出安卓 Gradle 工程
    static void ExportGradleProject() {}

    // 建立 macOS 独立运行环境
    static void CreateStandaloneMacOS() {}

}
