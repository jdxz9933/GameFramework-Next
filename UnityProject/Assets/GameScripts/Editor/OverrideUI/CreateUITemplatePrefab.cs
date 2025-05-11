// ================================================
//描 述 :  
//作 者 : 杜鑫 
//创建时间 : 2021-08-12 00-05-17  
//修改作者 : 杜鑫 
//修改时间 : 2021-08-12 00-05-17  
//版 本 : 0.1 
// ===============================================

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Deer.Editor
{
    public class CreateUITemplatePrefab
    {
        [MenuItem("GameObject/UI/U_UIForm", false, 0)]
        static void CreateUIFormObj(MenuCommand menuCommand)
        {
            GameObject panel = SaveObject(menuCommand, "UIForm");
            panel.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            panel.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            panel.GetComponent<RectTransform>().offsetMin = new Vector2(0, 0);
            panel.GetComponent<RectTransform>().offsetMax = new Vector2(0, 0);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("GameObject/UI/U_UIPanel", false, 0)]
        static void CreateUIPanelObj(MenuCommand menuCommand)
        {
            GameObject panel = SaveObject(menuCommand, "UIPanel");
            panel.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            panel.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            panel.GetComponent<RectTransform>().offsetMin = new Vector2(0, 0);
            panel.GetComponent<RectTransform>().offsetMax = new Vector2(0, 0);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("GameObject/UI/U_Progress", false, 1)]
        static void CreateProgressObj(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "Progress");
        }

        [MenuItem("GameObject/UI/U_Text", false, 2)]
        static void CreateTextObj(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "Txt_Text");
        }


        [MenuItem("GameObject/UI/U_Button", false, 20)]
        static void CreateUISuperButton(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "Btn_UIButton");
        }
        [MenuItem("GameObject/UI/U_Toggle - TextMeshPro", false, 21)]
        static void CreateUIToggle(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "UIToggle");
        }
        [MenuItem("GameObject/UI/U_InputField", false, 22)]
        static void CreateUIInputField(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "Input_InputField");
        }
        [MenuItem("GameObject/UI/U_UIModel", false, 23)]
        static void CreateUIModel(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "UIModel");
        }
        [MenuItem("GameObject/UI/U_ScrollView/HListScroll View", false, 22)]
        static void CreateHListScroll(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "ScrollView/HListV_ViewScroll");
        }
        [MenuItem("GameObject/UI/U_ScrollView/HGridScroll View", false, 23)]
        static void CreateHGridScroll(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "ScrollView/HGridV_ViewScrol");
        }

        [MenuItem("GameObject/UI/U_ScrollView/VListScroll View", false, 24)]
        static void CreateVListScroll(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "ScrollView/VListV_ViewScrol");
        }

        [MenuItem("GameObject/UI/U_ScrollView/VGridScroll View", false, 25)]
        static void CreateVGridScroll(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "ScrollView/VGridV_ViewScrol");
        }
        [MenuItem("GameObject/UI/U_ScrollView/ScrollVItemPrefab", false, 26)]
        static void CreateScrollVItemPrefab(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "ScrollView/ViewItem");
        }

        [MenuItem("GameObject/UI/U_SpriteAnimation", false, 2)]
        static void CreateUGUISpriteAnimation(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "UISpriteAnimation");
        }
        [MenuItem("GameObject/UI/U_UIHealthBar", false, 3)]
        static void CreateUIHealthbar(MenuCommand menuCommand)
        {
            SaveObject(menuCommand, "UIHealthBar");
        }
        static GameObject SaveObject(MenuCommand menuCommand, string prefabName, string objName = "")
        {
            var path = $@"Assets\GameMain\UI\UITemplate\{prefabName}.prefab";
            path = path.Replace("\\", "/");
            GameObject prefab = (GameObject)AssetDatabase.LoadMainAssetAtPath(path);
            if (prefab)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (!string.IsNullOrEmpty(objName))
                {
                    inst.gameObject.name = objName;
                }
                var img = inst.GetComponent<Image>();
                if (img)
                {
                    img.color = new Color(1, 1, 1, 1);
                }
                var text = inst.GetComponent<Text>();
                if (text)
                {
                    text.text = "";
                }
                GameObjectUtility.SetParentAndAlign(inst, menuCommand.context as GameObject);
                Undo.RegisterCreatedObjectUndo(inst, $"Create {inst.name}__" + inst.name);
                Selection.activeObject = inst;
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                return inst;
            }
            return null;
        }
    }
}