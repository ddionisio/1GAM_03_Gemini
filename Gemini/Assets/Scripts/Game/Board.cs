using UnityEngine;
using System.Collections;

public class Board : MonoBehaviour {
    public const string BlockPoolType = "block";

    public int numRow;
    public int numCol;

    public Vector2 tileSize;
            
    private PoolController mBlockPool;
    private Transform mBlockHolder; //this is where blocks are put in

    private int mMaxBlocks;

    /// <summary>
    /// [row, col]
    /// </summary>
    private Block[,] mTable;

    public int maxBlocks { get { return mMaxBlocks; } }

    public Transform blockHolder { get { return mBlockHolder; } }

    /// <summary>
    /// [row, col]
    /// </summary>
    public Block[,] table {
        get {
            return mTable;
        }
    }

    void Awake() {
        mBlockPool = GetComponent<PoolController>();
                
        mMaxBlocks = numRow * numCol * 2 + numCol;

        mTable = new Block[numRow*2, numCol];
    }

    // Use this for initialization
    void Start() {
        mBlockHolder = mBlockPool.GetDefaultParent(BlockPoolType);

        mBlockPool.Expand(BlockPoolType, maxBlocks);
                
        //check if there are pre-added blocks on the board
        foreach(Transform block in mBlockHolder) {
            Block b = block.GetComponent<Block>();
            b.Init(this);
        }
    }

    // Update is called once per frame
    void Update() {

    }

    void OnDrawGizmos() {
        Gizmos.color = Color.cyan;

        if(tileSize != Vector2.zero) {
            Vector3 pos = transform.position;

            for(int r = 0; r < numRow; r++) {
                for(int c = 0; c < numCol; c++) {
                    Vector3 curPos = pos + new Vector3(tileSize.x * 0.5f + c * tileSize.x, tileSize.y * 0.5f + r * tileSize.y, 0.0f);
                    Gizmos.DrawWireCube(curPos, new Vector3(tileSize.x, tileSize.y, 1.0f));
                }
            }
        }
    }
}
