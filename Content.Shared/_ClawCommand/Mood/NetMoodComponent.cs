using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Mood;

/// <summary>
///     Networked mirror of the server-only <c>MoodComponent</c>. It exists so the client can predict the
///     movement speed modifier that mood applies, without ever learning the identity of the individual
///     moodlets that produced it. All mood logic is otherwise handled by the server.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NetMoodComponent : Component
{
    [DataField, AutoNetworkedField]
    public float CurrentMoodLevel;

    [DataField, AutoNetworkedField]
    public float NeutralMoodThreshold = 50f;

    [DataField, AutoNetworkedField]
    public MoodThreshold CurrentMoodThreshold = MoodThreshold.Neutral;

    /// <summary>
    ///     The base of the geometric sequence used for the high-mood speed bonus.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedBonusGrowth = 1.003f;

    /// <summary>
    ///     The lowest point that low morale can multiply our movement speed by.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinimumSpeedModifier = 0.75f;

    /// <summary>
    ///     The maximum amount that high morale can multiply our movement speed by.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaximumSpeedModifier = 1.15f;
}
