using UnityEngine;
using System.Collections;

public class Countdown : MonoBehaviour {
    public GameObject[] countDowns;
    public float delay = 1.0f;

    private Board mBoard;
    private WaitForSeconds mWait;

    void Awake() {
        mBoard = M8.Util.GetComponentUpwards<Board>(transform, false);
        mWait = new WaitForSeconds(delay);

        foreach(GameObject go in countDowns) {
            go.SetActive(false);
        }

        mBoard.actCallback += OnBoardAction;
    }

    // Use this for initialization
    void Start() {

    }

    void OnBoardAction(Board board, Board.Action act) {
        switch(act) {
            case Board.Action.Activate:
                //start counting
                StartCoroutine(DoCount());
                break;
        }
    }

    IEnumerator DoCount() {
        foreach(GameObject go in countDowns) {
            go.SetActive(true);

            yield return mWait;

            go.SetActive(false);
        }

        mBoard.StartGame();
    }
}
