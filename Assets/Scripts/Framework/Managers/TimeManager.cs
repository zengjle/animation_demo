using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// 获得进程运行时间
//
public class TimeManager : MN.SingletonMono<TimeManager>, IManager {

    System.DateTime startTime;

    public IEnumerator Init(GameObject parent) {

        Debug.Log("Init TimeManager");

        this.gameObject.transform.SetParent(parent.transform);

        startTime = System.DateTime.Now;

        yield return null;
    }

    public void Free() {

        Debug.Log("Free TimeManager");
        GameObject.Destroy(this.gameObject);
    }

    // 从开始到现在的秒数
    public double seconds {
        get {
            return (System.DateTime.Now - startTime).TotalSeconds;
        }
    }

    // 从开始到现在的毫秒数
    public double milliseconds {
        get {
            return (System.DateTime.Now - startTime).TotalMilliseconds;
        }
    }

    // 间隔时间
    public double interval {
        get {
            return Time.deltaTime;
        }
    }
}
