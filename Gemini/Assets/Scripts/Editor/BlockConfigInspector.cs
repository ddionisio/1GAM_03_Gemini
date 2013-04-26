using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(BlockConfig))]
public class BlockConfigInspector : Editor {
    private tk2dGenericIndexItem[] mAnimLibs = null;
    private string[] mAnimLibNames = null;
    private bool mAnimInitialized = false;

    private bool blockInfoFoldout = true;

    private void InitAnimLib() {
        if(!mAnimInitialized) {
            mAnimLibs = tk2dEditorUtility.GetOrCreateIndex().GetSpriteAnimations();
            if(mAnimLibs != null) {
                mAnimLibNames = new string[mAnimLibs.Length];
                for(int i = 0; i < mAnimLibs.Length; ++i) {
                    mAnimLibNames[i] = mAnimLibs[i].AssetName;
                }
            }
            mAnimInitialized = true;
        }
    }

    private void PanelSpriteChangedCallbackImpl(tk2dSpriteCollectionData spriteCollection, int spriteId, object data) {
        BlockConfig.BlockInfo info = (BlockConfig.BlockInfo)data;
        info.panelSpriteCollection = spriteCollection;
        info.panelSpriteId = spriteId;
    }
    
    public override void OnInspectorGUI() {
        FontStyle defaultLabelFontStyle = GUI.skin.label.fontStyle;

        //initialize sprite animation lib
        InitAnimLib();

        //setup block types
        BlockConfig data = target as BlockConfig;

        int countType = (int)Block.Type.NumTypes;

        if(data.blockTypes == null) {
            data.blockTypes = new BlockConfig.BlockInfo[countType];
        }
        else if(data.blockTypes.Length != countType) {
            System.Array.Resize(ref data.blockTypes, countType);
        }

        blockInfoFoldout = EditorGUILayout.Foldout(blockInfoFoldout, "Block Types");

        if(blockInfoFoldout) {
            for(int typeInd = 0; typeInd < data.blockTypes.Length; typeInd++) {
                Block.Type type = (Block.Type)typeInd;
                BlockConfig.BlockInfo info;

                if(data.blockTypes[typeInd] == null) {
                    info = data.blockTypes[typeInd] = new BlockConfig.BlockInfo();
                }
                else {
                    info = data.blockTypes[typeInd];
                }

                info.type = (Block.Type)typeInd;

                GUILayout.BeginVertical(GUI.skin.box);

                ////type
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUILayout.Label(type.ToString());
                GUI.skin.label.fontStyle = defaultLabelFontStyle;

                ////icon
                GUILayout.BeginVertical(GUI.skin.box);

                info.hasIcon = GUILayout.Toggle(info.hasIcon, "Icon");

                if(info.hasIcon) {
                    //anim for icon
                    if(mAnimLibs == null) {
                        GUILayout.Label("no animation libraries found");
                        if(GUILayout.Button("Refresh")) {
                            mAnimInitialized = false;
                            InitAnimLib();
                        }
                    }
                    else {
                        int selAnimLib = 0;

                        if(info.icon == null) {
                            info.icon = mAnimLibs[selAnimLib].GetAsset<tk2dSpriteAnimation>();
                        }
                        else {
                            string selectedGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(info.icon));
                            for(int i = 0; i < mAnimLibs.Length; ++i) {
                                if(mAnimLibs[i].assetGUID == selectedGUID) {
                                    selAnimLib = i;
                                    break;
                                }
                            }
                        }

                        int newAnimLib = EditorGUILayout.Popup("Anim", selAnimLib, mAnimLibNames);
                        if(newAnimLib != selAnimLib) {
                            info.icon = mAnimLibs[newAnimLib].GetAsset<tk2dSpriteAnimation>();
                        }
                    }

                    info.iconAlwaysUp = GUILayout.Toggle(info.iconAlwaysUp, "Always Up");
                }
                else {
                    info.icon = null;
                    info.iconAlwaysUp = false;
                }

                GUILayout.EndVertical();

                ////panel sprite
                GUILayout.BeginVertical(GUI.skin.box);

                info.hasPanel = GUILayout.Toggle(info.hasPanel, "Panel");
                if(info.hasPanel) {
                    tk2dSpriteGuiUtility.SpriteSelector(info.panelSpriteCollection, info.panelSpriteId, PanelSpriteChangedCallbackImpl, info);
                }
                else {
                    info.panelSpriteCollection = null;
                    info.panelSpriteId = 0;
                }

                GUILayout.EndVertical();

                info.color = EditorGUILayout.ColorField("Color", info.color);

                GUILayout.EndVertical();
            }
        }

        M8.Editor.Utility.DrawSeparator();

        data.destroyFlashDelay = EditorGUILayout.FloatField("Destroy Flash Delay", data.destroyFlashDelay);
        data.destroyDelay = EditorGUILayout.FloatField("Destroy Delay", data.destroyDelay);
        data.fallDelay = EditorGUILayout.FloatField("Fall Delay", data.fallDelay);
        data.fallSpeed = EditorGUILayout.FloatField("Fall Speed", data.fallSpeed);
    }
}