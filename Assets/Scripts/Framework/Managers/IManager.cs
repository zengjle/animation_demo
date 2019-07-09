using System.Collections;
using UnityEngine;

public interface IManager {

    IEnumerator Init(GameObject parent);
    void Free();
}