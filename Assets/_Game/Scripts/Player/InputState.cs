using UnityEngine;

namespace DragonHeist
{
    /// <summary>Единая точка ввода: клавиатура/мышь (ПК) и тач-контролы (мобилки) пишут сюда.</summary>
    public static class InputState
    {
        public static Vector2 Move;          // -1..1
        public static bool JumpPressed;      // однократно за кадр
        public static bool ActionHeld;
        public static Vector2 LookDelta;     // пиксели
        public static float Zoom;
        public static bool Mobile;
        public static bool Blocked;         // открыто главное меню — персонаж не управляется

        // выставляются тач-контролами
        public static Vector2 TouchMove;
        public static bool TouchJump;
        public static bool TouchAction;
        public static Vector2 TouchLook;
        public static float TouchZoom;

        public static void Poll()
        {
            Vector2 kb = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) kb.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) kb.y -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) kb.x += 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) kb.x -= 1;
            Move = Vector2.ClampMagnitude(kb + TouchMove, 1f);

            JumpPressed = Input.GetKeyDown(KeyCode.Space) || TouchJump;
            TouchJump = false;
            ActionHeld = Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.F) || TouchAction;

            LookDelta = TouchLook;
            TouchLook = Vector2.zero;
            if (!Mobile && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
                LookDelta += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 12f;

            Zoom = -Input.mouseScrollDelta.y + TouchZoom;
            TouchZoom = 0;
        }
    }
}
