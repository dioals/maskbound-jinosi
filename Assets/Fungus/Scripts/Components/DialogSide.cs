// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

namespace Fungus
{
    /// <summary>
    /// Which side of the Say Dialog a character's dialogue box appears on.
    /// </summary>
    public enum DialogSide
    {
        /// <summary> Dialogue box on the left side. </summary>
        Left,
        /// <summary> Dialogue box on the right side. </summary>
        Right
    }

    /// <summary>
    /// Optional override for the dialogue box side on a Say command.
    /// None falls back to the speaking character's Dialog Side.
    /// </summary>
    public enum DialogSideOverride
    {
        /// <summary> Use the speaking character's Dialog Side. </summary>
        None,
        /// <summary> Force the dialogue box to the left side. </summary>
        Left,
        /// <summary> Force the dialogue box to the right side. </summary>
        Right
    }
}
