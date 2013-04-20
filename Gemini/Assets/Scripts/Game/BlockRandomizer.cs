using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BlockRandomizer : MonoBehaviour, IComparer<BlockRandomizer.Item> {
    public const string defaultGroup = "default";

    public string group = "default";

    [System.Serializable]
    public class Item {
        public Block.Type type = Block.Type.NumTypes;
        public float weight = 1.0f;

        private float mRange = 0.0f;

        public float range {
            get { return mRange; }
            set { mRange = value; }
        }
    }

    public Item[] items;

    private static Dictionary<string, BlockRandomizer> mGroup;

    private float mItemMaxRange = 0.0f;

    private Item mPicker = new Item();

    private int mPickInd = 0;

    public static BlockRandomizer GetDefaultRandomizer() {
        BlockRandomizer ret = null;
        if(mGroup != null) {
            mGroup.TryGetValue(defaultGroup, out ret);
        }

        return ret;
    }

    public static BlockRandomizer GetRandomizer(string group) {
        BlockRandomizer ret = null;
        if(mGroup != null) {
            mGroup.TryGetValue(group, out ret);
        }

        return ret;
    }

    public Block.Type lastPicked { get { return mPicker.type; } }

    public Block.Type pick {
        get {
            mPicker.range = Random.Range(0.0f, mItemMaxRange);

            mPickInd = System.Array.BinarySearch(items, mPicker, this);

            if(mPickInd < 0) {
                mPickInd = ~mPickInd;
                if(mPickInd < items.Length)
                    mPicker.type = items[mPickInd].type;
                else
                    mPicker.type = items[items.Length - 1].type;
            }
            else {
                mPicker.type = items[mPickInd].type;
            }

            return mPicker.type;
        }
    }

    public Block.Type next {
        get {
            mPickInd++;
            if(mPickInd == items.Length)
                mPickInd = 0;

            mPicker.type = items[mPickInd].type;

            return mPicker.type;
        }
    }

    void OnDestroy() {
        if(mGroup != null) {
            mGroup.Remove(group);

            if(mGroup.Count == 0) {
                mGroup = null;
            }
        }
    }

    void Awake() {
        if(mGroup == null)
            mGroup = new Dictionary<string, BlockRandomizer>();

        if(!mGroup.ContainsKey(group)) {
            mGroup.Add(group, this);
        }
        else {
            Debug.LogWarning("Randomizer group already exists for: " + group);
        }

        //prep up randomization

        mItemMaxRange = 0.0f;

        foreach(Item itm in items) {
            mItemMaxRange += itm.weight;
            itm.range = mItemMaxRange;
        }
    }

    public int Compare(Item obj1, Item obj2) {

        if(obj1 != null && obj2 != null) {
            float v = obj1.range - obj2.range;

            if(Mathf.Abs(v) <= float.Epsilon)
                return 0;
            else if(v < 0.0f)
                return -1;
            else
                return 1;
        }
        else if(obj1 == null && obj2 != null) {
            return 1;
        }
        else if(obj2 == null && obj1 != null) {
            return -1;
        }

        return 0;
    }
}
