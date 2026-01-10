using UnityEngine;
using System.Collections.Generic;

public class UIStateTrackable : MonoBehaviour 
{
    [Header("记录开关")]
    public bool trackActive = true;    // 物体显隐
    public bool trackTransform = true; // 缩放
    public bool trackSprite = true;    // Image组件图片
    public bool trackCanvasGroup = true; // 透明度

    [Header("额外组件(需手动拖入)")]
    public List<Behaviour> targetComponents = new List<Behaviour>();
}