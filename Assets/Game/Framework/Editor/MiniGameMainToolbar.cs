using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

static class MiniGameMainToolbar
{
    [MainToolbarElement("Examples/资源刷新", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement CreateRefreshButton()
    {
        var icon = EditorGUIUtility.IconContent("SettingsIcon").image as Texture2D;
        var content = new MainToolbarContent("资源刷新", icon, "资源刷新");
        return new MainToolbarButton(content, 
            () => { Debug.Log("Refresh Asset Button Clicked!");
            AssetDatabase.Refresh(); }
        );
    }

    [MainToolbarElement("Examples/启动场景", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement CreateEnterPlaymodeButton()
    {
        var icon = EditorGUIUtility.IconContent("PlayButton").image as Texture2D;
        var content = new MainToolbarContent("启动场景", icon, "启动场景");
        return new MainToolbarButton(content, 
            () => { 
                Debug.Log("启动场景!");
                EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(0));
                EditorApplication.EnterPlaymode();
            }
        );
    }

    [MainToolbarElement("Examples/Open Project Settings", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement ProjectSettingsButton()
    {
        var icon = EditorGUIUtility.IconContent("SettingsIcon").image as Texture2D;
        var content = new MainToolbarContent(icon);
        return new MainToolbarButton(content, () => { SettingsService.OpenProjectSettings(); });
    }
}