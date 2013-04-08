using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BlockDestroyer : MonoBehaviour {

    private class GroupData {
        public List<Block> blocks;

        public void Init(List<Block> matches) {
            blocks.Clear();
            foreach(Block b in matches) {
                b.state = Block.State.DestroyFlash;
                blocks.Add(b);
            }
        }
    }

    private Board mBoard;

    private Queue<GroupData> mProcessBuffer;

    void OnDestroy() {
        if(mBoard != null)
            mBoard.processMatchesCallback -= OnProcessMatchBlocks;
    }

    void Awake() {
        mBoard = GetComponent<Board>();
        mBoard.processMatchesCallback += OnProcessMatchBlocks;
    }

    // Use this for initialization
    void Start() {
        int maxDestroy = mBoard.numCol * mBoard.numRow;

        //fill up buffer
        mProcessBuffer = new Queue<GroupData>(maxDestroy);
        for(int i = 0; i < maxDestroy; i++) {
            GroupData dat = new GroupData();
            dat.blocks = new List<Block>(maxDestroy);
            mProcessBuffer.Enqueue(dat);
        }
    }

    void OnProcessMatchBlocks(List<Block> blocks, Board.MatchData dat) {
        if(mProcessBuffer.Count > 0) {
            GroupData group = mProcessBuffer.Dequeue();
            group.Init(blocks);
            StartCoroutine(RunBlocks(group));
        }
        else {
            Debug.LogError("Ran out of group buffer!");
        }
    }

    IEnumerator RunBlocks(GroupData group) {
        BlockConfig blockConfig = BlockConfig.instance;

        List<Block> blocks = group.blocks;

        //wait for flash to finish
        yield return new WaitForSeconds(blockConfig.destroyFlashDelay);

        //stop the flashing
        for(int i = 1; i < group.blocks.Count; i++) {
            blocks[i].state = Block.State.DestroyWait;
        }

        //destroy them one by one
        for(int i = 0; i < blocks.Count; i++) {
            Block b = blocks[i];

            b.state = Block.State.Destroy;

            //wait until current block is done
            while(b.state == Block.State.Destroy) {
                yield return new WaitForFixedUpdate();
            }

            b.state = Block.State.Destroyed;
        }

        //kill off blocks
        for(int i = 0; i < blocks.Count; i++) {
            blocks[i].Release();
        }

        blocks.Clear();

        mProcessBuffer.Enqueue(group);
    }
}
