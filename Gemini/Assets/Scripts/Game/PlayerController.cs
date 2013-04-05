using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {
    public Cursor cursor;

    private bool mInputEnabled = false;
    //TODO: set which player input: player1, player2, etc.

    void OnDestroy() {
        InputEnable(false);
    }

    // Use this for initialization
    void Start() {
        //bind inputs
        InputEnable(true);
    }

    void Update() {
        if(mInputEnabled && (cursor.state == Cursor.State.Move || cursor.state == Cursor.State.None)) {
            InputManager input = Main.instance.input;

            float x = input.GetAxis(InputAction.Horizontal);
            float y = input.GetAxis(InputAction.Vertical);

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
            cursor.Rotate(Cursor.RotateDir.CounterClockwise);
        }
    }

    void OnRotateRight(InputManager.Info data) {
        if(data.state == InputManager.State.Pressed) {
            cursor.Rotate(Cursor.RotateDir.Clockwise);
        }
    }

    void OnBoost(InputManager.Info data) {
    }

    private void InputEnable(bool yes) {
        InputManager input = Main.instance != null ? Main.instance.input : null;

        if(input != null) {
            if(yes) {
                input.AddButtonCall(InputAction.RotateLeft, OnRotateLeft);
                input.AddButtonCall(InputAction.RotateRight, OnRotateRight);
                input.AddButtonCall(InputAction.Boost, OnBoost);
            }
            else {
                input.RemoveButtonCall(InputAction.RotateLeft, OnRotateLeft);
                input.RemoveButtonCall(InputAction.RotateRight, OnRotateRight);
                input.RemoveButtonCall(InputAction.Boost, OnBoost);
            }

            mInputEnabled = yes;
        }
        else {
            mInputEnabled = false;
        }
    }
}
