using Content.Shared._ClawCommand.Lavaland.Megafauna.Systems;

// ReSharper disable once CheckNamespace
namespace Content.Shared._ClawCommand.Lavaland.Megafauna.Selectors;

public sealed partial class RandomPickCoordinatesSelector : MegafaunaSelector
{
    [DataField(required: true)]
    public float Radius;

    protected override float InvokeImplementation(MegafaunaCalculationBaseArgs args)
    {
        var system = args.EntityManager.System<MegafaunaSystem>();
        system.PickRandomPosition(args, Radius);

        return DelaySelector.Get(args);
    }
}
