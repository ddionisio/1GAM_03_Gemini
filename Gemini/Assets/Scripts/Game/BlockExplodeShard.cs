using UnityEngine;
using System.Collections;

//shard starts at origin then moves up.  So make sure to set the 'up' vector
public class BlockExplodeShard : MonoBehaviour {
    public tk2dAnimatedSprite shard;
    public tk2dAnimatedSprite shardGlow; //additive version

    public float ofs;
    public float delay;

    private float mCurTime;
    private bool mDone;

    public bool done { get { return mDone; } }

    public void Begin(Color color) {
        transform.localPosition = Vector3.zero;

        shard.color = color;
        shard.Play();

        if(shardGlow != null) {
            shardGlow.color = color;
            shardGlow.Play();
        }

        mCurTime = 0.0f;

        mDone = false;
    }

    void Update() {
        if(!mDone) {
            mCurTime += Time.deltaTime;
            if(mCurTime <= delay) {
                float d = M8.Ease.Out(mCurTime, delay, 0.0f, ofs);
                SetPos(d);
            }
            else {
                SetPos(1.0f);
                mDone = true;
            }
        }
    }

    void SetPos(float amt) {
        Vector3 pos = transform.up*amt;
        pos.x = Mathf.Round(pos.x);
        pos.y = Mathf.Round(pos.y);
        transform.localPosition = pos;
    }
}
