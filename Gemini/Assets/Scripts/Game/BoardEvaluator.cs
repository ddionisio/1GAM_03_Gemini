using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardEvaluator : MonoBehaviour {
    private Board mBoard;
    private Cursor mCursor;

    private HashSet<Block> mProcess;
    private bool mEvaluating;

    private List<Block> mMatches;

    private int mChainCounter = 0;
    private bool mChainCountActive = false;
    
    public int processCount { get { return mProcess != null ? mProcess.Count : 0; } }

    //goes through blocks upwards or downwards and get the matches
    //also adds given block to output if it hasn't been added already
    //return number of matches found
    private int GetNeightborMatchesRowRecurse(Block block, List<Block> output, bool upwards) {
        int rowCount = 0;

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
        Board.GetIndexRange(col, sizeCol, colDir, mBoard.numCol, out minCol, out maxCol);

        int r;
        if(upwards) {
            r = rowDir > 0 ? row + sizeRow : row + 1;
        }
        else {
            r = rowDir > 0 ? row - 1 : row - sizeRow;
        }

        if(r >= 0 && r < mBoard.numRow) {
            for(int c = minCol; c <= maxCol; c++) {
                Block b = mBoard.table[r][c];
                if(b != null && b.CheckMatch(type)) {
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
        Board.GetIndexRange(row, sizeRow, rowDir, mBoard.numRow, out minRow, out maxRow);

        int c;
        if(leftward) {
            c = colDir > 0 ? col + sizeCol : col + 1;
        }
        else {
            c = colDir > 0 ? col - 1 : col - sizeCol;
        }

        if(c >= 0 && c < mBoard.numCol) {
            for(int r = minRow; r <= maxRow; r++) {
                Block b = mBoard.table[r][c];
                if(b != null && b.CheckMatch(type)) {
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

    void OnDestroy() {
        if(mBoard != null) {
            mBoard.evalCallback -= EvalBlockCallback;
        }
    }

    void Awake() {
        mBoard = GetComponent<Board>();
        mBoard.evalCallback += EvalBlockCallback;

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
            int numDestroying = mBoard.destroyCounter;
            bool isRotating = mCursor != null ? mCursor.state == Cursor.State.Rotate : false;

            mChainCountActive = numFalling > 0 || numDestroying > 0 || isRotating || mEvaluating;
        }

        mChainCounter = 0;

        Debug.Log("Chain reset");
    }
}
