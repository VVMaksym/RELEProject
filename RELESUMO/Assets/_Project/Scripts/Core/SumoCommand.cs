using UnityEngine;

namespace ReleSumo.Core
{
    /// <summary>
    /// Controller-independent hybrid action consumed by <c>SumoMotor</c>.
    /// Move is continuous; dash, shove, and brace are discrete flags.
    /// </summary>
    public readonly struct SumoCommand
    {
        public static readonly SumoCommand None =
            new(Vector2.zero, dash: false, shove: false, brace: false);

        public Vector2 Move { get; }
        public bool Dash { get; }
        public bool Shove { get; }
        public bool Brace { get; }

        public SumoCommand(Vector2 move, bool dash, bool shove, bool brace)
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            Dash = dash;
            Shove = shove;
            Brace = brace;
        }
    }
}
