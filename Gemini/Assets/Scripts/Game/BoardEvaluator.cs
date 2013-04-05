using UnityEngine;
using System.Collections;

public class BoardEvaluator : MonoBehaviour {
    private Board mBoard;

    void Awake() {
        mBoard = GetComponent<Board>();
    }

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

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
}
