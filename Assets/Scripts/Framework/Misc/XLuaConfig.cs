using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using XLua;

public static class XLuaConfig
{
    [LuaCallCSharp]
    public static List<Type> LuaCallCSharp {
        get {
            return new List<Type>()
            {
                typeof(WaitForSeconds),
                typeof(WWW),
                typeof(System.Object),
                typeof(UnityEngine.Object),
                typeof(Transform),
                typeof(PlayerPrefs),
            };
        }
    }

    [LuaCallCSharp]
    [ReflectionUse]
    public static List<Type> LuaCallCSharpReflection {
        get {
            return new List<Type>() {
                typeof(UnityEngine.Application),
                typeof(UnityEngine.Screen),
            };
        }
    }

    [CSharpCallLua]
    public static List<Type> CSharpCallLua {
        get {
            return new List<Type>()
            {
                typeof(IEnumerator),
                typeof(System.Action<float>),
                typeof(System.Action<int>),
                // typeof(GeneralDataSource.IRegisterComponent),
                // typeof(GeneralDataSource.IElementNum),
                // typeof(GeneralDataSource.IElementSize),
                // typeof(ScrollList.OnItemRender),
                // typeof(PageView.OnPageViewSliding),
                // typeof(PageView.OnItemStateChanged),
                // typeof(PageViewSource.IElements),
                // typeof(PageViewSource.ICenterIndex),
                // typeof(VirtualJoystick.OnDragging),
                // typeof(VirtualJoystick.OnTapDown),
                // typeof(VirtualJoystick.OnTapUp),
            };
        }
    }

    [BlackList]
    public static List<List<string>> BlackList = new List<List<string>>() {
        new List<string>(){"UnityEngine.Light", "shadowRadius"},
        new List<string>(){"UnityEngine.Light", "shadowAngle"}
    };
}
