using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

namespace XLua
{
    public class LuaTarget : MonoBehaviour
    {
        private bool appQuitting = false;
        private LuaTable table;

        public int LuaReference {
            get {
                if (table != null)
                {
                    return table.LuaReference;
                }

                return 0;
            }
        }

        public LuaTable Table {
            get {
                return table;
            }
        }

        public bool Valid {
            get {
                return table != null;
            }
        }

        public string luaFilename;

        [SerializeField]
        private byte version = 1; // LuaTarget版本号

        [SerializeField]
        private UnityEngine.Object[] savedObjects;

        [SerializeField]
        private float[] savedSingles;

        [SerializeField]
        private string[] savedStrings;

        [SerializeField]
        private int[] savedIndexes;

        private Action<LuaTable> awakeAction;
        private Action<LuaTable> startAction;
        private Action<LuaTable> updateAction;
        private Action<LuaTable> onEnableAction;
        private Action<LuaTable> onDisableAction;

        private byte state = 0;

        const int stateAwake = 1;
        const int stateStart = 2;
        const int stateEnable = 3;

        public Dictionary<string, object> savedValues {
            get {
                try
                {
                    return LoadValues();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("load saved values error in file: " + luaFilename + ", error = " + e.Message);
                    return new Dictionary<string, object>();
                }
            }

            set {
                SaveValues(value);
            }
        }

        protected void Awake()
        {
            LoadLua(true);
            runNow(table.Get<string, Action<LuaTable>>("AwakeCS"));
            StartCoroutine(runEndFrame(internalInit, null));
        }

        protected void Start()
        {
            runNow(table.Get<string, Action<LuaTable>>("StartCS"));
        }

        protected void OnEnable()
        {
            if (getState(stateAwake))
            {
                runNow(onEnableAction);
            }
        }

        protected void OnDisable()
        {
            if (!getState(stateAwake))
            {
                internalInit(table);
            }

            StopAllCoroutines();

            runNow(onDisableAction);
            unsetState(stateEnable);
        }

        protected void Update()
        {
            if (updateAction != null && !appQuitting)
            {
                updateAction(table);
            }
        }

        protected void OnAnimation(string funName)
        {
             Action<LuaTable> func = table.Get<string, Action<LuaTable>>(funName);
             runNow(func);

        }

        protected void OnApplicationQuit()
        {
            appQuitting = true;
        }

        protected void internalInit(LuaTable table)
        {
            runNow(awakeAction);
            runNow(onEnableAction);
            runNow(startAction);

            setState(stateAwake);
            setState(stateEnable);
            setState(stateStart);
        }

        public object[] InvokeLuaMethod(string methodName, params object[] args)
        {
            if (!Valid || appQuitting)
            {
                return null;
            }

            try
            {
                object[] rets = CallLuaTargetMethod(table, methodName, args);
                return rets;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
        }

        public void InvokeLuaFunction(string methodName)
        {
            InvokeLuaMethod(methodName, table);
        }

        public void InvokeLuaFunction(string methodName, string args)
        {
            InvokeLuaMethod(methodName, table, args);
        }

        // 帧结束后运行
        IEnumerator runEndFrame(Action<LuaTable> action, Action<Action<LuaTable>> after)
        {
            yield return waitEndFrame;
            if (action != null && !appQuitting)
            {
                action(table);
                if (after != null)
                {
                    after(action);
                }
            }
        }

        // 立即运行
        void runNow(Action<LuaTable> action)
        {
            if (action != null && !appQuitting)
            {
                action(table);
            }
        }

        void afterRun(Action<LuaTable> action)
        {
            if (action == awakeAction)
            {
                setState(stateAwake);
            }
            else if (action == startAction)
            {
                setState(stateStart);
            }
            else if (action == onEnableAction)
            {
                setState(stateEnable);
            }
        }

        bool getState(int statebit)
        {
            return (state & (byte)(1 << statebit)) != 0;
        }

        void setState(int statebit)
        {
            state |= (byte)(1 << statebit);
        }

        void unsetState(int statebit)
        {
            state &= (byte)(~(1 << statebit));
        }

        protected void LoadLua(bool findInCache)
        {
            try
            {
                table = LoadLuaTarget(Utils.Lua.L, luaFilename, findInCache, savedValues, this.gameObject);

                if (table != null)
                {
                    awakeAction = table.Get<string, Action<LuaTable>>("Awake");
                    startAction = table.Get<string, Action<LuaTable>>("Start");
                    updateAction = table.Get<string, Action<LuaTable>>("Update");
                    onEnableAction = table.Get<string, Action<LuaTable>>("OnEnable");
                    onDisableAction = table.Get<string, Action<LuaTable>>("OnDisable");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        protected Dictionary<string, object> LoadValues()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();

            if (savedStrings == null || savedStrings.Length == 0)
            {
                return values;
            }

            int indexStr = 0;
            int indexObject = 0;
            int indexSingle = 0;
            int indexInt = 0;

            System.Func<System.Type, object> funcLoadValue = (type) =>
            {
                if (type == null)
                {
                    indexObject++;
                    return null;
                }

                if (type == typeof(float) || type == typeof(System.Double) || type == typeof(System.Single)) { return savedSingles[indexSingle++]; }
                if (type == typeof(bool) || type == typeof(System.Int32)) { return savedIndexes[indexInt++]; }
                if (type == typeof(string)) { return savedStrings[indexStr++]; }
                if (type == typeof(Vector2)) { Vector2 v = new Vector2(); for (int i = 0; i < 2; i++) { v[i] = savedSingles[indexSingle++]; } return v; }
                if (type == typeof(Vector3)) { Vector3 v = new Vector3(); for (int i = 0; i < 3; i++) { v[i] = savedSingles[indexSingle++]; } return v; }
                if (type == typeof(Vector4)) { Vector4 v = new Vector4(); for (int i = 0; i < 4; i++) { v[i] = savedSingles[indexSingle++]; } return v; }
                if (type == typeof(Color)) { Color v = new Color(); for (int i = 0; i < 4; i++) { v[i] = savedSingles[indexSingle++]; } return v; }
                if (type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object))) { return savedObjects[indexObject++]; }
                if (type.IsSubclassOf(typeof(System.Enum))) { return System.Enum.ToObject(type, savedIndexes[indexInt++]); }

                if (type == typeof(Rect))
                {
                    Rect v = new Rect();

                    v.x = savedSingles[indexSingle++];
                    v.y = savedSingles[indexSingle++];
                    v.width = savedSingles[indexSingle++];
                    v.height = savedSingles[indexSingle++];
                    return v;
                }

                if (type == typeof(Bounds))
                {
                    Vector3 center = new Vector3();
                    Vector3 size = new Vector3();

                    for (int i = 0; i < 3; i++)
                    {
                        center[i] = savedSingles[indexSingle++];
                        size[i] = savedSingles[indexSingle++];
                    }

                    return new Bounds(center, size);
                }

                return null;
            };

            while (indexStr < savedStrings.Length)
            {
                string key = savedStrings[indexStr++];
                object value = null;

                int valueTypeIndex = savedIndexes[indexInt++];
                if (valueTypeIndex == typeArrayIndex)
                {
                    int elementTypeIndex = savedIndexes[indexInt++];
                    System.Type elementType =
                        (elementTypeIndex == typeEnumIndex || elementTypeIndex == typeUnityEngineObjectIndex) ?
                        GetInternalType(savedStrings[indexStr++]) : SupportTypes[elementTypeIndex];
                    int arraySize = savedIndexes[indexInt++];
                    System.Array array = System.Array.CreateInstance(elementType, arraySize);

                    for (int i = 0; i < arraySize; i++)
                    {
                        array.SetValue(funcLoadValue(elementType), i);
                    }

                    value = array;
                }
                else
                {
                    System.Type valueType =
                        valueTypeIndex == typeEnumIndex ? GetInternalType(savedStrings[indexStr++]) :
                        valueTypeIndex == typeUnityEngineObjectIndex ? typeof(UnityEngine.Object) : SupportTypes[valueTypeIndex];

                    value = funcLoadValue(valueType);
                }

                values[key] = value;
            }

            return values;
        }

        protected void SaveValues(Dictionary<string, object> values)
        {
            List<string> arrayString = new List<string>();
            List<float> arraySingle = new List<float>();
            List<UnityEngine.Object> arrayObjects = new List<UnityEngine.Object>();
            List<int> arrayIndex = new List<int>();

            System.Action<System.Type, object> funcSaveValue = (type, value) =>
            {
                if (type == typeof(float)) { float v = (float)value; arraySingle.Add(v); }
                else if (type == typeof(System.Int32)) { int v = System.Convert.ToInt32(value.ToString()); arrayIndex.Add(v); }
                else if (type == typeof(System.Double) || type == typeof(System.Single)) { float v = System.Convert.ToSingle(value.ToString()); arraySingle.Add(v); }
                else if (type == typeof(bool)) { arrayIndex.Add((bool)value ? 1 : 0); }
                else if (type == typeof(string)) { arrayString.Add((string)value); }
                else if (type == typeof(Vector2)) { Vector2 v = (Vector2)value; for (int i = 0; i < 2; i++) { arraySingle.Add(v[i]); } }
                else if (type == typeof(Vector3)) { Vector3 v = (Vector3)value; for (int i = 0; i < 3; i++) { arraySingle.Add(v[i]); } }
                else if (type == typeof(Vector4)) { Vector4 v = (Vector4)value; for (int i = 0; i < 4; i++) { arraySingle.Add(v[i]); } }
                else if (type == typeof(Color)) { Color v = (Color)value; for (int i = 0; i < 4; i++) { arraySingle.Add(v[i]); } }
                else if (type == typeof(Rect)) { Rect v = (Rect)value; arraySingle.Add(v.x); arraySingle.Add(v.y); arraySingle.Add(v.width); arraySingle.Add(v.height); }
                else if (type == typeof(Bounds)) { Bounds v = (Bounds)value; for (int i = 0; i < 3; i++) { arraySingle.Add(v.center[i]); arraySingle.Add(v.size[i]); } }
                else if (type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object))) { arrayObjects.Add((UnityEngine.Object)value); }
                else if (type.IsSubclassOf(typeof(System.Enum))) { arrayIndex.Add(System.Convert.ToInt32(value)); }
            };

            foreach (var p in values)
            {
                var value = p.Value;

                if (value == null)
                {
                    continue;
                }

                arrayString.Add(p.Key);

                var valueType = value.GetType();
                if (valueType.IsArray)
                {
                    arrayIndex.Add(typeArrayIndex);

                    var elementType = valueType.GetElementType();
                    if (elementType.IsSubclassOf(typeof(System.Enum)))
                    {
                        arrayIndex.Add(typeEnumIndex);
                        arrayString.Add(elementType.FullName);
                    }
                    else if (elementType == typeof(UnityEngine.Object) || elementType.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
                        arrayIndex.Add(typeUnityEngineObjectIndex);
                        arrayString.Add(elementType.FullName);
                    }
                    else
                    {
                        arrayIndex.Add(System.Array.FindIndex(SupportTypes, t => t == elementType));
                    }

                    var array = value as System.Array;

                    arrayIndex.Add(array.Length);

                    foreach (var element in array)
                    {
                        funcSaveValue(elementType, element);
                    }
                }
                else
                {
                    if (valueType.IsSubclassOf(typeof(System.Enum)))
                    {
                        arrayIndex.Add(typeEnumIndex);
                        arrayString.Add(valueType.FullName);
                    }
                    else if (valueType == typeof(UnityEngine.Object) || valueType.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
                        arrayIndex.Add(typeUnityEngineObjectIndex);
                    }
                    else
                    {
                        int typeIndex = System.Array.FindIndex(SupportTypes, t => t == valueType);
                        if (typeIndex == -1)
                        {
                            Debug.LogError("unsupport type " + valueType.ToString() + " case exception");
                        }

                        arrayIndex.Add(typeIndex);
                    }

                    funcSaveValue(valueType, value);
                }
            }

            savedStrings = arrayString.ToArray();
            savedObjects = arrayObjects.ToArray();
            savedSingles = arraySingle.ToArray();
            savedIndexes = arrayIndex.ToArray();
        }

        public void Reload()
        {
            if (Valid)
            {
                table.Dispose();
                table = null;
            }

            ClearCachedLuaFile(luaFilename);
            LoadLua(false);
        }

        protected void OnDestroy()
        {
            Action onDestroyAction = Valid ? table.Get<string, Action>("OnDestroy") : null;
            if (onDestroyAction != null && !appQuitting)
            {
                onDestroyAction();
            }

            startAction = null;
            updateAction = null;
            onEnableAction = null;
            onDisableAction = null;

            Utils.Lua.FullGc();
            Debug.Log("~" + (luaFilename != null ? System.IO.Path.GetFileNameWithoutExtension(luaFilename) : name) + " was destroy!");
        }


        //--------------------------- Static & Const -------------------------------------
        const int typeArrayIndex = 2345;
        const int typeEnumIndex = 3456;
        const int typeUnityEngineObjectIndex = 4567;

        public static System.Type[] SupportTypes = {
            typeof(float),
            typeof(bool),
            typeof(string),
            typeof(System.Double),
            typeof(System.Single),
            typeof(System.Int32),
            typeof(UnityEngine.Vector2),
            typeof(UnityEngine.Vector3),
            typeof(UnityEngine.Vector4),
            typeof(UnityEngine.Color),
            typeof(UnityEngine.Rect),
            typeof(UnityEngine.Bounds),
        };

        static string nameNumber = "Number";
        static string nameString = "String";
        static string nameBoolean = "Boolean";

        public static string NameEvents = "events";
        public static string ArgsName = "args";
        public static string NameMin = "min";
        public static string NameMax = "max";

        public static WaitForEndOfFrame waitEndFrame = new WaitForEndOfFrame();

        static LuaTable caches = null;

        static LuaTable LoadLuaTarget(RealStatePtr L, string filename, bool findInCache, Dictionary<string, object> values, UnityEngine.GameObject go)
        {
            LuaTable table = null;

            try
            {
                if (table == null)
                {
                    byte[] chunk = LuaManager.Instance.LoadScript(filename);
                    object[] rets = Utils.Lua.DoString(chunk, filename);

                    if (rets.Length == 0)
                    {
                        throw new Exception("lua file " + filename + " need return table type");
                    }

                    table = rets[0] as LuaTable;
                }

                if (table == null)
                {
                    throw new Exception("lua file " + filename + " convert to table type failed");
                }

                table.Set<string, UnityEngine.GameObject>("gameObject", go);

                LuaTable args = table.Get<string, LuaTable>(ArgsName);
                if (args != null)
                {
                    foreach (var p in values)
                    {
                        var value = p.Value;
                        if (value == null)
                        {
                            continue;
                        }

                        args.Set(p.Key, p.Value);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return table;
        }

        static object[] CallLuaTargetMethod(LuaTable tbl, string methodName, params object[] args)
        {
            LuaTypes type = LuaTypes.LUA_TNIL;
            object value = null;

            tbl.Get<string, object>(methodName, out value, out type);
            LuaFunction func = value as LuaFunction;
            if (type != LuaTypes.LUA_TFUNCTION || func == null)
            {
                return null;
            }

            return func.Call(args);
        }

        public static Type GetInternalType(string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr))
            {
                return null;
            }

            Type type = null;

            if (typeStr == nameNumber)
            {
                type = typeof(float);
            }
            else if (typeStr == nameString)
            {
                type = typeof(string);
            }
            else if (typeStr == nameBoolean)
            {
                type = typeof(bool);
            }
            else
            {
                type = Utils.ObjTranslator.FindType(typeStr);
            }

            return type;
        }

        #region Cache

        // 初始化缓存系统
        static void BuildCacheSystem()
        {
            int orginTop = LuaAPI.lua_gettop(Utils.Lua.L);
            LuaAPI.lua_newtable(Utils.Lua.L);
            int reference = LuaAPI.luaL_ref(Utils.Lua.L);
            caches = new LuaTable(reference, Utils.Lua);
            LuaAPI.lua_settop(Utils.Lua.L, orginTop);
        }

        // 缓存LuaTable
        static void CacheLuaFile(string key, byte[] content)
        {
            if (caches == null)
            {
                BuildCacheSystem();
            }

            caches.Set<string, byte[]>(key, content);
        }

        // 查找已经缓存过的LuaTable
        static LuaTable FindLuaInCached(string key)
        {
            if (caches == null)
            {
                return null;
            }

            return caches.Get<string, LuaTable>(key);
        }

        // 清理缓存的LuaTable
        static void ClearCachedLuaFile(string key)
        {
            if (caches == null)
            {
                return;
            }

            caches.Set<string, object>(key, null);
        }

        #endregion
    }

}