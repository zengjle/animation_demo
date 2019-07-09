using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class popUpCom : MonoBehaviour
{
    public float changeTime = 0.1f;
    // public float restTime = 0.5f;
    public int vertical = 1;
    public int horizontal = 0;
    public Button btnOpen;
    public Button btnClose;
    //openState    "normal":不变化  "largen",:变大 "shrink":变小
    public string openState = "normal";

    int springbackState = 0;//为0时物体放大,为1时为放大后的回弹状态
    void Start()
    {
        btnOpen.onClick.AddListener(ViewOpen);
        btnClose.onClick.AddListener(ViewClose);
        ViewOpen();
    }

    void ViewOpen(){
        openState = "largen";
        if(vertical == 1 && horizontal ==1){
        transform.localScale = new Vector3(0,0,1);    
        }else{
        transform.localScale = new Vector3(vertical,horizontal,1);
        }
        springbackState = 0;
        Debug.Log("open!");
    }

    void ViewClose(){
        openState = "shrink";
    }

    void FadeIn(){
        
    }

    void FadeOut(){

    }
    // Update is called once per frame
    void Update()
    {
        // gameObject.GetComponent<Image>().color = new Color().a;

        if(openState == "normal"){
            return;
        }
        // restTime += Time.deltaTime;
        // if(restTime > changeTime) return;
        if(openState == "largen"){
            if(vertical == 1){
                if(transform.localScale.y >= 1.1f || springbackState == 1 ){
                    transform.localScale -= new Vector3(0,(Time.deltaTime * 1.2f) / changeTime,0);
                    springbackState = 1;
                    if(transform.localScale.y <= 1){
                        transform.localScale = new Vector3( 1, 1, 1);
                        openState = "normal";
                    }
                }else if(springbackState == 0){
                    transform.localScale += new Vector3(0,(Time.deltaTime * 1.2f) / changeTime,0);
                }
                Debug.Log("++++++++++y" + transform.localScale.y);
            }
            if(horizontal == 1){
                if(transform.localScale.x >= 1.1f || springbackState == 1 ){
                    transform.localScale -= new Vector3((Time.deltaTime * 1.2f) / changeTime,0,0);
                    springbackState = 1;
                    if(transform.localScale.x <= 1){
                        transform.localScale = new Vector3( 1, 1, 1);
                        openState = "normal";
                    }
                }else if(springbackState == 0){
                    transform.localScale += new Vector3((Time.deltaTime * 1.2f) / changeTime,0,0);
                }
                 Debug.Log("++++++++++x" + transform.localScale.x);
            }
        }else if(openState == "shrink"){
            if(vertical == 1){
                transform.localScale -= new Vector3(0,Time.deltaTime / changeTime,0);
                Debug.Log("----------y" + transform.localScale.y);
                if(transform.localScale.y <= 0){
                    transform.localScale = new Vector3( 0, 0, 1);
                    openState = "normal";
                }
            }
            if(horizontal == 1){
                transform.localScale -= new Vector3(Time.deltaTime / changeTime,0,0);
                Debug.Log("----------x" + transform.localScale.x);
                if(transform.localScale.x <= 0){
                    transform.localScale = new Vector3( 0, 0, 1);
                    openState = "normal";
                }
            }
        }
    }
}
