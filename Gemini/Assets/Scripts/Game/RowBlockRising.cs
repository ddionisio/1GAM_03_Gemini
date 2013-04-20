using UnityEngine;
using System.Collections;

//make sure this is in the same object as the board
public class RowBlockRising : MonoBehaviour {
    public enum State {
        None,
        Rising,
        Danger,
        Boost,
    }
    
    public float moveSpeed;
    public float boostSpeed;
    public float dangerDelay;

    /// <summary>
    /// set this to a randomizer group to allow spawning of special blocks
    /// If you want chances of special type not happening, add a "NumType" in the randomizer
    /// </summary>
    public string specialGroupRandomizer = "";

    /// <summary>
    /// The number of specials that can be added in the row
    /// </summary>
    public int maxSpecial = 1;
    
    private Board mBoard = null;
    
    private State mState = State.None;
    private float mCountDown;

    private float mYOfs;

    private float mCurSpeed;

    private Block.Type[] mSpecialInserts;
        
    public bool boost {
        get {
            return mState == State.Boost;
        }
        set {
            if(value) {
                if(mState == State.Rising) {
                    //change speed and state
                    mCurSpeed = boostSpeed;
                    mState = State.Boost;
                }
            }
            else {
                if(mState == State.Boost) {
                    //return to rising
                    mCurSpeed = moveSpeed;
                    mState = State.Rising;
                }
            }
        }
    }

    public void Begin() {
        EndCurrentState();
        mState = mBoard.CheckBlocksRow(mBoard.numRow - 1) ? State.Danger : State.Rising;
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

        mSpecialInserts = new Block.Type[mBoard.numCol];
    }

    void Update() {
        bool destroyActive = mBoard.destroyCounter > 0;

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
                        Begin();
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

                mYOfs = mYOfs > mBoard.tileSize.y ? mYOfs - mBoard.tileSize.y : 0.0f;

                Vector3 holderPos = mBoard.blockHolder.localPosition;
                holderPos.y = 0.0f;
                mBoard.blockHolder.localPosition = holderPos;
                break;
        }
    }

    void StartCurrentState() {
        switch(mState) {
            case State.Danger:
                mCountDown = dangerDelay;
                break;

            case State.Rising:
                RisePrep(moveSpeed);
                break;

            case State.Boost:
                RisePrep(boostSpeed);
                break;
        }
    }

    private void RisePrep(float speed) {
        //generate a new one
        GenerateBlocks();
        mYOfs = 0.0f;
        mCurSpeed = speed;
    }

    //returns true if done
    private bool RiseMove() {
        mYOfs += mCurSpeed*Time.deltaTime;
        if(mYOfs >= mBoard.tileSize.y) {
            return true;
        }
        else {
            Vector3 holderPos = mBoard.blockHolder.localPosition;
            holderPos.y = Mathf.Round(mYOfs);
            mBoard.blockHolder.localPosition = holderPos;
        }

        return false;
    }
    
    private void GenerateBlocks() {
        //set power blocks to insert
        if(!string.IsNullOrEmpty(specialGroupRandomizer)) {
            BlockRandomizer randomizer = BlockRandomizer.GetRandomizer(specialGroupRandomizer);
            for(int i = 0; i < mSpecialInserts.Length; i++) {
                if(i < maxSpecial) {
                    mSpecialInserts[i] = randomizer.pick;
                }
                else {
                    mSpecialInserts[i] = Block.Type.NumTypes;
                }
            }

            M8.ArrayUtil.Shuffle(mSpecialInserts);

            mBoard.GenerateRow(-1, Block.State.Wait, true, mSpecialInserts);
        }
        else {
            mBoard.GenerateRow(-1, Block.State.Wait, true, null);
        }
    }
}
