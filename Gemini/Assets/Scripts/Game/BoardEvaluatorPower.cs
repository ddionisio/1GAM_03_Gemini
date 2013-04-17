using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardEvaluatorPower : MonoBehaviour {
    private Board mBoard;
    private BlockDestroyer mDestroyer;
    private Cursor mCursor;

    private HashSet<Block> mProcess;
    private bool mEvaluating;

    private List<Block> mMatches;

    private int mChainCounter = 0;
    private bool mChainCountActive = false;

    public int processCount { get { return mProcess != null ? mProcess.Count : 0; } }

    public int GetMatchesRecurse(Block block, List<Block> output, ref Block powerOutput) {
        int count = block.tileSize.col*block.tileSize.row;

        int colDir = block.tileDir.col, rowDir = block.tileDir.row, col = block.tilePos.col, row = block.tilePos.row;
        int sizeCol = block.tileSize.col, sizeRow = block.tileSize.row;

        //add this block to output
        block.flags |= Block.Flag.Match;
        output.Add(block);

        if(block.isPower && powerOutput == null) {
            powerOutput = block;
        }

        int r, c;

        //vertical
        int minCol, maxCol;
        Board.GetIndexRange(col, sizeCol, colDir, mBoard.numCol, out minCol, out maxCol);

        //check upwards
        r = rowDir > 0 ? row + sizeRow : row + 1;
        if(r < mBoard.numRow) {
            for(c = minCol; c <= maxCol; c++) {
                Block b = mBoard.table[r][c];
                if(b != null && block.CheckMatch(b) && (b.flags & Block.Flag.Match) == 0) {
                    count += GetMatchesRecurse(b, output, ref powerOutput);
                }
            }
        }

        //check downwards
        r = rowDir > 0 ? row - 1 : row - sizeRow;
        if(r >= 0) {
            for(c = minCol; c <= maxCol; c++) {
                Block b = mBoard.table[r][c];
                if(b != null && block.CheckMatch(b) && (b.flags & Block.Flag.Match) == 0) {
                    count += GetMatchesRecurse(b, output, ref powerOutput);
                }
            }
        }

        //horizontal
        int minRow, maxRow;
        Board.GetIndexRange(row, sizeRow, rowDir, mBoard.numRow, out minRow, out maxRow);

        //check right
        c = colDir > 0 ? col + sizeCol : col + 1;
        if(c < mBoard.numCol) {
            for(r = minRow; r <= maxRow; r++) {
                Block b = mBoard.table[r][c];
                if(b != null && block.CheckMatch(b) && (b.flags & Block.Flag.Match) == 0) {
                    count += GetMatchesRecurse(b, output, ref powerOutput);
                }
            }
        }

        //check left
        c = colDir > 0 ? col - 1 : col - sizeCol;
        if(c >= 0) {
            for(r = minRow; r <= maxRow; r++) {
                Block b = mBoard.table[r][c];
                if(b != null && block.CheckMatch(b) && (b.flags & Block.Flag.Match) == 0) {
                    count += GetMatchesRecurse(b, output, ref powerOutput);
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Add blocks to output if there are any matches >= 3 based on given block,
    /// the given block will also be added if we did get matches.
    /// Returns the number of blocks added to output
    /// </summary>
    public int GetMatches(Block block, List<Block> output) {
        int lastAddIndex = output.Count - 1;

        Block powerBlock = null;
        int count = 0;

        if((block.flags & Block.Flag.Match) == 0) {
            count = GetMatchesRecurse(block, output, ref powerBlock);

            if(powerBlock == null || count < 4) {
                //clear out the added crap
                for(int i = lastAddIndex + 1; i < output.Count; i++) {
                    output[i].flags ^= Block.Flag.Match;
                }
                output.RemoveRange(lastAddIndex + 1, output.Count - lastAddIndex - 1);

                count = 0;
            }
        }
       
        return count;
    }

    void OnDestroy() {
        if(mBoard != null) {
            mBoard.evalCallback -= EvalBlockCallback;
        }
    }

    void Awake() {
        mBoard = GetComponent<Board>();
        mBoard.evalCallback += EvalBlockCallback;

        mDestroyer = GetComponent<BlockDestroyer>();

        mCursor = GetComponentInChildren<Cursor>();
    }

    // Use this for initialization
    void Start() {

        mMatches = new List<Block>(mBoard.maxBlocks);

        mProcess = new HashSet<Block>(mMatches);
    }

    void EvalBlockCallback(Block block) {
        mProcess.Add(block);

        if(!mEvaluating) {
            mEvaluating = true;

            StartCoroutine(Eval());
        }
    }

    IEnumerator Eval() {
        while(mEvaluating) {
            yield return new WaitForFixedUpdate();

            //int numFalling = Block.fallCounter;
            //int numDestroying = mDestroyer != null ? mDestroyer.numActive : 0;

            bool isRotating = mCursor != null ? mCursor.state == Cursor.State.Rotate : false;

            //make sure nothing is falling, this is to allow the falling cascade to all land
            if(/*Block.fallCounter == 0 &&*/ !isRotating) {
                int numChainMark = 0;

                //go through the list
                foreach(Block b in mProcess) {
                    //make sure it is still in idle
                    if(b.state == Block.State.Idle) {
                        if((b.flags & Block.Flag.Match) == 0) {
                            GetMatches(b, mMatches);
                        }

                        //check for chaining, add to counter and remove flag
                        if((b.flags & Block.Flag.Chain) != 0) {
                            numChainMark++;
                            b.flags ^= Block.Flag.Chain;
                        }
                    }
                }

                if(mMatches.Count > 0) {

                    //chain stuff accordingly, let everyone know what to do with the matched blocks
                    if(numChainMark > 0) {
                        mChainCounter++;
                    }
                    else {
                        mChainCounter = 1;
                    }

                    Board.MatchData matchDat;
                    matchDat.chain = mChainCounter;

                    Debug.Log("chain: " + mChainCounter);

                    mBoard.ProcessMatches(mMatches, matchDat);

                    mMatches.Clear();

                    //chain reset once nothing is falling or destroying
                    if(!mChainCountActive) {
                        mChainCountActive = true;
                        StartCoroutine(ChainCheckActive());
                    }
                }

                mProcess.Clear();


                mEvaluating = false;
            }
        }
    }

    IEnumerator ChainCheckActive() {
        while(mChainCountActive) {
            yield return new WaitForFixedUpdate();

            int numFalling = mBoard.fallCounter;
            int numDestroying = mDestroyer != null ? mDestroyer.numActive : 0;
            bool isRotating = mCursor != null ? mCursor.state == Cursor.State.Rotate : false;

            mChainCountActive = numFalling > 0 || numDestroying > 0 || isRotating || mEvaluating;
        }

        mChainCounter = 0;

        Debug.Log("Chain reset");
    }
}