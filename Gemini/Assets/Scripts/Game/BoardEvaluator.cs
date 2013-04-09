using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardEvaluator : MonoBehaviour {
    public struct MatchData {
        public int chain;
    }

    public delegate void ProcessMatchCallback(List<Block> blocks, MatchData dat);

    public event ProcessMatchCallback processMatchesCallback; //called after matches are found
        
    private Board mBoard;
    private BlockDestroyer mDestroyer;
    private Cursor mCursor;

    private HashSet<Block> mProcess;
    private bool mEvaluating;

    private List<Block> mMatches;

    private int mChainCounter = 0;
    private bool mChainCountActive = false;
    
    public int processCount { get { return mProcess != null ? mProcess.Count : 0; } }

    void OnDestroy() {
        if(mBoard != null) {
            mBoard.evalCallback -= EvalBlockCallback;
        }

        processMatchesCallback = null;
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
                            mBoard.GetMatches(b, mMatches);
                        }

                        //check for chaining, add to counter and remove flag
                        if((b.flags & Block.Flag.Chain) != 0) {
                            numChainMark++;
                            b.flags &= ~Block.Flag.Chain;
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

                    MatchData matchDat;
                    matchDat.chain = mChainCounter;

                    Debug.Log("chain: " + mChainCounter);

                    if(processMatchesCallback != null)
                        processMatchesCallback(mMatches, matchDat);

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

            int numFalling = Block.fallCounter;
            int numDestroying = mDestroyer != null ? mDestroyer.numActive : 0;
            bool isRotating = mCursor != null ? mCursor.state == Cursor.State.Rotate : false;

            mChainCountActive = numFalling > 0 || numDestroying > 0 || isRotating || mEvaluating;
        }

        mChainCounter = 0;

        Debug.Log("Chain reset");
    }
}
