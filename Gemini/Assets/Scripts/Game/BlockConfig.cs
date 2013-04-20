using UnityEngine;
using System.Collections;

//global setting for blocks
public class BlockConfig : MonoBehaviour {
    [System.Serializable]
    public class BlockInfo {
        public bool hasIcon = true;
        public tk2dSpriteAnimation icon;
        public bool iconAlwaysUp = true;

        public bool hasPanel = true;
        public tk2dSpriteCollectionData panelSpriteCollection;
        public int panelSpriteId;

        public Color color = Color.white;

        public Block.Flag flags = (Block.Flag)0;

        private int[] mSpriteClipIds;

        public int[] spriteClipIds { get { return mSpriteClipIds; } }

        public void Init() {
            mSpriteClipIds = new int[(int)Block.SpriteState.NumStates];

            for(int c = 0; c < (int)Block.SpriteState.NumStates; c++) {
                mSpriteClipIds[c] = icon.GetClipIdByName(((Block.SpriteState)c).ToString());
                if(mSpriteClipIds[c] == -1)
                    mSpriteClipIds[c] = 0;
            }
        }
    }
    
    public BlockInfo[] blockTypes;

    public float destroyFlashDelay;
    public float destroyDelay;

    public float fallDelay;
    public float fallSpeed; //pixel/sec
            
    private static BlockConfig mInstance;

    public static BlockConfig instance { get { return mInstance; } }
            
    void OnDestroy() {
        mInstance = null;
    }

    void Awake() {
        mInstance = this;

        //configure data
        for(int i = 0; i < blockTypes.Length; i++) {
            blockTypes[i].Init();
        }
    }
}
