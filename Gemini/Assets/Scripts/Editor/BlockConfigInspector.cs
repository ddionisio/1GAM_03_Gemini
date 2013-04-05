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

                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.Label(type.ToString());

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

                    int newAnimLib = EditorGUILayout.Popup("Icon", selAnimLib, mAnimLibNames);
                    if(newAnimLib != selAnimLib) {
                        info.icon = mAnimLibs[newAnimLib].GetAsset<tk2dSpriteAnimation>();
                    }
                }

                //panel sprite
                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.Label("Panel");
                tk2dSpriteGuiUtility.SpriteSelector(info.panelSpriteCollection, info.panelSpriteId, PanelSpriteChangedCallbackImpl, info);

                GUILayout.EndVertical();

                info.color = EditorGUILayout.ColorField("Color", info.color);

                GUILayout.EndVertical();
            }
        }

        M8.Editor.Utility.DrawSeparator();

        data.fallDelay = EditorGUILayout.FloatField("Fall Delay", data.fallDelay);
        data.fallSpeed = EditorGUILayout.FloatField("Fall Speed", data.fallSpeed);
    }
}