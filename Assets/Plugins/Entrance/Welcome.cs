using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Welcome : MonoBehaviour {

    public Image Background;
    public Text VersionInfo;

    void Awake() {
        this.gameObject.name = "welcome";
    }
}
