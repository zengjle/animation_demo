using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class Comon : MN.SingletonMono<Comon>, IManager
{

    private Dictionary<string,PlayableBinding> bindingDict;
    public PlayableDirector qPlayableDirector = null;

    public IEnumerator Init(GameObject parent) {
        Debug.Log("Init Comon");

        this.gameObject.transform.SetParent(parent.transform);
        yield return null;
    }

    public void testFun(string str){
        Debug.Log("调用到CS脚本!!!!!!!!!!!!!!!!!!!!!!"+str);   
    }

    //初始化binding列表
    public void initBindingDict(PlayableDirector PladPlayableDirector) {
        bindingDict = new Dictionary<string, PlayableBinding>();
        qPlayableDirector = PladPlayableDirector;
        foreach (PlayableBinding pb in PladPlayableDirector.playableAsset.outputs){
                    if (!bindingDict.ContainsKey(pb.streamName)){
                        bindingDict.Add(pb.streamName, pb);
                    }
                }
    }

    //  str : binding名称
    //  Obj : 需要加入binding的对象
    public void setPlayableBinding(string str, GameObject Obj){
        qPlayableDirector.SetGenericBinding(bindingDict[str].sourceObject, Obj);
    }

    //根据track名删除其绑定的对象
    public void ClearGenericBinding(string key){
        qPlayableDirector.ClearGenericBinding(bindingDict[key].sourceObject);
    }

    public void play(){
        qPlayableDirector.Play();
    }

    public void Free() {
        GameObject.Destroy(this.gameObject);
    }
}
