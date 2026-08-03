namespace Content.Shared._ClawCommand.Body;

/// <summary>
///     Claw Command - Turns the two character-sprite scales into a body weight.
///
///     Kept as plain statics with no entity involved so the lobby can show a live figure while the
///     player drags the sliders, and the server can compute the same number for a spawned mob,
///     without either duplicating the formula.
/// </summary>
public static class BodyWeight
{
    /// <summary>
    ///     How much heavier or lighter this build is than the species baseline.
    ///
    ///     A body is three-dimensional but only two scales are exposed, so the width slider is taken
    ///     to widen the character front-to-back as well as side-to-side. That makes volume - and so
    ///     mass - scale with height once and width twice. Squaring width is also what stops the
    ///     sliders feeling interchangeable: broadening a character does noticeably more to their
    ///     weight than lengthening them, which is how real bodies behave.
    /// </summary>
    public static float GetScale(float height, float width)
    {
        return height * width * width;
    }

    /// <summary>
    ///     Body weight in kilograms.
    /// </summary>
    public static float GetWeight(float baseWeight, float height, float width)
    {
        return baseWeight * GetScale(height, width);
    }

    /// <summary>
    ///     Standing height in centimetres. Straight linear scale off the species baseline.
    /// </summary>
    public static float GetHeightCm(float baseHeightCm, float height)
    {
        return baseHeightCm * height;
    }
}
