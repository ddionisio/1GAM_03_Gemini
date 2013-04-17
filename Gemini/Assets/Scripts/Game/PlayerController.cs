using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {
    public int player;

    public Board board;

    private RowBlockRising mBlockRiser = null;

    private bool mInputEnabled = false;

    void OnDestroy() {
        InputEnable(false);
    }

    void Awake() {
        mBlockRiser = board.GetComponent<RowBlockRising>();
    }

    // Use this for initialization
    void Start() {
        //bind inputs
        InputEnable(true);
    }

    void Update() {
        Cursor cursor = board.cursor;

        if(mInputEnabled && (cursor.state == Cursor.State.Move || cursor.state == Cursor.State.None)) {
            InputManager input = Main.instance.input;

            float x = input.GetAxis(player, InputAction.Horizontal);
            float y = input.GetAxis(player, InputAction.Vertical);

            if(x != 0.0f || y != 0.0f) {
                if(y == 0.0f || Mathf.Abs(x) > Mathf.Abs(y)) {
                    if(x < 0.0f)
                        cursor.MoveTo(Cursor.Dir.Left);
                    else
                        cursor.MoveTo(Cursor.Dir.Right);
                }
                else {
                    if(y < 0.0f)
                        cursor.MoveTo(Cursor.Dir.Down);
                    else
                        cursor.MoveTo(Cursor.Dir.Up);
                }
            }
            else {
                cursor.Stop();
            }
        }
    }

    void OnRotateLeft(InputManager.Info data) {
        if(data.state == InputManager.State.Pressed) {
            board.cursor.Rotate(Cursor.RotateDir.CounterClockwise);
        }
    }

    void OnRotateRight(InputManager.Info data) {
        if(data.state == InputManager.State.Pressed) {
            board.cursor.Rotate(Cursor.RotateDir.Clockwise);
        }
    }

    void OnBoost(InputManager.Info data) {
        if(mBlockRiser != null) {
            if(data.state == InputManager.State.Pressed) {
                mBlockRiser.boost = true;
            }
            else if(data.state == InputManager.State.Released) {
                mBlockRiser.boost = false;
            }
        }
    }

    private void InputEnable(bool yes) {
        InputManager input = Main.instance != null ? Main.instance.input : null;

        if(input != null) {
            if(yes) {
                input.AddButtonCall(player, InputAction.RotateLeft, OnRotateLeft);
                input.AddButtonCall(player, InputAction.RotateRight, OnRotateRight);
                input.AddButtonCall(player, InputAction.Boost, OnBoost);
            }
            else {
                input.RemoveButtonCall(player, InputAction.RotateLeft, OnRotateLeft);
                input.RemoveButtonCall(player, InputAction.RotateRight, OnRotateRight);
                input.RemoveButtonCall(player, InputAction.Boost, OnBoost);
            }

            mInputEnabled = yes;
        }
        else {
            mInputEnabled = false;
        }
    }
}
