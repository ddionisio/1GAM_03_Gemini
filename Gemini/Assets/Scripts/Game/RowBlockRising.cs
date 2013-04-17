using UnityEngine;
using System.Collections;

//make sure this is in the same object as the board
public class RowBlockRising : MonoBehaviour {
    public enum State {
        None,
        Wait,
        Rising,
        Danger,
        Boost,
    }

    public Block.Type[] powerBlocks;

    public int numPowerPerRow = 2;

    public float waitDelay;
    public float moveDelay;
    public float dangerDelay;
    public float boostDelay;
    
    private Board mBoard = null;
    private BlockDestroyer mDestroyer = null;
    
    private State mState = State.None;
    private float mCountDown;

    private Vector3 mHolderPos;
    private float mHolderStartY;
    private float mHolderEndY;
    private float mCurMoveTime;

    private float mMoveDelay;

    private Block.Type[] mPowerBlockInserts;
    private int mCurPowerBlockInd = 0;

    public bool boost {
        get {
            return mState == State.Boost;
        }
        set {
            if(value) {
                switch(mState) {
                    case State.Wait:
                        EndCurrentState();
                        mState = State.Boost;
                        StartCurrentState();
                        break;

                    case State.Rising:
                        //change delay and state
                        mMoveDelay = boostDelay;
                        mState = State.Boost;
                        break;
                }
            }
            else {
                if(mState == State.Boost) {
                    //return to rising
                    mMoveDelay = moveDelay;
                    mState = State.Rising;
                }
            }
        }
    }

    public void Begin() {
        EndCurrentState();
        mState = mBoard.CheckBlocksRow(mBoard.numRow - 1) ? State.Danger : State.Wait;
        StartCurrentState();
    }

    void OnDestroy() {
        if(mBoard != null) {
            mBoard.actCallback -= OnBoardAction;
        }
    }

    void Awake() {
        mBoard = GetComponent<Board>();
        mBoard.actCallback += OnBoardAction;

        mPowerBlockInserts = new Block.Type[numPowerPerRow];

        mDestroyer = GetComponent<BlockDestroyer>();
    }

    void Update() {
        bool destroyActive = mDestroyer != null ? mDestroyer.numActive > 0 : false;

        if(mBoard.fallCounter == 0 && !destroyActive) {
            switch(mState) {
                case State.Rising:
                    if(RiseMove()) {
                        Begin();
                    }
                    break;

                case State.Boost:
                    if(RiseMove()) {
                        EndCurrentState();

                        if(mBoard.CheckBlocksRow(mBoard.numRow - 1)) {
                            //gameover, user should have released boost!
                            mState = State.None;
                            mBoard.GameOver();
                        }
                        else {
                            StartCurrentState();
                        }
                    }
                    break;

                case State.Wait:
                    mCountDown -= Time.deltaTime;
                    if(mCountDown <= 0.0f) {
                        //check if game over
                        EndCurrentState();
                        mState = mBoard.CheckBlocksRow(mBoard.numRow - 1) ? State.Danger : State.Rising;
                        StartCurrentState();
                    }
                    break;

                case State.Danger:
                    //if no longer danger, start in wait again
                    if(mBoard.CheckBlocksRow(mBoard.numRow - 1)) {
                        mCountDown -= Time.deltaTime;
                        if(mCountDown <= 0.0f) {
                            mState = State.None;
                            mBoard.GameOver();
                        }
                    }
                    else {
                        EndCurrentState();
                        mState = State.Wait;
                        StartCurrentState();
                    }
                    break;
            }
        }
    }

    void OnDrawGizmos() {
        Board board = mBoard == null ? GetComponent<Board>() : mBoard;
        if(board != null) {
            Gizmos.color = Color.cyan * 0.5f;

            if(board.tileSize != Vector2.zero) {
                Vector3 pos = transform.position;
                pos.y -= board.tileSize.y * 0.5f;

                for(int c = 0; c < board.numCol; c++) {
                    Vector3 curPos = pos + new Vector3(board.tileSize.x * 0.5f + c * board.tileSize.x, 0.0f, 0.0f);
                    Gizmos.DrawWireCube(curPos, new Vector3(board.tileSize.x, board.tileSize.y, 1.0f));
                }
            }
        }
    }

    void OnBoardAction(Board board, Board.Action act) {
        switch(act) {
            case Board.Action.Init:
                break;

            case Board.Action.Activate:
                mState = State.None;
                break;

            case Board.Action.StartGame:
                Begin(); //start moving
                break;

            case Board.Action.GameOver:
                EndCurrentState();
                mState = State.None;
                StartCurrentState();
                break;
        }
    }

    void EndCurrentState() {
        switch(mState) {
            case State.Boost:
            case State.Rising:
                //push up the row
                mBoard.PushRow();

                mHolderPos.y = mHolderStartY;
                mBoard.blockHolder.localPosition = mHolderPos;
                break;
        }
    }

    void StartCurrentState() {
        switch(mState) {
            case State.Wait:
                mCountDown = waitDelay;
                break;

            case State.Danger:
                mCountDown = dangerDelay;
                break;

            case State.Rising:
                RisePrep(moveDelay);
                break;

            case State.Boost:
                RisePrep(boostDelay);
                break;
        }
    }

    private void RisePrep(float delay) {
        //generate a new one
        GenerateBlocks();

        //make the board rise starting one row below
        mHolderPos = mBoard.blockHolder.localPosition;

        mHolderStartY = mHolderPos.y;
        mHolderEndY = mHolderPos.y + mBoard.tileSize.y;

        mHolderPos.y = mHolderStartY;

        mBoard.blockHolder.localPosition = mHolderPos;

        mCurMoveTime = 0.0f;

        mMoveDelay = delay;
    }

    //returns true if done
    private bool RiseMove() {
        mCurMoveTime += Time.deltaTime;
        if(mCurMoveTime >= mMoveDelay) {
            return true;
        }
        else {
            float t = mCurMoveTime / mMoveDelay;
            mHolderPos.y = Mathf.Round(mHolderStartY + t * (mHolderEndY - mHolderStartY));
            mBoard.blockHolder.localPosition = mHolderPos;
        }

        return false;
    }

    private Block.Type NextPowerBlock() {
        Block.Type ret = powerBlocks[mCurPowerBlockInd];
        mCurPowerBlockInd++;
        if(mCurPowerBlockInd == powerBlocks.Length)
            mCurPowerBlockInd = 0;
        return ret;
    }

    private void GenerateBlocks() {
        //set power blocks to insert
        for(int i = 0; i < numPowerPerRow; i++) {
            mPowerBlockInserts[i] = NextPowerBlock();
        }

        mBoard.GenerateRow(-1, Block.State.Wait, true, mPowerBlockInserts);
    }
}
