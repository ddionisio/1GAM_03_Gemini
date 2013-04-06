using UnityEngine;
using System.Collections;

public class Board : MonoBehaviour {
    public enum Action {
        Begin,
        PushRow,
        GameOver,
    }

    public delegate void ActionCallback(Board board, Action act);

    public const string BlockPoolType = "block";
        
    public string blockPickGroup = BlockRandomizer.defaultGroup;
    public string longBlockPickGroup = BlockRandomizer.defaultGroup;

    public int numRow;
    public int numCol;

    public Vector2 tileSize;

    public event ActionCallback actCallback;
            
    private PoolController mBlockPool;
    private Transform mBlockHolder; //this is where blocks are put in, cursor is also here

    private int mMaxBlocks;

    /// <summary>
    /// [row, col]
    /// </summary>
    private Block[][] mTable;

    private bool[] mLongBlockGen;

    private BlockRandomizer mBlockTypePicker;
    private BlockRandomizer mLongBlockTypePicker;

    public int maxBlocks { get { return mMaxBlocks; } }

    public Transform blockHolder { get { return mBlockHolder; } }

    /// <summary>
    /// [row, col]
    /// </summary>
    public Block[][] table {
        get {
            return mTable;
        }
    }

    public void GameOver() {
        //let everyone know, a block should know about it and update itself, also cursor, hopefully!
        if(actCallback != null)
            actCallback(this, Action.GameOver);
    }

    public void PushRow() {
        //let everyone know, a block should know about it and update itself, also cursor, hopefully!
        if(actCallback != null)
            actCallback(this, Action.PushRow);
    }

    /// <summary>
    /// Returns true if there are any blocks on given row
    /// </summary>
    public bool CheckBlocksRow(int row) {
        bool ret = false;

        if(row >= 0 && row < mTable.Length) {
            Block[] tableRow = mTable[row];

            for(int c = 0; c < numCol; c++) {
                if(tableRow[c] != null) {
                    ret = true;
                    break;
                }
            }
        }

        return ret;
    }

    public Block SpawnBlock(Block.Type type, Block.State state, int row, int col, int numRow, int numCol) {
        Transform spawned = mBlockPool.Spawn(BlockPoolType, type.ToString(), null, null);
        if(spawned != null) {
            Block newB = spawned.GetComponent<Block>();
            newB.Init(this, type, row, col, numRow, numCol, state);

            return newB;
        }
        else {
            Debug.LogWarning("Failed to spawn block!");

            return null;
        }
    }

    public void GenerateRow(int rowInd, int numLongBlocks, Block.State state, bool checkPrevType) {
        if(numLongBlocks > 0) {
            ShuffleLongBlocks(numLongBlocks);

            Block.Type pickType = Block.Type.NumTypes;

            for(int c = 0; c < numCol; c++) {
                if(c % 2 == 0 && mLongBlockGen[c / 2]) {
                    //make sure the type picked doesn't make a match
                    pickType = Pick(mLongBlockTypePicker, checkPrevType ? pickType : Block.Type.NumTypes, rowInd, c, 1, 2, 1, 1);

                    SpawnBlock(pickType, state, rowInd, c, 1, 2);

                    c++;
                }
                else {
                    pickType = Pick(mBlockTypePicker, checkPrevType ? pickType : Block.Type.NumTypes, rowInd, c, 1, 1, 1, 1);

                    SpawnBlock(pickType, state, rowInd, c, 1, 1);
                }
            }
        }
        else {
            Block.Type pickType = Block.Type.NumTypes;

            for(int c = 0; c < numCol; c++) {
                pickType = Pick(mBlockTypePicker, checkPrevType ? pickType : Block.Type.NumTypes, rowInd, c, 1, 1, 1, 1);

                SpawnBlock(pickType, state, rowInd, c, 1, 1);
            }
        }
    }

    /// <summary>
    /// Pick a type based on given randomizer making sure nothing will match it on the table
    /// also don't match given prev type (except prevType == NumType)
    /// </summary>
    public Block.Type Pick(BlockRandomizer randomizer, Block.Type prevType, int row, int col, int aNumRow, int aNumCol, int rowDir, int colDir) {
        Block.Type pickType = randomizer.pick;

        //check if it will check-match within table
        if(CheckMatch(pickType, row, col, aNumRow, aNumCol, rowDir, colDir) || pickType == prevType) {
            //keep getting to the next until there is no match
            for(int i = 0; i < randomizer.items.Length; i++) {
                pickType = randomizer.next;
                if(!CheckMatch(pickType, row, col, aNumRow, aNumCol, rowDir, colDir) && pickType != prevType)
                    break;
            }
        }

        return pickType;
    }

    public bool CheckMatch(Block.Type type, int row, int col, int aNumRow, int aNumCol, int rowDir, int colDir) {
        return GetMaxMatchCount(type, row, col, aNumRow, aNumCol, rowDir, colDir) >= 2;
    }

    public int GetMaxMatchCount(Block.Type type, int row, int col, int aNumRow, int aNumCol, int rowDir, int colDir) {
        BlockConfig blockConfig = BlockConfig.instance;

        int count = 0;

        //vertical
        int minCol, maxCol;
        if(colDir > 0) {
            minCol = col;
            maxCol = col + aNumCol - 1;
        }
        else {
            minCol = col - aNumCol - 1;
            maxCol = col;
        }

        //cap
        if(minCol < 0)
            minCol = 0;
        if(maxCol >= numCol)
            maxCol = numCol - 1;

        for(int c = minCol; c <= maxCol; c++) {
            int rowCount = aNumRow-1;

            //check upwards
            for(int r = rowDir > 0 ? row + aNumRow : row + 1; r < table.Length; r++) {
                Block b = table[r][c];
                if(b != null && blockConfig.CheckMatch(b.type, type))
                    rowCount++;
                else
                    break;
            }

            //check downwards
            for(int r = rowDir > 0 ? row - 1 : row - aNumRow; r >= 0; r--) {
                Block b = table[r][c];
                if(b != null && blockConfig.CheckMatch(b.type, type))
                    rowCount++;
                else
                    break;
            }

            if(rowCount > count)
                count = rowCount;
        }

        //horizontal
        int minRow, maxRow;
        if(rowDir > 0) {
            minRow = row;
            maxRow = row + aNumRow - 1;
        }
        else {
            minRow = row - aNumRow - 1;
            maxRow = row;
        }

        //cap
        if(minRow < 0)
            minRow = 0;
        if(maxRow >= mTable.Length)
            maxRow = mTable.Length - 1;

        for(int r = minRow; r <= maxRow; r++) {
            int colCount = aNumCol - 1;

            //check right
            for(int c = colDir > 0 ? col + aNumCol : col + 1; c < table.Length; c++) {
                Block b = table[r][c];
                if(b != null && blockConfig.CheckMatch(b.type, type))
                    colCount++;
                else
                    break;
            }

            //check left
            for(int c = rowDir > 0 ? col - 1 : col - aNumCol; c >= 0; c--) {
                Block b = table[r][c];
                if(b != null && blockConfig.CheckMatch(b.type, type))
                    colCount++;
                else
                    break;
            }

            if(colCount > count)
                count = colCount;
        }

        return count;
    }
    
    void Awake() {
        mBlockPool = GetComponent<PoolController>();
                
        mMaxBlocks = numRow * numCol * 2 + numCol;

        mLongBlockGen = new bool[numCol/2];

        mTable = M8.ArrayUtil.NewDoubleArray<Block>(numRow * 2, numCol);
    }

    // Use this for initialization
    void Start() {
        mBlockTypePicker = BlockRandomizer.GetRandomizer(blockPickGroup);
        mLongBlockTypePicker = BlockRandomizer.GetRandomizer(longBlockPickGroup);

        mBlockHolder = mBlockPool.GetDefaultParent(BlockPoolType);

        mBlockPool.Expand(BlockPoolType, maxBlocks);
                
        //check if there are pre-added blocks on the board
        foreach(Transform block in mBlockHolder) {
            Block b = block.GetComponent<Block>();
            if(b != null)
                b.Init(this, Block.State.Idle);
        }

        if(actCallback != null)
            actCallback(this, Action.Begin);
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

    private void ShuffleLongBlocks(int count) {

        for(int i = 0, c = 0; i < mLongBlockGen.Length; i++, c++) {
            mLongBlockGen[i] = c < count;
        }

        M8.ArrayUtil.Shuffle(mLongBlockGen);
    }
}
