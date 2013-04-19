using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Board : MonoBehaviour {

    public enum Action {
        Init, //called after board is initialized on start
        Activate, //when the board becomes active
        StartGame, //the game begins, usually after countdown ends, this is called
        PushRow, //request to push upwards
        GameOver, //when a condition fails based on certain rules
        ExitGame, //when we are exiting the scene, start fading shit out
    }

    public struct MatchData {
        public int chain;
    }
                
    public delegate void ActionCallback(Board board, Action act);
    public delegate void EvalBlockCallback(Block block);
    public delegate void ProcessMatchCallback(List<Block> blocks, MatchData dat);
            
    public const string BlockPoolType = "block";
        
    public string blockPickGroup = BlockRandomizer.defaultGroup;

    public int numRow;
    public int numCol;

    public Vector2 tileSize;

    public string enterAnimation;
    public string exitAnimation;

    public tk2dSlicedSprite tint;

    public event ActionCallback actCallback;
    public event EvalBlockCallback evalCallback;
    public event ProcessMatchCallback processMatchesCallback; //called after matches are found
                
    private PoolController mBlockPool;
    private Transform mBlockHolder; //this is where blocks are put in, cursor is also here

    private Cursor mCursor;

    private int mMaxBlocks;

    /// <summary>
    /// [row, col]
    /// </summary>
    private Block[][] mTable;

    private BlockRandomizer mBlockTypePicker;

    private int mFallCounter = 0;
    private int mDestroyCounter = 0;

    private Block.Type[] mRowInserts;

    public Cursor cursor { get { return mCursor; } }

    public int maxBlocks { get { return mMaxBlocks; } }

    public Transform blockHolder { get { return mBlockHolder; } }

    /// <summary>
    /// Get number of blocks falling
    /// </summary>
    public int fallCounter { get { return mFallCounter; } }

    /// <summary>
    /// Get number of destruction in process
    /// </summary>
    public int destroyCounter { get { return mDestroyCounter; } }

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

    /// <summary>
    /// Call this after board is ready, e.g. after board enter animation
    /// </summary>
    public void Activate() {
        mBlockHolder.gameObject.SetActive(true);

        //go through current active blocks on board and set state to Activate
        foreach(Transform block in mBlockHolder) {
            Block b = block.GetComponent<Block>();
            if(b != null)
                b.state = Block.State.Activate;
        }

        if(actCallback != null) {
            actCallback(this, Action.Activate);
        }
    }

    /// <summary>
    /// After activate, some other crap may be shown, normally that's the countdown, so countdown will call this when the game officially starts
    /// </summary>
    public void StartGame() {
        if(actCallback != null) {
            actCallback(this, Action.StartGame);
        }
    }

    public void ExitGame() {
        if(actCallback != null) {
            actCallback(this, Action.ExitGame);
        }

        if(animation != null && !string.IsNullOrEmpty(exitAnimation)) {
            animation.Play(exitAnimation);
        }
    }

    //block is ready for evaluation, this is after it lands, rotate, etc.
    public void Eval(Block b) {
        if(evalCallback != null)
            evalCallback(b);
    }

    public void ProcessMatches(List<Block> matches, MatchData data) {
        if(processMatchesCallback != null)
            processMatchesCallback(matches, data);
    }

    public void GameOver() {
        Debug.Log("gameover!");

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

    public void GenerateRow(int rowInd, Block.State state, bool checkPrevType, Block.Type[] insertBlocks) {
        Block.Type pickType = Block.Type.NumTypes;

        if(insertBlocks != null) {
            //shuffle the insert blocks
            for(int i = 0; i < numCol; i++) {
                mRowInserts[i] = i < insertBlocks.Length ? insertBlocks[i] : Block.Type.NumTypes;
            }

            M8.ArrayUtil.Shuffle(mRowInserts);
            //

            //go through and put stuff on the row
            for(int c = 0; c < numCol; c++) {
                if(mRowInserts[c] == Block.Type.NumTypes)
                    pickType = Pick(mBlockTypePicker, checkPrevType ? pickType : Block.Type.NumTypes, rowInd, c, 1, 1, 1, 1);
                else {
                    pickType = mRowInserts[c];
                }

                SpawnBlock(pickType, state, rowInd, c, 1, 1);
            }
        }
        else {
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
        //check the surrounding
        
        int minCol, maxCol;
        GetIndexRange(col, aNumCol, colDir, numCol, out minCol, out maxCol);
        minCol--; maxCol++;

        int minRow, maxRow;
        GetIndexRange(row, aNumRow, rowDir, numRow, out minRow, out maxRow);
        minRow--; maxRow++;

        //top/bottom
        for(int c = minCol; c <= maxCol; c++) {
            if(c >= 0 && c < numCol) {
                //top
                if(maxRow < numRow) {
                    Block b = mTable[maxRow][c];
                    if(b != null && b.CheckMatch(type))
                        return true;
                }

                //bottom
                if(minRow >= 0) {
                    Block b = mTable[minRow][c];
                    if(b != null && b.CheckMatch(type))
                        return true;
                }
            }
        }

        //left/right
        minRow++; maxRow--;
        for(int r = minRow; r <= maxRow; r++) {
            if(r >= 0 && r < numRow) {
                //right
                if(maxCol < numCol) {
                    Block b = mTable[r][maxCol];
                    if(b != null && b.CheckMatch(type))
                        return true;
                }

                //left
                if(minCol >= 0) {
                    Block b = mTable[r][minCol];
                    if(b != null && b.CheckMatch(type))
                        return true;
                }
            }
        }

        return false;
        //return GetMaxMatchCount(type, row, col, aNumRow, aNumCol, rowDir, colDir) >= 2;//> Mathf.Max(aNumCol, aNumRow);
    }

    public int GetMaxMatchCount(Block.Type type, int row, int col, int aNumRow, int aNumCol, int rowDir, int colDir) {
        int count = 0;

        //vertical
        int minCol, maxCol;
        GetIndexRange(col, aNumCol, colDir, numCol, out minCol, out maxCol);

        for(int c = minCol; c <= maxCol; c++) {
            int rowCount = 1;// aNumRow;

            //check upwards
            for(int r = rowDir > 0 ? row + aNumRow : row + 1; r < table.Length;) {
                Block b = table[r][c];
                if(b != null && b.CheckMatch(type)) {
                    rowCount++;
                    r += b.tileSize.row;
                }
                else
                    break;
            }

            //check downwards
            for(int r = rowDir > 0 ? row - 1 : row - aNumRow; r >= 0;) {
                Block b = table[r][c];
                if(b != null && b.CheckMatch(type)) {
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
                if(b != null && b.CheckMatch(type)) {
                    colCount++;
                    c += b.tileSize.col;
                }
                else
                    break;
            }

            //check left
            for(int c = rowDir > 0 ? col - 1 : col - aNumCol; c >= 0;) {
                Block b = table[r][c];
                if(b != null && b.CheckMatch(type)) {
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

    //used by block only!
    public void _BlockSetFallCounter(int val) {
        mFallCounter = val;
    }

    //used by destroyers only!
    public void _DestroySetCounter(int val) {
        mDestroyCounter = val;
    }

    void OnDestroy() {
        actCallback = null;
        evalCallback = null;
        processMatchesCallback = null;
    }
    
    void Awake() {
        mBlockPool = GetComponent<PoolController>();
                
        mMaxBlocks = numRow * numCol * 2 + numCol;

        mTable = M8.ArrayUtil.NewDoubleArray<Block>(numRow * 2, numCol);

        mRowInserts = new Block.Type[numCol];

        mCursor = GetComponentInChildren<Cursor>();
        mCursor.board = this;
    }

    // Use this for initialization
    void Start() {
        //adjust tint
        if(tint != null) {
            tint.dimensions = new Vector2(numCol*tileSize.x, tileSize.y);
        }

        mBlockTypePicker = BlockRandomizer.GetRandomizer(blockPickGroup);

        mBlockHolder = mBlockPool.GetDefaultParent(BlockPoolType);

        mBlockPool.Expand(BlockPoolType, maxBlocks);
                
        //check if there are pre-added blocks on the board
        foreach(Transform block in mBlockHolder) {
            Block b = block.GetComponent<Block>();
            if(b != null)
                b.Init(this, Block.State.Wait);
        }

        //wait until activate
        mBlockHolder.gameObject.SetActive(false);

        StartCoroutine(ActDelay(Action.Init));
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

    private IEnumerator ActDelay(Action act) {
        yield return new WaitForFixedUpdate();

        if(actCallback != null)
            actCallback(this, act);

        if(act == Action.Init) {
            if(animation != null && !string.IsNullOrEmpty(enterAnimation)) {
                animation.Play(enterAnimation);
            }
            else {
                Activate();
            }
        }
    }
}
