using UnityEngine;
using System.Collections;

//make sure this is in the same object as the board
public class RowBlockRising : MonoBehaviour {
    public enum State {
        None,
        Wait,
        Rising
    }

    public const int longBlockChanceWeight = 100;

    public float waitDelay;
    public float moveDelay;

    public int numMinLongBlocks; //2x1 blocks
    public int numMaxLongBlocks; //2x1 blocks

    public float longBlockChance; //[0, 100] percent

    private Board mBoard = null;
    private BlockDestroyer mDestroyer = null;
    
    private State mState = State.None;
    private float mCountDown;
    private float mCountDownStart;

    private Vector3 mHolderPos;
    private float mHolderStartY;
    private float mHolderEndY;
    private float mCurMoveTime;
        
    public void Begin() {
        EndCurrentState();
        mState = State.Wait;
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

        mDestroyer = GetComponent<BlockDestroyer>();
    }

    void Update() {
        bool destroyActive = mDestroyer != null ? mDestroyer.numActive > 0 : false;

        if(Block.fallCounter == 0 && !destroyActive) {
            switch(mState) {
                case State.Rising:
                    mCurMoveTime += Time.deltaTime;
                    if(mCurMoveTime >= moveDelay) {
                        Begin();
                    }
                    else {
                        float t = mCurMoveTime / moveDelay;
                        mHolderPos.y = mHolderStartY + t * (mHolderEndY - mHolderStartY);
                        mBoard.blockHolder.localPosition = mHolderPos;
                    }
                    break;

                case State.Wait:
                    mCountDown -= Time.deltaTime;
                    if(mCountDown <= 0.0f) {


                        //check if game over
                        if(mBoard.CheckBlocksRow(mBoard.numRow - 1)) {
                            mBoard.GameOver();
                        }
                        else {
                            EndCurrentState();
                            mState = State.Rising;
                            StartCurrentState();
                        }
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
            case Board.Action.Begin:
                GenerateBlocks();
                Begin();
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
            case State.Rising:
                mHolderPos.y = mHolderEndY;
                mBoard.blockHolder.localPosition = mHolderPos;
                break;
        }
    }

    void StartCurrentState() {
        switch(mState) {
            case State.Wait:
                mCountDownStart = waitDelay;
                break;

            case State.Rising:
                //push up the row
                mBoard.PushRow();

                //generate a new one
                GenerateBlocks();

                //make the board rise starting one row below
                mHolderPos = mBoard.blockHolder.localPosition;
                
                mHolderStartY = mHolderPos.y - mBoard.tileSize.y;
                mHolderEndY = mHolderPos.y;

                mHolderPos.y = mHolderStartY;

                mBoard.blockHolder.localPosition = mHolderPos;

                mCurMoveTime = 0.0f;
                break;
        }

        mCountDown = mCountDownStart;
    }

    private void GenerateBlocks() {
        int numLong = Random.Range(0.0f, 100.0f) < longBlockChance ? Random.Range(numMinLongBlocks, numMaxLongBlocks + 1) : 0;

        mBoard.GenerateRow(-1, numLong, Block.State.Wait, true);
    }
}
