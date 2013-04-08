using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardEvaluator : MonoBehaviour {
    public int processCount { get { return mProcess != null ? mProcess.Count : 0; } }

    private Board mBoard;
    private Cursor mCursor;

    private HashSet<Block> mProcess;
    private bool mEvaluating;

    private List<Block> mMatches;

    private int mChainCounter;
    private bool mChainCountActive;

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
            bool isRotating = mCursor != null ? mCursor.state == Cursor.State.Rotate : false;

            //make sure nothing is falling, this is to allow the falling cascade to all land
            if(Block.fallCounter == 0 && !isRotating) {
                //go through the list
                foreach(Block b in mProcess) {
                    //make sure it is still in idle
                    if(b.state == Block.State.Idle) {
                        if((b.flags & Block.Flag.Match) == 0) {
                            mBoard.GetMatches(b, mMatches);
                        }
                    }
                }

                if(mMatches.Count > 0) {
                    //chain stuff accordingly, send to board to process matched blocks
                    Board.MatchData matchDat;

                    matchDat.chain = 0;

                    mBoard.ProcessMatchedBlocks(mMatches, matchDat);

                    mMatches.Clear();
                }

                mProcess.Clear();
                
                mEvaluating = false;

                //increment chain

            }

            yield return new WaitForFixedUpdate();
        }

        yield break;
    }
}
