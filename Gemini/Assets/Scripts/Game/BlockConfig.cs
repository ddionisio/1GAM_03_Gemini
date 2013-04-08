using UnityEngine;
using System.Collections;

//global setting for blocks
public class BlockConfig : MonoBehaviour {
    [System.Serializable]
    public class BlockInfo {
        public tk2dSpriteAnimation icon;

        public tk2dSpriteCollectionData panelSpriteCollection;
        public int panelSpriteId;

        public Color color = Color.white;
    }

    public class BlockData {
        public int[] spriteClipIds; //id for each state
    }
        
    public BlockInfo[] blockTypes;

    public float destroyFlashDelay;

    public float fallDelay;
    public float fallSpeed; //pixel/sec
            
    private static BlockConfig mInstance;

    private BlockData[] mBlockData;

    public static BlockConfig instance { get { return mInstance; } }

    public BlockData[] blockData { get { return mBlockData; } }

    public bool CheckMatch(Block.Type source, Block.Type target) {
        //TODO: special blocks matching, like all-color match or something
        //default: same type
        return source == target;
    }

    void OnDestroy() {
        mInstance = null;
    }

    void Awake() {
        mInstance = this;

        //configure data
        mBlockData = new BlockData[(int)Block.Type.NumTypes];

        for(int i = 0; i < blockTypes.Length; i++) {
            mBlockData[i] = new BlockData();

            mBlockData[i].spriteClipIds = new int[(int)Block.SpriteState.NumStates];

            for(int c = 0; c < (int)Block.SpriteState.NumStates; c++) {
                mBlockData[i].spriteClipIds[c] = blockTypes[i].icon.GetClipIdByName(((Block.SpriteState)c).ToString());
                if(mBlockData[i].spriteClipIds[c] == -1)
                    mBlockData[i].spriteClipIds[c] = 0;
            }
        }
    }
}
