using UnityEngine;
using System.Collections;

//This needs to be a child of the board
//TODO: variable size? for now it's always 2x2
public class Cursor : MonoBehaviour {
    public enum Dir {
        Up,
        Down,
        Left,
        Right
    }

    public enum RotateDir {
        Clockwise,
        CounterClockwise
    }

    public enum State {
        None,
        Rotate,
        Move
    }

    public static readonly M8.TilePos size = new M8.TilePos() { col = 2, row = 2 };

    public Transform holder; //this is where the blocks are placed and rotated

    public float rotateDelay;
    public float moveStartDelay;
    public float moveDelay;

    private Board mBoard;

    private State mState = State.None;

    private Dir mMoveDir;
    private bool mMoveStart;

    private float mRotStart;
    private float mRotEnd;
    private Block[] mRotBlocks = new Block[size.row * size.col];
    private int mRotNumBlocks = 0;

    private M8.TilePos mTilePos;

    private float mCurTime;
        
    public Board board { get { return mBoard; } }

    public State state { get { return mState; } }

    public bool MoveValid(Dir dir) {
        switch(dir) {
            case Dir.Up:
                return mTilePos.row + size.row < mBoard.numRow;

            case Dir.Down:
                return mTilePos.row - 1 >= 0;

            case Dir.Left:
                return mTilePos.col - 1 >= 0;

            case Dir.Right:
                return mTilePos.col + size.col < mBoard.numCol;
        }

        return false;
    }

    //returns true if we are now moving
    public bool MoveTo(Dir dir) {
        //check if we can indeed move
        if(MoveValid(dir)) {
            if(mState != State.Move || dir != mMoveDir) {
                EndCurrentState();

                mMoveDir = dir;

                mState = State.Move;

                StartCurrentState();
            }

            return true;
        }

        return false;
    }

    public void Stop() {
        if(mState != State.None) {
            EndCurrentState();

            mState = State.None;

            StartCurrentState();
        }
    }

    //returns true if we are now rotating
    public bool Rotate(RotateDir dir) {
        if(mState == State.Rotate)
            return false;

        int numRotable = 0;

        for(int r = 0; r < size.row; r++) {
            int curR = mTilePos.row + r;

            for(int c = 0; c < size.col; c++) {
                int curC = mTilePos.col + c;

                Block b = mBoard.table[curR, curC];
                if(b != null) {
                    if(b.canRotate && b.IsContainedIn(mTilePos, size)) {
                        numRotable++;
                    }
                    else {
                        numRotable = 0;
                        break;
                    }
                }
            }
        }

        if(numRotable > 0) {
            //clear out previous state
            EndCurrentState();

            mState = State.Rotate;

            Vector2 cursorPos = transform.localPosition;
            cursorPos += mBoard.tileSize;
            
            //rotate tile reference for each block
            for(int r = 0; r < size.row; r++) {
                int curR = mTilePos.row + r;

                for(int c = 0; c < size.col; c++) {
                    int curC = mTilePos.col + c;

                    Block b = mBoard.table[curR, curC];
                    if(b != null && RotateAdd(b)) {
                        b.SetRotateTile(dir, cursorPos);
                        b.transform.parent = holder;
                    }
                }
            }

            //set rotation anim
            mRotStart = transform.localEulerAngles.z;

            switch(dir) {
                case RotateDir.Clockwise:
                    mRotEnd = mRotStart - 90.0f;
                    break;

                case RotateDir.CounterClockwise:
                    mRotEnd = mRotStart + 90.0f;
                    break;
            }

            StartCurrentState();

            return true;
        }
        else {
            return false;
        }
    }

    public bool RotateContains(Block b) {
        for(int i = 0; i < mRotNumBlocks; i++) {
            if(mRotBlocks[i] == b)
                return true;
        }

        return false;
    }

    void Awake() {
        mBoard = transform.parent != null ? transform.parent.GetComponent<Board>() : null;
        if(mBoard == null) {
            Debug.LogError("Board component needs to exist from the parent of cursor!");
        }

        //set to center bottom of board
        SetTilePos(0, (mBoard.numCol / 2) - (size.col / 2));

        holder.localRotation = Quaternion.identity;
    }

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        switch(mState) {
            case State.None:
                break;

            case State.Move:
                mCurTime += Time.deltaTime;

                if(mMoveStart && mCurTime >= moveStartDelay) {
                    if(MoveValid(mMoveDir)) {
                        //keep moving
                        mCurTime = 0.0f;
                        mMoveStart = false;

                        MoveCurrentDir();
                    }
                    else {
                        Stop();
                    }
                }
                else if(mCurTime >= moveDelay) {
                    if(MoveValid(mMoveDir)) {
                        //keep moving
                        mCurTime -= moveDelay;

                        MoveCurrentDir();
                    }
                    else {
                        Stop();
                    }
                }
                break;

            case State.Rotate:
                mCurTime += Time.deltaTime;
                if(mCurTime >= rotateDelay) {
                    Stop();
                }
                else {
                    Vector3 rot = holder.localEulerAngles;
                    rot.z = mRotStart + (mCurTime / rotateDelay) * (mRotEnd - mRotStart);
                    holder.localEulerAngles = rot;
                }
                break;
        }
    }

    void OnDrawGizmos() {
        mBoard = transform.parent != null ? transform.parent.GetComponent<Board>() : null;
        if(mBoard != null) {
            Gizmos.color = Color.green;

            Vector3 pos = transform.position;
            pos.x += mBoard.tileSize.x;
            pos.y += mBoard.tileSize.y;

            Gizmos.DrawWireCube(pos, new Vector3(mBoard.tileSize.x * size.col, mBoard.tileSize.y * size.row, 0.0f));
        }
    }

    private void EndCurrentState() {
        switch(mState) {
            case State.Rotate:
                //rotate fully if animation hasn't ended
                Vector3 rots = holder.localEulerAngles;
                rots.z = mRotEnd;
                holder.localEulerAngles = rots;

                //place the blocks back to the board
                for(int i = 0; i < mRotNumBlocks; i++) {
                    if(mRotBlocks[i] != null) {
                        mRotBlocks[i].transform.parent = mBoard.blockHolder;
                        mRotBlocks[i].state = Block.State.Idle;
                        mRotBlocks[i].SnapToGrid();
                        mRotBlocks[i] = null;
                    }
                }

                //clear out
                mRotNumBlocks = 0;
                break;
        }
    }

    private void StartCurrentState() {
        mCurTime = 0.0f;

        switch(mState) {
            case State.Move:
                mMoveStart = true;

                //move right away
                MoveCurrentDir();
                break;

            case State.Rotate:
                break;
        }
    }

    private void MoveCurrentDir() {
        //TODO: play bling sound

        switch(mMoveDir) {
            case Dir.Up:
                SetTilePos(mTilePos.row + 1, mTilePos.col);
                break;

            case Dir.Down:
                SetTilePos(mTilePos.row - 1, mTilePos.col);
                break;

            case Dir.Left:
                SetTilePos(mTilePos.row, mTilePos.col - 1);
                break;

            case Dir.Right:
                SetTilePos(mTilePos.row, mTilePos.col + 1);
                break;
        }
    }

    private void SetTilePos(int row, int col) {
        mTilePos.row = row;
        mTilePos.col = col;

        Vector2 pos = mTilePos.ToVector2(mBoard.tileSize);
        transform.localPosition = new Vector3(pos.x, pos.y, transform.localPosition.z);
    }

    //true = added
    private bool RotateAdd(Block b) {
        if(mRotNumBlocks < mRotBlocks.Length && !RotateContains(b)) {
            b.state = Block.State.Rotate;

            mRotBlocks[mRotNumBlocks] = b;
            mRotNumBlocks++;

            return true;
        }
        else {
            return false;
        }
    }
}
