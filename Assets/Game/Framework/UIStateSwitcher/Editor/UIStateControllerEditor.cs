using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UIStateController))]
public class UIStateControllerEditor : Editor {
    private int _sel;
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        var script = (UIStateController)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("【捕获当前状态】", GUILayout.Height(35))) Capture(script);
        if (GUILayout.Button("导出 XML")) script.SaveToXml();
        if (GUILayout.Button("从 XML 导入配置", GUILayout.Height(30))) script.LoadFromXml();

        if (script.allStates.Count > 0) {
            EditorGUILayout.Separator();
            string[] names = script.allStates.ConvertAll(s => s.stateName).ToArray();
            _sel = EditorGUILayout.Popup("测试预览", _sel, names);
            if (GUILayout.Button("切换预览")) script.ApplyStateInstant(_sel);
        }
    }

    private void Capture(UIStateController controller) {
        Undo.RecordObject(controller, "Capture");
        var newState = new UIStateController.UIState { stateName = "State_" + controller.allStates.Count };
        foreach (var t in controller.GetComponentsInChildren<UIStateTrackable>(true)) {
            var objData = new UIStateController.ObjectData {
                path = GetPath(controller.transform, t.transform),
                isActive = t.trackActive ? t.gameObject.activeSelf : true,
                scale = t.trackTransform ? t.transform.localScale : Vector3.one
            };
            if (t.trackCanvasGroup && t.TryGetComponent<CanvasGroup>(out var cg)) {
                objData.hasCG = true; 
                objData.alpha = cg.alpha;
            }
            if (t.trackSprite && t.TryGetComponent<UnityEngine.UI.Image>(out var img))
                objData.spriteName = img.sprite ? img.sprite.name : "";
            
            foreach (var c in t.targetComponents)
                if (c) objData.comps.Add(new UIStateController.ComponentData { typeName = c.GetType().Name, isEnabled = c.enabled });

            newState.objects.Add(objData);
        }
        controller.allStates.Add(newState);
    }

    private string GetPath(Transform r, Transform t) {
        if (r == t) return "";
        string p = t.name;
        while (t.parent && t.parent != r) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}