using UnityEngine;
using System.Collections;

//explosion effect for the block
public class BlockExplode : MonoBehaviour {

    public delegate void OnFinish();

    public GameObject[] levels;

    public float angleStart;

    private BlockExplodeShard[][] mLevelShards;

    private WaitForFixedUpdate mWaitUpdate = new WaitForFixedUpdate();

    private int mNumActive = 0;

    public bool isActive { get { return mNumActive > 0; } }

    public void Begin(int level, Color color) {
        if(level >= levels.Length)
            level = levels.Length - 1;

        StartCoroutine(DoIt(levels[level], mLevelShards[level], color));
    }

    public void Stop() {
        if(mNumActive > 0) {
            StopAllCoroutines();

            foreach(GameObject go in levels)
                go.SetActive(false);

            mNumActive = 0;
        }
    }

    void Awake() {
        mLevelShards = new BlockExplodeShard[levels.Length][];
        for(int i = 0; i < levels.Length; i++) {
            mLevelShards[i] = levels[i].GetComponentsInChildren<BlockExplodeShard>(true);

            //set the directions
            float dAngle = Mathf.Deg2Rad*(360.0f/mLevelShards[i].Length);

            Vector2 curUp = M8.MathUtil.Rotate(Vector2.up, Mathf.Deg2Rad * angleStart);

            foreach(BlockExplodeShard shard in mLevelShards[i]) {
                shard.transform.up = curUp;
                curUp = M8.MathUtil.Rotate(curUp, dAngle);
            }

            levels[i].gameObject.SetActive(false);
        }
    }

    IEnumerator DoIt(GameObject level, BlockExplodeShard[] shards, Color color) {
        mNumActive++;

        level.SetActive(true);

        foreach(BlockExplodeShard shard in shards) {
            shard.Begin(color);
        }

        //wait until all shards are done
        while(true) {
            int numDone = 0;

            foreach(BlockExplodeShard shard in shards) {
                if(shard.done)
                    numDone++;
            }

            if(numDone == shards.Length)
                break;

            yield return mWaitUpdate;
        }

        level.SetActive(false);

        mNumActive--;
    }
}
