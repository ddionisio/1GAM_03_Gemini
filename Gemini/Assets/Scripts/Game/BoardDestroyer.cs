using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardDestroyer : MonoBehaviour {
    private class GroupData {
        public List<Block> blocks;
    }

    private Board mBoard;
    
    private int mNumActive = 0;

    private WaitForSeconds mWaitFlashDelay;
    private WaitForSeconds mWaitDestroyDelay;
    private WaitForFixedUpdate mWaitUpdate = new WaitForFixedUpdate();

    private Queue<GroupData> mProcessBuffer;

    void OnDestroy() {
        if(mBoard != null)
            mBoard.processMatchesCallback -= OnProcessMatchBlocks;
    }

    void Awake() {
        mBoard = GetComponent<Board>();

        mBoard.processMatchesCallback += OnProcessMatchBlocks;

        int maxDestroy = mBoard.numCol * mBoard.numRow;

        //fill up buffer
        mProcessBuffer = new Queue<GroupData>(maxDestroy);
        for(int i = 0; i < maxDestroy; i++) {
            GroupData dat = new GroupData();
            dat.blocks = new List<Block>(maxDestroy);
            mProcessBuffer.Enqueue(dat);
        }
    }

    // Use this for initialization
    void Start() {
        BlockConfig blockConfig = BlockConfig.instance;
                
        mWaitFlashDelay = new WaitForSeconds(blockConfig.destroyFlashDelay);
        mWaitDestroyDelay = new WaitForSeconds(blockConfig.destroyDelay);
    }

    void OnProcessMatchBlocks(List<Block> blocks, Board.MatchData dat) {
        if(mProcessBuffer.Count > 0)
            StartCoroutine(RunBlocks(blocks, dat.chain));
        else
            Debug.LogError("Ran out of process buffer!");
    }

    IEnumerator RunBlocks(List<Block> aBlocks, int explodeLevel) {
        GroupData grp = mProcessBuffer.Dequeue();
        
        List<Block> grpBlocks = grp.blocks;

        mNumActive++;
        mBoard._DestroySetCounter(mNumActive);

        //init and set to flash
        grpBlocks.Clear();

        foreach(Block block in aBlocks) {
            block.state = Block.State.Flash;

            grpBlocks.Add(block);
        }
        
        //wait for flash to finish
        yield return mWaitFlashDelay;

        //stop the flashing
        for(int i = 1; i < grpBlocks.Count; i++) {
            grpBlocks[i].state = Block.State.DestroyWait;
        }

        //destroy them one by one
        foreach(Block b in grpBlocks) {
            b.explodeLevel = explodeLevel - 1;
            b.state = Block.State.Destroy;

            //wait a bit
            yield return mWaitDestroyDelay;
        }

        //wait until all blocks are done exploding
        while(true) {
            int numDone = 0;
            foreach(Block b in grpBlocks) {
                if(!b.exploder.isActive)
                    numDone++;
            }

            if(numDone == grpBlocks.Count)
                break;

            yield return mWaitUpdate;
        }
        
        //destroy blocks
        foreach(Block b in grpBlocks) {
            b.Release();
        }
                
        grpBlocks.Clear();
        mProcessBuffer.Enqueue(grp);

        //avoid chain reset, allow blocks to fall first
        yield return mWaitUpdate;

        mNumActive--;
        mBoard._DestroySetCounter(mNumActive);
    }
}
