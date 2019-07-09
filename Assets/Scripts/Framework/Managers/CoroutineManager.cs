using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using XLua;

public class CoroutineManager : MN.SingletonMono<CoroutineManager>, IManager {

    System.Action ScheduleUpdate;

    public IEnumerator Init(GameObject parent) {

        this.gameObject.transform.SetParent(parent.transform);

        yield return null;
    }

    public void Free() {
        GameObject.Destroy(this.gameObject);
    }

    public void StartupEnv(string table, string name) {
        LuaManager.Instance.GetFunction(table, name, ref ScheduleUpdate);
    }

    void Update() {
        if (ScheduleUpdate != null) {
            ScheduleUpdate();
        }
    }
}
