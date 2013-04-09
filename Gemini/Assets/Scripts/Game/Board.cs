using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Board : MonoBehaviour {
    public enum Action {
        Begin,
        PushRow,
        GameOver,
    }
        
    public delegate void ActionCallback(Board board, Action act);
    public delegate void EvalBlockCallback(Block block);
            
    public const string BlockPoolType = "block";
        
    public string blockPickGroup = BlockRandomizer.defaultGroup;
    public string longBlockPickGroup = BlockRandomizer.defaultGroup;

    public int numRow;
    public int numCol;

    public Vector2 tileSize;

    public event ActionCallback actCallback;
    public event EvalBlockCallback evalCallback;
                
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

    public PoolController pool { get { return mBlockPool; } }

    public static void GetIndexRange(int start, int count, int dir, int maxCount, out int min, out int max) {
        if(dir > 0) {
            min = start;
            max = start + count - 1;
        }
        else {
            min = start - (count - 1);
            max = start;
        }

        //cap
        if(min < 0)
            min = 0;
        if(max >= maxCount)
            max = maxCount - 1;
    }

    public void Eval(Block b) {
        if(evalCallback != null)
            evalCallback(b);
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

    //goes through blocks upwards or downwards and get the matches
    //also adds given block to output if it hasn't been added already
    //return number of matches found
    private int GetNeightborMatchesRowRecurse(Block block, List<Block> output, bool upwards) {
        int rowCount = 0;

        BlockConfig blockConfig = BlockConfig.instance;

        Block.Type type = block.type;
        int colDir = block.tileDir.col, rowDir = block.tileDir.row, col = block.tilePos.col, row = block.tilePos.row;
        int sizeCol = block.tileSize.col, sizeRow = block.tileSize.row;

        //if it's already matched, it's guaranteed to be in output already
        if((block.flags & Block.Flag.Match) == 0) {
            block.flags |= Block.Flag.Match;
            output.Add(block);
        }

        //vertical
        int minCol, maxCol;
        GetIndexRange(col, sizeCol, colDir, numCol, out minCol, out maxCol);

        int r;
        if(upwards) {
            r = rowDir > 0 ? row + sizeRow : row + 1;
        }
        else {
            r = rowDir > 0 ? row - 1 : row - sizeRow;
        }

        if(r >= 0 && r < numRow) {
            for(int c = minCol; c <= maxCol; c++) {
                Block b = table[r][c];
                if(b != null && b.state == Block.State.Idle && (b.flags & Block.Flag.MatchLock) == 0 && blockConfig.CheckMatch(b.type, type)) {
                    rowCount = GetNeightborMatchesRowRecurse(b, output, upwards) + 1;
                }
            }
        }

        return rowCount;
    }

    //goes through blocks left or right and get the matches
    //also adds given block to output if it hasn't been added already
    //return new colCount
    private int GetNeightborMatchesColRecurse(Block block, List<Block> output, bool leftward) {
        int colCount = 0;

        BlockConfig blockConfig = BlockConfig.instance;

        Block.Type type = block.type;
        int colDir = block.tileDir.col, rowDir = block.tileDir.row, col = block.tilePos.col, row = block.tilePos.row;
        int sizeCol = block.tileSize.col, sizeRow = block.tileSize.row;

        //if it's already matched, it's guaranteed to be in output already
        if((block.flags & Block.Flag.Match) == 0) {
            block.flags |= Block.Flag.Match;
            output.Add(block);
        }

        //horizontal
        int minRow, maxRow;
        GetIndexRange(row, sizeRow, rowDir, numRow, out minRow, out maxRow);

        int c;
        if(leftward) {
            c = colDir > 0 ? col + sizeCol : col + 1;
        }
        else {
            c = colDir > 0 ? col - 1 : col - sizeCol;
        }

        if(c >= 0 && c < numCol) {
            for(int r = minRow; r <= maxRow; r++) {
                Block b = table[r][c];
                if(b != null && b.state == Block.State.Idle && (b.flags & Block.Flag.MatchLock) == 0 && blockConfig.CheckMatch(b.type, type)) {
                    colCount = GetNeightborMatchesColRecurse(b, output, leftward) + 1;
                }
            }
        }

        return colCount;
    }

    /// <summary>
    /// Add blocks to output if there are any matches >= 3 based on given block,
    /// the given block will also be added if we did get matches.
    /// Returns the number of blocks added to output
    /// </summary>
    public int GetMatches(Block block, List<Block> output) {
        int lastCount = output.Count;

        int lastAddIndex = output.Count - 1;

        //check vertical
        int rowCount = 1;

        rowCount += GetNeightborMatchesRowRecurse(block, output, true);
        rowCount += GetNeightborMatchesRowRecurse(block, output, false);

        if(rowCount < 3) {
            //clear out the added crap
            for(int i = lastAddIndex + 1; i < output.Count; i++) {
                output[i].flags ^= Block.Flag.Match;
            }
            output.RemoveRange(lastAddIndex + 1, output.Count - lastAddIndex - 1);
        }
        else {
            lastAddIndex = output.Count - 1;
        }

        //check horizontal
        int colCount = 1;

        colCount += GetNeightborMatchesColRecurse(block, output, true);
        colCount += GetNeightborMatchesColRecurse(block, output, false);

        if(colCount < 3) {
            //clear out the added crap
            for(int i = lastAddIndex + 1; i < output.Count; i++) {
                output[i].flags ^= Block.Flag.Match;
            }
            output.RemoveRange(lastAddIndex + 1, output.Count - lastAddIndex - 1);
        }

        return output.Count - lastCount;
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
        return GetMaxMatchCount(type, row, col, aNumRow, aNumCol, rowDir, colDir) >= 2;//> Mathf.Max(aNumCol, aNumRow);
    }

    public int GetMaxMatchCount(Block.Type type, int row, int col, int aNumRow, int aNumCol, int rowDir, int colDir) {
        BlockConfig blockConfig = BlockConfig.instance;

        int count = 0;

        //vertical
        int minCol, maxCol;
        GetIndexRange(col, aNumCol, colDir, numCol, out minCol, out maxCol);

        for(int c = minCol; c <= maxCol; c++) {
            int rowCount = 1;// aNumRow;

            //check upwards
            for(int r = rowDir > 0 ? row + aNumRow : row + 1; r < table.Length;) {
                Block b = table[r][c];
                if(b != null && b.state == Block.State.Idle && (b.flags & Block.Flag.MatchLock) == 0 && blockConfig.CheckMatch(b.type, type)) {
                    rowCount++;
                    r += b.tileSize.row;
                }
                else
                    break;
            }

            //check downwards
            for(int r = rowDir > 0 ? row - 1 : row - aNumRow; r >= 0;) {
                Block b = table[r][c];
                if(b != null && b.state == Block.State.Idle && (b.flags & Block.Flag.MatchLock) == 0 && blockConfig.CheckMatch(b.type, type)) {
                    rowCount++;
                    r -= b.tileSize.row;
                }
                else
                    break;
            }

            if(rowCount > count)
                count = rowCount;
        }

        //horizontal
        int minRow, maxRow;
        GetIndexRange(row, aNumRow, rowDir, numRow, out minRow, out maxRow);

        for(int r = minRow; r <= maxRow; r++) {
            int colCount = 1;// aNumCol;

            //check right
            for(int c = colDir > 0 ? col + aNumCol : col + 1; c < numCol;) {
                Block b = table[r][c];
                if(b != null && b.state == Block.State.Idle && (b.flags & Block.Flag.MatchLock) == 0 && blockConfig.CheckMatch(b.type, type)) {
                    colCount++;
                    c += b.tileSize.col;
                }
                else
                    break;
            }

            //check left
            for(int c = rowDir > 0 ? col - 1 : col - aNumCol; c >= 0;) {
                Block b = table[r][c];
                if(b != null && b.state == Block.State.Idle && (b.flags & Block.Flag.MatchLock) == 0 && blockConfig.CheckMatch(b.type, type)) {
                    colCount++;
                    c -= b.tileSize.col;
                }
                else
                    break;
            }

            if(colCount > count)
                count = colCount;
        }

        return count;
    }

    void OnDestroy() {
        actCallback = null;
        evalCallback = null;
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
