using UnityEngine;
using UnityEngine.EventSystems;

namespace RadioMod.Client
{
    /// <summary>
    /// Makes the recordings window modal: while it is open the game underneath must not react to
    /// the mouse.
    ///
    /// ConfigurationManager's own recipe (unlock the cursor, put a full-screen IMGUI button behind
    /// the window, call <c>Input.ResetInputAxes</c>) is not enough on its own — it never touches the
    /// EventSystem, and an IMGUI control only ever consumes IMGUI events. The game's menus run on
    /// uGUI, which knows nothing about IMGUI, so clicks pass straight through to them. Disabling the
    /// EventSystem is the part that actually closes that hole.
    ///
    /// Safety rule: a disabled EventSystem must never outlive the window. <see cref="EnsureClosed"/>
    /// runs from Update every frame the window is not open, so a single exception thrown mid-draw
    /// cannot leave the player in a menu with a dead mouse.
    /// </summary>
    internal static class WindowModality
    {
        private static bool _active;
        private static bool _eventSystemWasEnabled;
        private static EventSystem _disabledEventSystem;
        private static CursorLockMode _savedLockState;
        private static bool _savedCursorVisible;

        public static bool IsActive => _active;

        /// <summary>Takes over input. Safe to call every frame; only the first call does work.</summary>
        public static void Open()
        {
            if (_active)
            {
                return;
            }

            _active = true;

            // Remember what the game had, not what we assume it had — the player may be in a screen
            // that legitimately wants the cursor locked or hidden.
            _savedLockState = Cursor.lockState;
            _savedCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            try
            {
                EventSystem current = EventSystem.current;
                if (current != null)
                {
                    _disabledEventSystem = current;
                    _eventSystemWasEnabled = current.enabled;
                    current.enabled = false;
                }
            }
            catch
            {
                // A missing or replaced EventSystem must not break the window; the mod simply
                // falls back to the weaker IMGUI-only blocking.
                _disabledEventSystem = null;
            }
        }

        /// <summary>Hands input back exactly as it was found.</summary>
        public static void Close()
        {
            if (!_active)
            {
                return;
            }

            _active = false;

            try
            {
                if (_disabledEventSystem != null)
                {
                    _disabledEventSystem.enabled = _eventSystemWasEnabled;
                }
            }
            catch
            {
                // Nothing useful to do — the component is gone, which means it is not stuck disabled.
            }

            _disabledEventSystem = null;
            Cursor.lockState = _savedLockState;
            Cursor.visible = _savedCursorVisible;
        }

        /// <summary>
        /// Called from Update whenever the window is not open. This is the guard that makes the
        /// whole thing safe: closing is not allowed to depend on the draw path finishing.
        /// </summary>
        public static void EnsureClosed()
        {
            if (_active)
            {
                Close();
            }
        }

        /// <summary>
        /// Full-screen layer behind the window: swallows IMGUI clicks, dims the scene so the window
        /// reads as modal, and reports a click outside the window so the caller can close it.
        /// Must be drawn before the window itself.
        /// </summary>
        public static bool DrawBlocker(Rect windowRect, float dim)
        {
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            Color prev = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, dim);
            GUI.DrawTexture(screen, Texture2D.whiteTexture);
            GUI.color = prev;

            // An invisible button over the whole screen is what stops IMGUI clicks reaching
            // anything drawn behind us this frame.
            bool clicked = GUI.Button(screen, GUIContent.none, GUIStyle.none);

            Vector2 mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            return clicked && !windowRect.Contains(mouse);
        }

        // Deliberately no Input.ResetInputAxes() here.
        //
        // ConfigurationManager calls it, and it was tried — but ResetInputAxes clears mouse BUTTONS
        // as well as axes ("all buttons return to 0 for one frame"), which killed the window's own
        // resize grip: it polls Input.GetMouseButtonDown/GetMouseButton directly. Gating the call
        // does not help either, because the frame the button first goes down is exactly the frame
        // the previous reset has already blanked.
        //
        // The two remaining layers are the ones that actually matter and are confirmed working in
        // game: the disabled EventSystem stops uGUI clicks reaching the menus, and the full-screen
        // IMGUI blocker stops IMGUI ones. Per the spec, a targeted key block is added only if
        // testing shows an actual leak — not pre-emptively at the cost of our own controls.
    }
}
