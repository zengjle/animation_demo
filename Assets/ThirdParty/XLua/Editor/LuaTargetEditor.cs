using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using UnityEditorInternal;
using System;

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
    #region Descriptor

    public struct DescriptorValue
    {
        public object v;
        public Descriptor[] desc;
    }

    public class Descriptor
    { }

    public class MinMaxDescriptor : Descriptor
    {
        public float min;
        public float max;

        public MinMaxDescriptor(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    public class TooltipDescriptor : Descriptor
    {
        public string tooltip;
    }

    public class StyleDescriptor : Descriptor
    {
        public string style;
    }

    public class OrderDescriptor : Descriptor
    {
        public int order;
    }

    #endregion


    [CustomEditor(typeof(LuaTarget))]
    public class LuaTargetEditor : Editor
    {
        LuaTarget lastTarget;
        string[] fileOptions;
        List<string> lastKeyList;
        List<DescriptorValue> lastValueList;
        string lastPath;
        System.DateTime lastModifyTime;
        List<Region> lastRegionList;
        GUIStyle regionStyle;//= new GUIStyle(EditorStyles.foldout);
        Dictionary<string, bool> foldout = new Dictionary<string, bool>();
        Dictionary<string, bool> regionFoldout = new Dictionary<string, bool>();
        Dictionary<string, ArrayItem> listmap = new Dictionary<string, ArrayItem>();
        string content;
        string editorMethodArgument = string.Empty;
        
        const float defaultReorderableElementHeight = 20f;
        const int maxMethodButtonPerRow = 3;

        static readonly Dictionary<System.Type, float> reorderableListHeightMap = new Dictionary<System.Type, float>
        {
            {typeof(Bounds), 2f},
            {typeof(Vector4), 2f},
            {typeof(Rect), 2f},
            {typeof(Sprite), 3f}
        };

        class Region
        {
            public int start;
            public int end;
            public string name;
            public float fadeValue;
            public AnimFloat fadeAnim;
        }

        class ArrayItem
        {
            public IList datalist;
            public ReorderableList viewlist;
        }

        private void OnEnable()
        {
            fileOptions = Directory.GetFiles(LuaEnv.LuaDir, "*.lua", SearchOption.AllDirectories);

            for (int i = 0; i < fileOptions.Length; i++)
            {
                fileOptions[i] = fileOptions[i].Replace('\\', '/');
                fileOptions[i] = fileOptions[i].Substring(fileOptions[i].IndexOf(LuaEnv.LuaRelativeDir) + LuaEnv.LuaRelativeDir.Length + 1);
            }

            lastTarget = null;
        }

        GUIStyle RegionStyle {
            get {
                if (regionStyle == null)
                {
                    regionStyle = new GUIStyle(EditorStyles.foldout);
                }

                return regionStyle;
            }
        }

        public override void OnInspectorGUI()
        {
            LuaTarget currTarget = target as LuaTarget;

            if (EditorApplication.isPlaying)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("luaFilename"));

            EditorGUILayout.BeginHorizontal();

            string targetPath = currTarget.luaFilename == null ? string.Empty : LuaEnv.LuaRelativeDir + "/" + currTarget.luaFilename;
            string targetLuaPath = targetPath.EndsWith(".lua") ? targetPath : targetPath + ".lua";

            if (targetLuaPath.StartsWith(LuaEnv.LuaRelativeDir) && targetLuaPath.Length > LuaEnv.LuaRelativeDir.Length + 1)
            {
                targetLuaPath = targetLuaPath.Substring(LuaEnv.LuaRelativeDir.Length + 1);
            }

            int prevIndex = System.Array.FindIndex(fileOptions, luaFile => luaFile == targetLuaPath);
            int currIndex = EditorGUILayout.Popup(prevIndex, fileOptions);

            if (prevIndex != currIndex)
            {
                if ((uint)currIndex < fileOptions.Length)
                {
                    currTarget.luaFilename = fileOptions[currIndex];
                }

                GUI.changed = true;
            }

            string apath = Path.Combine("Assets", targetPath);
            if (!apath.EndsWith(".lua"))
            {
                apath += ".lua";
            }

            var prevAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(apath);
            var currAsset = EditorGUILayout.ObjectField(prevAsset, typeof(UnityEngine.Object), false);

            if (currAsset != prevAsset)
            {
                currTarget.luaFilename = currAsset != null ? AssetDatabase.GetAssetPath(currAsset).Substring(("Assets/" + LuaEnv.LuaRelativeDir + "/").Length) : string.Empty;
                targetPath = currTarget.luaFilename == null ? string.Empty : LuaEnv.LuaRelativeDir + "/" + currTarget.luaFilename;
                GUI.changed = true;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            if (EditorApplication.isPlaying)
            {
                EditorGUI.EndDisabledGroup();

                OnInspectorGUI_Playing();

                return;
            }

            string path = Path.Combine(Application.dataPath, targetPath);
            if (!path.EndsWith(".lua"))
            {
                path += ".lua";
            }

            if (!File.Exists(path))
            {
                serializedObject.ApplyModifiedProperties();

                if (lastPath != "" && PrefabUtility.GetPrefabType(serializedObject.targetObject) == PrefabType.Prefab)
                {
                    Debug.Log("Auto save in project view 1");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                lastPath = "";
                lastKeyList = null;

                return;
            }

            List<string> keylist = new List<string>();
            List<DescriptorValue> valuelist = new List<DescriptorValue>();

            var prevValues = currTarget.savedValues;
            var currValues = new Dictionary<string, object>();
            if (lastTarget == currTarget && lastKeyList != null && lastPath != path && lastModifyTime != File.GetLastWriteTime(path))
            {
                lastPath = path;
                keylist = lastKeyList;
                valuelist = lastValueList;
            }
            else
            {
                int originTop = 0;
                try
                {
                    var content = File.ReadAllText(path);
                    LuaTable argsTable = null;
                    keylist = ParseLuaBehaviour_Begin(Utils.LuaL, path, ref originTop, ref argsTable);
                    if (keylist == null)
                    {
                        ParseLuaTarget_End(Utils.LuaL, originTop);
                        return;
                    }

                    var argsFieldIndex = GetFieldSourceIndex(content, LuaTarget.ArgsName);
                    var argsFieldContent = content.Substring(argsFieldIndex.Key, argsFieldIndex.Value - argsFieldIndex.Key - 1);
                    var orderResult = (argsFieldIndex.Key == -1 || argsFieldIndex.Value == -1) ? null : GetOrderedKey(keylist, argsFieldContent);
                    string region = "--region";
                    string endRegion = "--endregion";
                    if (orderResult != null)
                    {
                        keylist = keylist.OrderBy(key => { int index; orderResult.TryGetValue(key, out index); return index; }).ToList();

                        List<int> lastRegionIndexList = null;
                        List<string> regionNameList = null;

                        int i = 0;
                        int len = argsFieldContent.Length;

                        while (i < len)
                        {
                            int start = argsFieldContent.IndexOf(region, i);
                            if (start < 0)
                            {
                                break;
                            }

                            int regionNameEndIndex = argsFieldContent.IndexOf('\n', start);
                            if (regionNameEndIndex < start + region.Length + 1)
                            {
                                break;
                            }

                            int end = argsFieldContent.IndexOf(endRegion, start);
                            if (end < 0)
                            {
                                break;
                            }

                            if (lastRegionIndexList == null)
                            {
                                lastRegionIndexList = new List<int>();
                                regionNameList = new List<string>();
                            }

                            lastRegionIndexList.Add(start);
                            lastRegionIndexList.Add(end);
                            regionNameList.Add(argsFieldContent.Substring(start + region.Length + 1, regionNameEndIndex - start - region.Length - 1));

                            i = end + endRegion.Length;
                        }

                        if (lastRegionIndexList != null)
                        {
                            var keyIndexList = keylist.ConvertAll(key => { int index; orderResult.TryGetValue(key, out index); return index; });
                            int currKeyIndexListIndex = 0;

                            lastRegionList = new List<Region>();

                            for (int j = 0; j < lastRegionIndexList.Count; j += 2)
                            {
                                int start = lastRegionIndexList[j];
                                int end = lastRegionIndexList[j + 1];
                                int curr = keyIndexList[currKeyIndexListIndex];

                                while (curr < start)
                                {
                                    currKeyIndexListIndex++;
                                    if (currKeyIndexListIndex >= keyIndexList.Count)
                                    {
                                        curr = -1;
                                        break;
                                    }
                                    else
                                    {
                                        curr = keyIndexList[currKeyIndexListIndex];
                                    }
                                }

                                if (curr == -1)
                                {
                                    break;
                                }

                                int startKeyListIndex = currKeyIndexListIndex;
                                while (curr < end)
                                {
                                    currKeyIndexListIndex++;
                                    if (currKeyIndexListIndex >= keyIndexList.Count)
                                    {
                                        curr = -1;
                                        break;
                                    }
                                    else
                                    {
                                        curr = keyIndexList[currKeyIndexListIndex];
                                    }
                                }

                                lastRegionList.Add(new Region()
                                {
                                    start = startKeyListIndex,
                                    end = currKeyIndexListIndex,
                                    name = regionNameList[j >> 1],
                                    fadeValue = 1f,
                                });

                                if (curr == -1)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        keylist.Sort();
                    }

                    valuelist = ParseLuaTarget_GetValues(Utils.LuaL, argsTable, keylist);

                    ParseLuaTarget_End(Utils.LuaL, originTop);

                    lastKeyList = keylist;
                    lastValueList = valuelist;
                    lastTarget = currTarget;
                    lastPath = path;
                    lastModifyTime = File.GetLastWriteTime(path);
                    regionFoldout = new Dictionary<string, bool>();
                }
                catch(System.Exception e)
                {
                    ParseLuaTarget_End(Utils.LuaL, originTop);
                    Debug.LogError(e.Message);
                }
            }

            int currRegionIndex = 0;
            int regionNextIndex = (lastRegionList != null && lastRegionList.Count > currRegionIndex) ? lastRegionList[currRegionIndex].start : -1;
            bool inRegion = false;
            bool regionShow = true;

            for (int i = 0; i < keylist.Count; i++)
            {
                var key = keylist[i];
                DescriptorValue value = valuelist[i];
                object pv;

                prevValues.TryGetValue(key, out pv);
                if (pv != null)
                {
                    if (value.v is System.Type)
                    {
                        if (!(pv is System.Type) && (pv.GetType() == value.v || pv.GetType().IsSubclassOf(value.v as System.Type)))
                        {
                            value.v = pv;
                        }
                    }
                    else
                    {
                        if (pv.GetType() == value.v.GetType())
                        {
                            value.v = pv;
                        }
                    }
                }

                if (regionNextIndex != -1 && i >= regionNextIndex && inRegion)
                {
                    EditorGUILayout.EndFadeGroup();
                    EditorGUILayout.EndVertical();
                    currRegionIndex++;
                    regionNextIndex = (lastRegionList != null && lastRegionList.Count > currRegionIndex) ? lastRegionList[currRegionIndex].start : -1;
                    inRegion = false;
                    regionShow = true;
                }

                if (regionNextIndex != -1 && i >= regionNextIndex && !inRegion)
                {
                    EditorGUILayout.BeginVertical("GroupBox");

                    var region = lastRegionList[currRegionIndex];

                    regionNextIndex = region.end;
                    inRegion = true;

                    if (!regionFoldout.TryGetValue(region.name, out regionShow))
                    {
                        regionShow = true;
                    }

                    EditorGUI.indentLevel++;
                    bool currRegionShow = EditorGUILayout.Foldout(regionShow, region.name, RegionStyle);
                    EditorGUI.indentLevel--;

                    regionFoldout[region.name] = currRegionShow;

                    if (currRegionShow != regionShow)
                    {
                        if (region.fadeAnim != null)
                        {
                            region.fadeAnim.valueChanged = null;
                        }

                        region.fadeAnim = new AnimFloat(region.fadeValue)
                        {
                            target = currRegionShow ? 1f : 0f,
                            speed = 2.5f,
                        };

                        var tRegion = region;

                        var unityEvent = new UnityEvent();
                        unityEvent.AddListener(() =>
                        {
                            Repaint();
                            if (tRegion.fadeAnim != null)
                            {
                                tRegion.fadeValue = tRegion.fadeAnim.value;
                            }
                        });

                        region.fadeAnim.valueChanged = unityEvent;
                    }

                    EditorGUILayout.BeginFadeGroup(region.fadeValue);
                    regionShow = region.fadeValue > 0f;
                }

                if (regionShow)
                {
                    currValues[key] = ShowItem(key, value.v, value.desc);
                }
                else
                {
                    var type = value.v as System.Type;
                    if (type != null)
                    {
                        object ret = type.IsArray ? System.Array.CreateInstance(type.GetElementType(), 0) : type.IsValueType ? System.Activator.CreateInstance(type) : null;
                        if (ret != null && !(ret is System.Type))
                        {
                            GUI.changed = true;
                        }

                        currValues[key] = ret;
                    }
                    else
                    {
                        currValues[key] = value.v;
                    }
                }
            }

            if (inRegion)
            {
                EditorGUILayout.EndFadeGroup();
                EditorGUILayout.EndVertical();
            }

            if (GUI.changed)
            {
                currTarget.savedValues = currValues;
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(currTarget, "LuaTarget Value Updated");
                EditorUtility.SetDirty(currTarget);

                if (PrefabUtility.GetPrefabType(serializedObject.targetObject) == PrefabType.Prefab)
                {
                    Debug.Log("Auto save in project view 2");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }

        void OnInspectorGUI_Playing()
        {
            var currTarget = target as LuaTarget;

            if (currTarget == null || !currTarget.Valid)
            {
                EditorGUILayout.HelpBox("lua not initialized", MessageType.Warning);
                return;
            }

            EditorGUI.BeginDisabledGroup(false);

            int originTop = LuaAPI.lua_gettop(Utils.LuaL);

            LuaAPI.lua_getref(Utils.LuaL, currTarget.LuaReference);
            object value = Utils.ObjTranslator.GetObject(Utils.LuaL, -1, typeof(object));

            ShowInternalItem_Playing(string.Empty, string.Empty, value);

            LuaAPI.lua_settop(Utils.LuaL, originTop);

            EditorGUI.EndDisabledGroup();

            originTop = LuaAPI.lua_gettop(Utils.LuaL);

            object evtVal = null;
            LuaTypes evtValueType = LuaTypes.LUA_TNIL;

            currTarget.Table.Get<string, object>(LuaTarget.NameEvents, out evtVal, out evtValueType);
            if (evtValueType == LuaTypes.LUA_TTABLE)
            {
                var events = new List<string>();
                EditorGUILayout.BeginVertical("Box");

                LuaTable evtTable = evtVal as LuaTable;

                evtTable.ForEach<string, object>((key, val, type) =>
                {
                    if (type != LuaTypes.LUA_TFUNCTION)
                    {
                        return;
                    }

                    events.Add(key);
                });

                if (content != null)
                {
                    var eventsFieldIndex = GetFieldSourceIndex(content, LuaTarget.ArgsName);
                    var orderMap = (eventsFieldIndex.Key == -1 || eventsFieldIndex.Value == -1) ? null : GetOrderedKey(events, content.Substring(eventsFieldIndex.Key, eventsFieldIndex.Value - eventsFieldIndex.Key - 1));

                    events = events.OrderBy(key => { int index; orderMap.TryGetValue(key, out index); return index; }).ToList();
                }

                int methodNum = 0;
                EditorGUILayout.BeginHorizontal();
                bool invoked = false;

                foreach (var key in events)
                {
                    if (GUILayout.Button(key) && !invoked)
                    {
                        invoked = true;
                        LuaFunction func = null;

                        evtTable.Get<string, LuaFunction>(key, out func);
                        if (func != null)
                        {
                            func.Call(evtTable, editorMethodArgument);
                        }
                    }

                    methodNum++;
                    if (methodNum % maxMethodButtonPerRow == 0)
                    {
                        methodNum = 0;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }

                if (methodNum % maxMethodButtonPerRow != 0)
                {
                    EditorGUILayout.LabelField(string.Empty);
                }

                EditorGUILayout.EndHorizontal();

                editorMethodArgument = EditorGUILayout.TextField("Method Argument", editorMethodArgument);
                EditorGUILayout.EndVertical();
            }

            LuaAPI.lua_settop(Utils.LuaL, originTop);

            if (GUILayout.Button("Reload"))
            {
                currTarget.Reload();
                content = null;
            }
        }

        object ShowItem(string key, object luaValue, Descriptor[] descriptors)
        {
            if (luaValue == null)
            {
                return null;
            }

            object ret = null;
            var type = luaValue.GetType();

            if (luaValue is System.Type)
            {
                type = luaValue as System.Type;
                if (type.IsArray)
                {
                    ret = ShowInternalArray(key, type, System.Array.CreateInstance(type.GetElementType(), 0), descriptors);
                }
                else
                {
                    ret = ShowInternalItem(key, type, type.IsValueType ? System.Activator.CreateInstance(type) : null, descriptors);
                }
            }
            else
            {
                if (type.IsArray)
                {
                    ret = ShowInternalArray(key, type, luaValue, descriptors);
                }
                else
                {
                    ret = ShowInternalItem(key, type, luaValue, descriptors);
                }
            }

            return ret;
        }

        object ShowInternalArray(string key, System.Type type, object value, Descriptor[] descriptors)
        {
            object ret = null;
            var elementType = type.GetElementType();
            ArrayItem arrayItem;

            listmap.TryGetValue(key, out arrayItem);

            if (arrayItem == null || arrayItem.datalist.GetType().GetGenericArguments()[0] != elementType)
            {
                float height = 0f;
                if (!reorderableListHeightMap.TryGetValue(elementType, out height))
                {
                    height = 1f;
                }

                height *= defaultReorderableElementHeight;

                arrayItem = new ArrayItem();
                arrayItem.datalist = System.Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;

                foreach (var element in value as System.Array)
                {
                    arrayItem.datalist.Add(element);
                }

                arrayItem.viewlist = new ReorderableList(arrayItem.datalist, elementType);
                arrayItem.viewlist.elementHeight = height;
                arrayItem.viewlist.drawHeaderCallback += rect =>
                {
                    EditorGUI.PrefixLabel(rect, new GUIContent(GetPrefixKey(key)));
                };

                arrayItem.viewlist.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) => {
                    if (elementType == typeof(float)) { arrayItem.datalist[index] = EditorGUI.FloatField(rect, (float)arrayItem.datalist[index]); }
                    else if (elementType == typeof(bool)) { arrayItem.datalist[index] = EditorGUI.Toggle(rect, (bool)arrayItem.datalist[index]); }
                    else if (elementType == typeof(string)) { arrayItem.datalist[index] = EditorGUI.TextField(rect, "", (string)arrayItem.datalist[index]); }
                    else if (elementType == typeof(System.Int64)) { arrayItem.datalist[index] = EditorGUI.IntField(rect, "", (int)arrayItem.datalist[index]); }
                    else if (elementType == typeof(System.Int32)) { arrayItem.datalist[index] = EditorGUI.IntField(rect, "", (int)arrayItem.datalist[index]); }
                    else if (elementType == typeof(System.Double)) { arrayItem.datalist[index] = EditorGUI.DoubleField(rect, "", (double)arrayItem.datalist[index]); }
                    else if (elementType == typeof(System.Single)) { arrayItem.datalist[index] = EditorGUI.FloatField(rect, "", (float)arrayItem.datalist[index]); }
                    else if (elementType == typeof(Vector2)) { arrayItem.datalist[index] = EditorGUI.Vector2Field(rect, "", (Vector2)arrayItem.datalist[index]); }
                    else if (elementType == typeof(Vector3)) { arrayItem.datalist[index] = EditorGUI.Vector3Field(rect, "", (Vector3)arrayItem.datalist[index]); }
                    else if (elementType == typeof(Vector4)) { arrayItem.datalist[index] = EditorGUI.Vector4Field(rect, "", (Vector4)arrayItem.datalist[index]); }
                    else if (elementType == typeof(Color)) { arrayItem.datalist[index] = EditorGUI.ColorField(rect, "", (Color)arrayItem.datalist[index]); }
                    else if (elementType == typeof(Rect)) { arrayItem.datalist[index] = EditorGUI.RectField(rect, "", (Rect)arrayItem.datalist[index]); }
                    else if (elementType == typeof(Bounds)) { arrayItem.datalist[index] = EditorGUI.BoundsField(rect, "", (Bounds)arrayItem.datalist[index]); }
                    else if (elementType == typeof(Sprite))
                    {
                        rect.width = Mathf.Min(rect.width, rect.height);
                        arrayItem.datalist[index] = EditorGUI.ObjectField(rect, (UnityEngine.Object)arrayItem.datalist[index], elementType, true);
                    }
                    else if (elementType == typeof(UnityEngine.Object) || elementType.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
                        arrayItem.datalist[index] = EditorGUI.ObjectField(rect, (UnityEngine.Object)arrayItem.datalist[index], elementType, true);
                    }
                    else if (elementType.IsSubclassOf(typeof(System.Enum)))
                    {
                        arrayItem.datalist[index] = EditorGUI.EnumPopup(rect, (System.Enum)arrayItem.datalist[index]);
                    }
                };

                arrayItem.viewlist.onReorderCallback += rlist =>
                {
                    GUI.changed = true;
                };

                if (elementType.IsClass)
                {
                    arrayItem.viewlist.onAddCallback += rlist =>
                    {
                        arrayItem.datalist.Add(null);
                    };
                }
                else if (elementType == typeof(UnityEngine.Color))
                {
                    arrayItem.viewlist.onAddCallback += rlist =>
                    {
                        arrayItem.datalist.Add(Color.white);
                    };
                }

                if (elementType.IsSubclassOf(typeof(System.Enum)))
                {
                    var menu = new GenericMenu();
                    foreach (var v in System.Enum.GetValues(elementType))
                    {
                        menu.AddItem(new GUIContent(v.ToString()), false, selection =>
                        {
                            arrayItem.datalist.Add(selection);
                        }, v);
                    }

                    arrayItem.viewlist.onAddDropdownCallback += (Rect buttonRect, ReorderableList list) =>
                    {
                        menu.DropDown(buttonRect);
                    };
                }

                listmap[key] = arrayItem;
            }

            Rect dragRect = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
            dragRect.height += arrayItem.datalist.Count > 0 ? 19f : 39f;

            var currentEvent = Event.current;
            switch (currentEvent.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        if (!dragRect.Contains(currentEvent.mousePosition))
                        {
                            break;
                        }

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (currentEvent.type == EventType.DragPerform)
                        {
                            bool isComponent = elementType.IsSubclassOf(typeof(UnityEngine.Component)) || elementType == typeof(UnityEngine.Component);
                            int length = arrayItem.datalist.Count;

                            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                            {
                                var po = DragAndDrop.objectReferences[i];
                                if (isComponent)
                                {
                                    var pGO = po as GameObject;
                                    if (pGO != null)
                                    {
                                        po = pGO.GetComponent(elementType);
                                    }
                                }

                                arrayItem.datalist.Add(po);
                            }

                            GUI.changed = true;
                        }
                    }
                    break;
                default:
                    break;
            }

            arrayItem.viewlist.DoLayoutList();

            var array = System.Array.CreateInstance(elementType, arrayItem.datalist.Count);
            arrayItem.datalist.CopyTo(array, 0);
            ret = array;

            return ret;
        }

        bool IsShowKey(System.Type type)
        {
            bool showKey = false;

            System.Type[] types =
            {
                typeof(float),
                typeof(string),
                typeof(bool),
                typeof(System.Int32),
                typeof(System.Int64),
                typeof(System.Double),
                typeof(System.Single),
            };

            for (int i = 0; i < types.Length; i++)
            {
                if (type == types[i])
                {
                    showKey = true;
                    break;
                }
            }

            if (!showKey && type.IsSubclassOf(typeof(System.Enum)))
            {
                showKey = true;
            }

            return showKey;
        }

        object ShowInternalItem(string key, System.Type type, object value, Descriptor[] descriptors)
        {
            object ret = null;
            bool endHorizontal = IsShowKey(type);

            key = GetPrefixKey(key);

            if (endHorizontal)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(key);
            }

            if (type == typeof(float))
            {
                if (descriptors != null && descriptors[0] is MinMaxDescriptor)
                {
                    var minMaxDescriptor = descriptors[0] as MinMaxDescriptor;
                    ret = EditorGUILayout.Slider((float)value, minMaxDescriptor.min, minMaxDescriptor.max);
                }
                else
                {
                    ret = EditorGUILayout.FloatField((float)value);
                }
            }
            else if (type == typeof(string)) { ret = EditorGUILayout.TextField(value as string); }
            else if (type == typeof(bool)) { ret = EditorGUILayout.Toggle((bool)value); }
            else if (type == typeof(System.Int32)) { ret = EditorGUILayout.IntField(System.Convert.ToInt32(value)); }
            else if (type == typeof(System.Int64)) { ret = EditorGUILayout.LongField(System.Convert.ToInt64(value)); }
            else if (type == typeof(System.Double)) { ret = EditorGUILayout.DoubleField(System.Convert.ToDouble(value)); }
            else if (type == typeof(System.Single)) { ret = EditorGUILayout.FloatField(System.Convert.ToSingle(value)); }
            else if (type == typeof(Vector2)) { ret = EditorGUILayout.Vector2Field(key, (Vector2)value); }
            else if (type == typeof(Vector3)) { ret = EditorGUILayout.Vector3Field(key, (Vector3)value); }
            else if (type == typeof(Vector4)) { ret = EditorGUILayout.Vector4Field(key, (Vector4)value); }
            else if (type == typeof(Color)) { ret = EditorGUILayout.ColorField(key, (Color)value); }
            else if (type == typeof(Rect)) { ret = EditorGUILayout.RectField(key, (Rect)value); }
            else if (type == typeof(Bounds)) { ret = EditorGUILayout.BoundsField(key, (Bounds)value); }
            else if (type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object))) { ret = EditorGUILayout.ObjectField(key, value as UnityEngine.Object, type, true); }
            else if (type.IsSubclassOf(typeof(System.Enum))) { ret = EditorGUILayout.EnumPopup(value as System.Enum); }

            if (endHorizontal)
            {
                EditorGUILayout.EndHorizontal();
            }

            return ret;
        }

        public void ShowInternalItem_Playing(string path, string key, object value, bool showKey = true)
        {
            if (value == null)
            {
                return;
            }

            var type = value.GetType();
            bool endSingleVertical = false;

            if (showKey)
            {
                if (type.IsValueType || type == typeof(string) || type.IsSubclassOf(typeof(UnityEngine.Object)) || type == typeof(UnityEngine.Object))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(GetPrefixKey(key));

                    endSingleVertical = true;
                }
            }

            if (type.IsValueType)
            {
                if (type == typeof(double)) { EditorGUILayout.DoubleField((double)value); }
                else if (type == typeof(bool)) { EditorGUILayout.Toggle((bool)value); }
                else if (type == typeof(System.Int32)) { EditorGUILayout.IntField(System.Convert.ToInt32(value)); }
                else if (type == typeof(System.Int64)) { EditorGUILayout.IntField(System.Convert.ToInt32(value)); }
                else if (type == typeof(System.Double)) { EditorGUILayout.DoubleField(System.Convert.ToDouble(value)); }
                else if (type == typeof(System.Single)) { EditorGUILayout.FloatField(System.Convert.ToSingle(value)); }
                else if (type == typeof(Vector2)) { EditorGUILayout.Vector2Field(string.Empty, (Vector2)value); }
                else if (type == typeof(Vector3)) { EditorGUILayout.Vector3Field(string.Empty, (Vector3)value); }
                else if (type == typeof(Vector4)) { EditorGUILayout.Vector4Field(string.Empty, (Vector4)value); }
                else if (type == typeof(Color)) { EditorGUILayout.ColorField((Color)value); }
                else if (type == typeof(Rect)) { EditorGUILayout.RectField((Rect)value); }
                else if (type == typeof(Bounds)) { EditorGUILayout.BoundsField((Bounds)value); }
                else if (type.IsSubclassOf(typeof(System.Enum))) { EditorGUILayout.EnumPopup(value as System.Enum); }
            }
            else
            {
                if (type == typeof(string)) { EditorGUILayout.TextField(value as string); }
                else if (type.IsSubclassOf(typeof(UnityEngine.Object)) || type == typeof(UnityEngine.Object)) { EditorGUILayout.ObjectField(value as UnityEngine.Object, type, true); }
                else if (type.IsArray)
                {
                    bool show = false;

                    string fullpath = path + "." + key;

                    EditorGUILayout.BeginVertical("Box");
                    EditorGUI.indentLevel++;

                    foldout.TryGetValue(fullpath, out show);
                    show = EditorGUILayout.Foldout(show, GetPrefixKey(key));
                    foldout[fullpath] = show;

                    if (show)
                    {
                        var array = value as System.Array;
                        int length = array.Length;
                        for (int i = 0; i < length; i++)
                        {
                            ShowInternalItem_Playing(fullpath, i.ToString(), array.GetValue(i), false);
                        }
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                }
                else if (type == typeof(LuaTable))
                {
                    bool show = false;
                    string fullpath = null;
                    if (string.IsNullOrEmpty(key))
                    {
                        show = true;
                        fullpath = key;
                    }
                    else
                    {
                        fullpath = path + "." + key;

                        EditorGUILayout.BeginVertical("Box");
                        

                        if (!foldout.TryGetValue(fullpath, out show) && string.IsNullOrEmpty(path))
                        {
                            show = true;
                        }

                        show = EditorGUILayout.Foldout(show, GetPrefixKey(key));
                        foldout[fullpath] = show;
                    }

                    if (show)
                    {
                        EditorGUI.indentLevel++;

                        LuaTable realValue = value as LuaTable;
                        realValue.ForEach<string, object>((k, v) =>
                        {
                            ShowInternalItem_Playing(fullpath, k, v);
                        });

                        EditorGUI.indentLevel--;
                    }

                    if (!string.IsNullOrEmpty(key))
                    {   
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            if (endSingleVertical)
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        KeyValuePair<int, int> GetFieldSourceIndex(string content, string fieldname)
        {
            var regex = new Regex('.' + fieldname + "[ \r\n\t]*=[ \r\n\t]*{");
            var match = regex.Match(content);

            if (match.Length == 0)
            {
                return new KeyValuePair<int, int>(-1, -1);
            }

            int quoteIndex = match.Groups[0].Index + match.Groups[0].Length;
            int quoteNum = 1;
            int endIndex = -1;

            for (int i = quoteIndex; i < content.Length; i++)
            {
                char ch = content[i];
                if (ch == '{')
                {
                    quoteNum++;
                }

                if (ch == '}')
                {
                    quoteNum--;
                    if (quoteNum == 0)
                    {
                        endIndex = i;
                        break;
                    }
                }
            }

            return new KeyValuePair<int, int>(quoteIndex, endIndex);
        }

        Dictionary<string, int> GetOrderedKey(List<string> keyset, string content)
        {
            var value = new Dictionary<string, int>();

            foreach (var key in keyset)
            {
                var regex = new Regex("[ \r\n\t,{]" + key + "[ \r\n\t]*=");
                var match = regex.Match(content);

                if (match.Length == 0)
                {
                    continue;
                }

                value[key] = match.Groups[0].Index;
            }

            return value;
        }

        List<string> ParseLuaBehaviour_Begin(RealStatePtr L, string filename, ref int originTop, ref LuaTable argsTable)
        {
            originTop = LuaAPI.lua_gettop(L);
            List<string> keylist = null;

            try
            {
                byte[] chunk = System.IO.File.ReadAllBytes(filename);
                object[] rets = Utils.Lua.DoString(chunk, filename);

                if (rets.Length == 0)
                {
                    throw new Exception("lua file " + filename + " need return table type");
                }

                LuaTable table = rets[0] as LuaTable;

                if (table == null)
                {
                    throw new Exception("lua file " + filename + " convert to table type failed");
                }

                argsTable = table.Get<string, LuaTable>(LuaTarget.ArgsName);

                if (argsTable == null)
                {
                    throw new Exception("lua file " + filename + " field " + LuaTarget.ArgsName + " should be table type");
                }

                keylist = new List<string>();

                argsTable.ForEach<string, object>((key, value, valueType) =>
                {
                    if (valueType == LuaTypes.LUA_TTABLE || valueType == LuaTypes.LUA_TNUMBER || valueType == LuaTypes.LUA_TSTRING ||
                        valueType == LuaTypes.LUA_TBOOLEAN || valueType == LuaTypes.LUA_TUSERDATA || valueType == LuaTypes.LUA_TLIGHTUSERDATA)
                    {
                        keylist.Add(key);
                    }
                });
            }
            catch (Exception e)
            {
                LuaAPI.lua_settop(L, originTop);
                throw e;
            }

            return keylist;
        }

        void ParseLuaTarget_End(RealStatePtr L, int originTop)
        {
            LuaAPI.lua_settop(L, originTop);
        }

        List<DescriptorValue> ParseLuaTarget_GetValues(RealStatePtr L, LuaTable argsTable, List<string> keylist)
        {
            List<DescriptorValue> valuelist = new List<DescriptorValue>();

            foreach (var key in keylist)
            {
                Descriptor[] descriptors = null;
                object value = null;
                LuaTypes valueType = LuaTypes.LUA_TNIL;

                argsTable.Get<string, object>(key, out value, out valueType);
                DescriptorValue dv = new DescriptorValue();

                dv.v = ParseLuaTarget_LuaValue(L, value, valueType, out descriptors);
                dv.desc = descriptors;

                valuelist.Add(dv);
            }

            return valuelist;
        }

        object ParseLuaTarget_LuaValue(RealStatePtr L, object value, LuaTypes valueType, out Descriptor[] descriptors)
        {
            descriptors = null;

            if (valueType == LuaTypes.LUA_TTABLE)
            {
                LuaTable realValue = (LuaTable)value;
                Type type = null;

                string typeStr = null;

                realValue.ForEach<int, string>((index, typeString) =>
                {
                    typeStr = typeString;
                    return true;
                });

                if (typeStr != null)
                {
                    type = LuaTarget.GetInternalType(typeStr);
                    if (type == typeof(float))
                    {
                        float min = 0f, max = 0f;
                        int flag = 0;
                        realValue.ForEach<string, float>((name, v) =>
                        {
                            if (name == LuaTarget.NameMin)
                            {
                                min = v;
                                flag++;
                            }
                            else if (name == LuaTarget.NameMax)
                            {
                                max = v;
                                flag++;
                            }
                        });

                        if (flag == 2)
                        {
                            descriptors = new Descriptor[] { new MinMaxDescriptor(min, max) };
                        }
                    }
                    return type != null ? type : value.GetType();
                }
            }
            else if (valueType == LuaTypes.LUA_TNUMBER)
            {
                return System.Convert.ToSingle(value);
            }

            return value;
        }

        static System.Text.StringBuilder sb = new System.Text.StringBuilder();

        public static string GetPrefixKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return key;
            }

            sb.Length = 0;
            var prevChar = '_';
            bool hasContent = false;

            for (int i = 0; i < key.Length; i++)
            {
                var ch = key[i];
                if (char.IsLower(ch))
                {
                    if (!hasContent)
                    {
                        hasContent = true;
                        sb.Append(char.ToUpper(ch));
                    }
                    else
                    {
                        if (char.IsLetter(prevChar))
                        {
                            sb.Append(ch);
                        }
                        else
                        {
                            sb.Append(' ');
                            sb.Append(char.ToUpper(ch));
                        }
                    }
                }
                else if (char.IsUpper(ch))
                {
                    if (!hasContent)
                    {
                        hasContent = true;
                        sb.Append(ch);
                    }
                    else
                    {
                        if (char.IsUpper(prevChar))
                        {
                            sb.Append(' ');
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                    }
                }
                else if (char.IsDigit(ch))
                {
                    hasContent = true;
                    sb.Append(ch);
                }

                prevChar = ch;
            }

            return sb.ToString();
        }
    }

}
