using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIStateController : MonoBehaviour
{
    [Serializable]
    public class ComponentData {
        public string typeName;
        public bool isEnabled;
    }

    [Serializable]
    public class ObjectData {
        public string path;
        public bool isActive;
        public Vector3 scale = Vector3.one;
        public float alpha = 1f;
        public bool hasCG;
        public string spriteName;
        public List<ComponentData> comps = new List<ComponentData>();
    }

    [Serializable]
    public class UIState {
        public string stateName;
        public List<ObjectData> objects = new List<ObjectData>();
    }

    public List<UIState> allStates = new List<UIState>();
    public float duration = 0.3f;
    private Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

    // 运行时丝滑切换
    public void SwitchState(int index) {
        if (index < 0 || index >= allStates.Count) return;
        var data = allStates[index];

        foreach (var obj in data.objects) {
            Transform t = transform.Find(obj.path);
            if (!t) continue;

            t.gameObject.SetActive(obj.isActive);
            if (obj.isActive) t.DOScale(obj.scale, duration).SetEase(Ease.OutCubic);
            
            if (obj.hasCG && t.TryGetComponent<CanvasGroup>(out var cg))
                cg.DOFade(obj.alpha, duration);

            if (!string.IsNullOrEmpty(obj.spriteName) && t.TryGetComponent<Image>(out var img))
                img.sprite = LoadSprite(obj.spriteName);

            foreach (var c in obj.comps) {
                var comp = t.GetComponent(c.typeName) as Behaviour;
                if (comp) comp.enabled = c.isEnabled;
            }
        }
    }

    // 编辑器或瞬时切换
    public void ApplyStateInstant(int index) {
        if (index < 0 || index >= allStates.Count) return;
        var data = allStates[index];
        foreach (var obj in data.objects) {
            Transform t = transform.Find(obj.path);
            if (!t) continue;
            t.gameObject.SetActive(obj.isActive);
            t.localScale = obj.scale;
            if (obj.hasCG && t.TryGetComponent<CanvasGroup>(out var cg)) cg.alpha = obj.alpha;
            if (!string.IsNullOrEmpty(obj.spriteName) && t.TryGetComponent<Image>(out var img))
                img.sprite = LoadSprite(obj.spriteName);
        }
    }

    private Sprite LoadSprite(string name) {
        if (_cache.TryGetValue(name, out var s)) return s;
        var loaded = Resources.Load<Sprite>(name);
        if (loaded) _cache[name] = loaded;
        return loaded;
    }

    public void SaveToXml() {
        string path = Path.Combine(Application.dataPath, $"UIConfig_{gameObject.name}.xml");
        var ser = new XmlSerializer(typeof(List<UIState>));
        using (var sw = new StreamWriter(path)) ser.Serialize(sw, allStates);
        Debug.Log("XML Saved: " + path);
    }

    // 在 UIStateController 类中增加此方法
    public void LoadFromXml()
    {
        string path = Path.Combine(Application.dataPath, $"UIConfig_{gameObject.name}.xml");
        if (!File.Exists(path))
        {
            Debug.LogError("找不到配置文件: " + path);
            return;
        }

        XmlSerializer serializer = new XmlSerializer(typeof(List<UIState>));
        using (var reader = new StreamReader(path))
        {
            // 将 XML 数据覆盖到内存中的列表
            allStates = (List<UIState>)serializer.Deserialize(reader);
        }
        Debug.Log($"[UIState] 成功从 XML 加载了 {allStates.Count} 个状态！");
    }
}