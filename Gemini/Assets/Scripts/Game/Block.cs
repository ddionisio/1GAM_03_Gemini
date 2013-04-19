using UnityEngine;
using System.Collections;

public class Block : MonoBehaviour {
    public enum Type {
        Fire,
        Earth,
        Metal,
        Water,
        Wood,

        FirePower,
        EarthPower,
        MetalPower,
        WaterPower,
        WoodPower,

        NumTypes
    }

    public enum State {
        Wait,
        Activate, //board is ready, show yourself
        Idle,
        Fall,
        Rotate,

        Flash,

        DestroyWait,
        Destroy,

        NumStates
    }

    public enum SpriteState {
        Idle,
        IdleAlt,
        Fall,
        Land,
        Destroy,
        Danger,

        NumStates
    }

    [System.Flags]
    public enum Flag {
        Chain = 0x1, //potential chain when matched
        Match = 0x2, //tagged during block matching
        RotateLock = 0x4, //can't rotate this block, sorry!
        MatchLock = 0x8, //can't match anything, useless!
        FallLock = 0x10, //can't fall, stuck!
        FixedIcon = 0x20 //fixed icon orientation
    }

    public Type startType = Type.NumTypes;

    public tk2dAnimatedSprite icon;
    public tk2dSlicedSprite panel;
    public tk2dSlicedSprite panelFlash; //for flashing

    public M8.TilePos tilePos = M8.TilePos.zero;
    public M8.TilePos tileSize = M8.TilePos.one;
    public M8.TilePos tileDir = M8.TilePos.one; //from bottom left used for traversing board based on tile size

    private BlockExplode mExploder;

    private Type mType = Type.NumTypes;

    private State mState = State.NumStates;

    private Board mOwner;
    private int[] mIconClipIds;

    private float mFallOfsY; //increment by fall speed, when it is >= board.tileSize.y, then update this block's table reference, continue
    private float mFallCheckDelay; //delay before checking for fall

    private Flag mFlags = (Flag)0;

    private Type mMatch = Type.NumTypes;

    private int mExplodeLevel = 0;

    public static Type GetMatchType(Type type) {
        switch(type) {
            case Type.Fire:
                return Type.FirePower;
            case Type.Earth:
                return Type.EarthPower;
            case Type.Metal:
                return Type.MetalPower;
            case Type.Water:
                return Type.WaterPower;
            case Type.Wood:
                return Type.WoodPower;

            case Type.FirePower:
            case Type.EarthPower:
            case Type.MetalPower:
            case Type.WaterPower:
            case Type.WoodPower:
                return type;
        }

        return Type.NumTypes;
    }
        
    public Board owner { get { return mOwner; } }

    public Flag flags {
        get { return mFlags; }
        set { mFlags = value; }
    }

    public bool isPower {
        get {
            switch(mType) {
                case Type.FirePower:
                case Type.EarthPower:
                case Type.MetalPower:
                case Type.WaterPower:
                case Type.WoodPower:
                    return true;
            }

            return false;
        }
    }

    public Type type {
        get { return mType; }

        set {
            if(mType != value) {
                mType = value;

                if(mType != Type.NumTypes) {
                    EndCurrentState();

                    int typeInd = (int)type;

                    BlockConfig.BlockInfo info = BlockConfig.instance.blockTypes[typeInd];

                    icon.gameObject.SetActive(info.hasIcon);
                    if(info.hasIcon)
                        icon.anim = info.icon;

                    panel.gameObject.SetActive(info.hasPanel);
                    if(info.hasPanel)
                        panel.SetSprite(info.panelSpriteCollection, info.panelSpriteId);

                    mIconClipIds = BlockConfig.instance.blockData[typeInd].spriteClipIds;

                    //set match
                    mMatch = GetMatchType(mType);

                    StartCurrentState();
                }
            }
        }
    }

    public State state {
        get { return mState; }

        set {
            if(mState != value) {
                //deinit previous
                EndCurrentState();

                mState = value;

                //init stuff
                StartCurrentState();
            }
        }
    }

    public float fallCheckDelay {
        get { return mFallCheckDelay; }
        set { mFallCheckDelay = value; }
    }

    public bool canFall {
        get {
            if((mFlags & Flag.FallLock) == 0) {
                //get the row below the block depending on size
                int rowDown;

                if(tileSize.row > 1 && tileDir.row < 0) {
                    rowDown = tilePos.row - tileSize.row;
                }
                else {
                    rowDown = tilePos.row - 1;
                }

                if(rowDown >= 0) {
                    //for long blocks, check each one in the column
                    if(tileSize.col > 1) {
                        if(tileDir.col > 0) {
                            for(int c = 0; c < tileSize.col; c++) {
                                Block blockDown = mOwner.table[rowDown][tilePos.col + c];

                                if(blockDown != null && blockDown.state != State.Fall) {
                                    return false;
                                }
                            }
                        }
                        else {
                            for(int c = 0; c < tileSize.col; c++) {
                                Block blockDown = mOwner.table[rowDown][tilePos.col - c];

                                if(blockDown != null) {// && blockDown.state != State.Fall) {
                                    return false;
                                }
                            }
                        }

                        return true;
                    }
                    else {
                        Block blockDown = mOwner.table[rowDown][tilePos.col];

                        return blockDown == null;// || blockDown.state == State.Fall;
                    }
                }
            }

            return false;
        }
    }

    public bool canRotate {
        get {
            return (mFlags & Flag.RotateLock) == 0 ? state == State.Idle : false;
        }
    }

    public bool canMatch {
        get {
            return state == State.Idle && (flags & Flag.MatchLock) == 0;
        }
    }

    public int minRow {
        get {
            return tileSize.row > 1 && tileDir.row < 0 ? tilePos.row - (tileSize.row - 1) : tilePos.row;
        }
    }

    public int minCol {
        get {
            return tileSize.col > 1 && tileDir.col < 0 ? tilePos.col - (tileSize.col - 1) : tilePos.col;
        }
    }

    public int maxRow {
        get {
            return tileSize.row > 1 && tileDir.row > 0 ? tilePos.row + tileSize.row - 1 : tilePos.row;
        }
    }

    public int maxCol {
        get {
            return tileSize.col > 1 && tileDir.col > 0 ? tilePos.col + tileSize.col - 1 : tilePos.col;
        }
    }

    public int explodeLevel {
        get { return mExplodeLevel; }
        set { mExplodeLevel = value; }
    }

    public BlockExplode exploder { get { return mExploder; } }

    public bool CheckMatch(Block other) {
        if(!canMatch || !other.canMatch)
            return false;

        return mMatch == other.mMatch;
    }

    //If 'other' is a power type, compare with this block's 'power'
    public bool CheckMatch(Type other) {
        //TODO: special blocks matching, like all-color match or something
        //default: same type
        if(!canMatch)
            return false;

        return mMatch == GetMatchType(other);
    }

    //assumes given pos is bottom-left, ie. dir = [1, 1]
    public bool IsContainedIn(M8.TilePos pos, M8.TilePos size) {
        bool rowContained = false;
        bool colContained = false;

        //determine column (horizontal)
        if(tileSize.col == 1) {
            colContained = tilePos.col >= pos.col && tilePos.col < pos.col + size.col;
        }
        else {
            int minCol, maxCol;
            if(tileDir.col < 0) {
                minCol = tilePos.col - (tileSize.col - 1);
                maxCol = tilePos.col;
            }
            else {
                minCol = tilePos.col;
                maxCol = tilePos.col + (tileSize.col - 1);
            }

            colContained = minCol >= pos.col && maxCol < pos.col + size.col;
        }

        //determine row (vertical)
        if(tileSize.row == 1) {
            rowContained = tilePos.row >= pos.row && tilePos.row < pos.row + size.row;
        }
        else {
            int minRow, maxRow;
            if(tileDir.row < 0) {
                minRow = tilePos.row - (tileSize.row - 1);
                maxRow = tilePos.row;
            }
            else {
                minRow = tilePos.row;
                maxRow = tilePos.row + (tileSize.row - 1);
            }

            rowContained = minRow >= pos.row && maxRow < pos.row + size.row;
        }

        return rowContained && colContained;
    }

    //called by board after spawning a block
    public void Init(Board owner, Type type, int row, int col, int numRow, int numCol, State aState) {
        //just in case
        if(mOwner != null && mOwner != owner) {
            RemoveTableReference();
            mOwner.actCallback -= OnBoardAction;
        }

        mOwner = owner;
        mOwner.actCallback += OnBoardAction;

        Vector2 boardTileSize = mOwner.tileSize;

        //set size
        Vector2 panelSize = boardTileSize;
        panelSize.x *= numCol;
        panelSize.y *= numRow;

        panel.dimensions = panelSize;
        panelFlash.dimensions = panelSize;

        //set position, assume tileDir = 1, 1
        transform.localPosition = new Vector3(col * boardTileSize.x, row * boardTileSize.y, 0.0f);

        RefreshTile(false);

        this.type = type;

        state = aState;
    }

    //set the internal data properly, this is called for pre-added blocks on the board
    public void Init(Board owner, State aState) {
        //just in case
        if(mOwner != null && mOwner != owner) {
            RemoveTableReference();
            mOwner.actCallback -= OnBoardAction;
        }

        mOwner = owner;
        mOwner.actCallback += OnBoardAction;

        RefreshTile(true);

        type = startType;

        state = aState;
    }

    /// <summary>
    /// Set the block within the board table reference with given row and col
    /// </summary>
    public void SetTilePosition(int row, int col) {
        RemoveTableReference();

        //set new indices
        tilePos.row = row;
        tilePos.col = col;

        //refresh pixel position
        SnapToGrid();

        //put back on the table
        SetTableReference();
    }

    /// <summary>
    /// Called before rotating to set the tile reference based on dir.
    /// The references are set before animation happens
    /// </summary>
    public void SetRotateTile(Cursor.RotateDir rotDir, Vector2 cursorPos) {
        //remove current references in the board table
        RemoveTableReference();

        float sign = 0;

        switch(rotDir) {
            case Cursor.RotateDir.Clockwise:
                tileDir = new M8.TilePos(-tileDir.col, tileDir.row);
                sign = 1;
                break;

            case Cursor.RotateDir.CounterClockwise:
                tileDir = new M8.TilePos(tileDir.col, -tileDir.row);
                sign = -1;
                break;
        }

        Vector2 boardTileSize = mOwner.tileSize;

        Vector2 pos = transform.localPosition;
        pos -= cursorPos;

        pos.Set(pos.y*sign, -pos.x*sign);

        pos += cursorPos;

        pos.x += boardTileSize.x * 0.5f * tileDir.col;
        pos.y += boardTileSize.y * 0.5f * tileDir.row;

        tilePos = new M8.TilePos(pos, boardTileSize);

        tileSize = new M8.TilePos(tileSize.col, tileSize.row);
        
        //set new references in the board table
        SetTableReference();
    }

    /// <summary>
    /// Call this during init, tile pos is based on local position in pixel space
    /// </summary>
    public void RefreshTile(bool snapToGrid) {
        Vector2 boardTileSize = mOwner.tileSize;

        //remove current references in the board table
        RemoveTableReference();

        //set tile direction based on rotation
        float rot = transform.localEulerAngles.z * Mathf.Deg2Rad;
        Vector2 dir = M8.MathUtil.Rotate(new Vector2((float)tileDir.col, (float)tileDir.row), -rot);
        tileDir.col = Mathf.RoundToInt(dir.x);
        tileDir.row = Mathf.RoundToInt(dir.y);

        //determine tile dimension based on rot
        Vector2 size = M8.MathUtil.Rotate(panel.dimensions, rot);
        size.x = Mathf.Abs(size.x) + boardTileSize.x * 0.5f;
        size.y = Mathf.Abs(size.y) + boardTileSize.y * 0.5f;

        tileSize = new M8.TilePos(size, boardTileSize);

        //set position of icon to center
        Vector3 iconPos = icon.transform.localPosition;
        iconPos.x = panel.dimensions.x * 0.5f;
        iconPos.y = panel.dimensions.y * 0.5f;
        icon.transform.localPosition = iconPos;

        //set tile pos (row, col) based on pixel position
        //origin at center
        Vector2 pos = transform.localPosition;
        pos.x += boardTileSize.x * 0.5f * dir.x;
        pos.y += boardTileSize.y * 0.5f * dir.y;

        tilePos = new M8.TilePos(pos, boardTileSize);

        if(snapToGrid) {
            SnapToGrid();
        }

        //set new references in the board table
        SetTableReference();
    }

    /// <summary>
    /// Refresh pixel position based on tile (row, col) position
    /// </summary>
    public void SnapToGrid() {
        Vector2 pos = tilePos.ToVector2(mOwner.tileSize);

        if(tileDir.row < 0)
            pos.y += mOwner.tileSize.y;

        if(tileDir.col < 0)
            pos.x += mOwner.tileSize.x;

        transform.localPosition = pos;
    }

    public void Release() {
        if(mOwner != null) {
            mOwner.pool.Release(transform);
        }
        else {
            Destroy(gameObject);
        }
    }

    public void RemoveTableReference() {
        if(mOwner != null) {
            for(int r = 0, curR = tilePos.row; r < tileSize.row; r++, curR = tileDir.row < 0 ? tilePos.row - r : tilePos.row + r) {
                if(curR >= 0 && curR < mOwner.table.Length) {
                    for(int c = 0, curC = tilePos.col; c < tileSize.col; c++, curC = tileDir.col < 0 ? tilePos.col - c : tilePos.col + c) {
                        if(curC >= 0 && curC < mOwner.numCol && mOwner.table[curR][curC] == this)
                            mOwner.table[curR][curC] = null;
                    }
                }
            }
        }
        else {
            Debug.LogWarning("owner is missing!");
        }
    }

    public void SetTableReference() {
        if(mOwner != null) {
            for(int r = 0, curR = tilePos.row; r < tileSize.row; r++, curR = tileDir.row < 0 ? tilePos.row - r : tilePos.row + r) {
                if(curR >= 0 && curR < mOwner.table.Length) {
                    for(int c = 0, curC = tilePos.col; c < tileSize.col; c++, curC = tileDir.col < 0 ? tilePos.col - c : tilePos.col + c) {
                        if(curC >= 0 && curC < mOwner.numCol)
                            mOwner.table[curR][curC] = this;
                    }
                }
            }
        }
        else {
            Debug.LogWarning("owner is missing!");
        }
    }

    void OnDestroy() {
        //must not be from pool
        OnDespawned();
    }

    void OnSpawned() {
        //reset some data
        mType = Type.NumTypes;
        mState = State.NumStates;
        mIconClipIds = null;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        tilePos = M8.TilePos.zero;
        tileDir = M8.TilePos.one;
        mFlags = (Flag)0;
    }

    void OnDespawned() {
        state = State.NumStates;

        if(mOwner != null) {
            RemoveTableReference();
            mOwner.actCallback -= OnBoardAction;
            mOwner = null;
        }

        mFlags = (Flag)0;
    }

    void Awake() {
        mExploder = GetComponentInChildren<BlockExplode>();

        panelFlash.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update() {
        switch(mState) {
            case State.Activate:
                //TODO: wait for some sort of animation end
                state = State.Idle;
                break;

            case State.Idle:
                if(mFallCheckDelay > 0.0f) {
                    mFallCheckDelay -= Time.deltaTime;
                }
                else if(canFall) {
                    state = State.Fall;
                }
                break;

            case State.Fall:
                BlockConfig config = BlockConfig.instance;

                float dY = config.fallSpeed * Time.deltaTime;

                //update visual
                Vector3 pos = transform.localPosition;
                pos.y -= dY;
                transform.localPosition = pos;

                //update reference
                mFallOfsY += dY;
                if(mFallOfsY >= mOwner.tileSize.y) {
                    //check if we can land
                    if(canFall) {
                        //update table ref and row
                        RemoveTableReference();

                        tilePos.row--;

                        SetTableReference();

                        mFallOfsY = mFallOfsY - mOwner.tileSize.y;
                    }
                    else {
                        SnapToGrid();
                        
                        state = State.Idle;

                        //TODO: landing etc.

                        mFlags |= Flag.Chain;

                        mOwner.Eval(this);
                    }
                }
                break;
        }
    }

    void OnBoardAction(Board board, Board.Action act) {
        switch(act) {
            case Board.Action.PushRow:
                RemoveTableReference();

                //update our reference and move the visual
                int prevRow = tilePos.row;

                tilePos.row++;

                SetTableReference();

                //set pixel position
                Vector3 pos = transform.localPosition;
                pos.y += board.tileSize.y;
                transform.localPosition = pos;

                //we were previously generated from the bottom, set to idle
                if(prevRow == -1) {
                    state = State.Idle;

                    mOwner.Eval(this);
                }
                break;
        }
    }

    private void EndCurrentState() {
        if(icon != null)
            icon.Stop();

        switch(mState) {
            case State.Activate:
                break;

            case State.Idle:
                mFallCheckDelay = 0.0f;
                break;

            case State.Fall:
                if(mOwner != null)
                    mOwner._BlockSetFallCounter(mOwner.fallCounter - 1);
                break;

            case State.Flash:
                panelFlash.gameObject.SetActive(false);
                break;

            case State.DestroyWait:
                break;

            case State.Destroy:
                mExploder.Stop();
                break;
        }
    }

    private void StartCurrentState() {
        switch(mState) {
            case State.Activate:
                break;

            case State.Wait:
            case State.Idle:
                icon.Play(mIconClipIds[(int)SpriteState.Idle]);
                break;

            case State.Rotate:
                mFlags &= ~Flag.Chain;
                mFallCheckDelay = BlockConfig.instance.fallDelay;
                break;

            case State.Fall:
                int rowIndAbove = tilePos.row + 1;

                icon.Play(mIconClipIds[(int)SpriteState.Fall]);

                //update table ref and row
                RemoveTableReference();

                tilePos.row--;

                SetTableReference();

                mFallOfsY = 0.0f;

                mOwner._BlockSetFallCounter(mOwner.fallCounter + 1);

                mFlags &= ~Flag.Chain;

                //check if block above can fall
                if(rowIndAbove < mOwner.numRow) {
                    Block[] rowAbove = mOwner.table[rowIndAbove];
                    int col, maxCol;
                    Board.GetIndexRange(tilePos.col, tileSize.col, tileDir.col, mOwner.numCol, out col, out maxCol);
                    for(; col <= maxCol; col++) {
                        Block blockAbove = rowAbove[col];
                        if(blockAbove != null && (blockAbove.tileSize.col == 1 || blockAbove.canFall)) {
                            blockAbove.state = State.Fall;
                        }
                    }
                }

                break;

            case State.Flash:
                panelFlash.gameObject.SetActive(true);
                break;

            case State.DestroyWait:
                icon.Play(mIconClipIds[(int)SpriteState.Destroy]);

                mFallOfsY = 0.0f;
                break;

            case State.Destroy:
                //disable stuff
                icon.gameObject.SetActive(false);
                panel.gameObject.SetActive(false);

                //enable destroy object
                BlockConfig.BlockInfo info = BlockConfig.instance.blockTypes[(int)type];

                mExploder.Begin(explodeLevel, info.color);
                break;
        }
    }
}
