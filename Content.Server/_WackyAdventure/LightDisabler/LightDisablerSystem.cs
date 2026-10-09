using Content.Shared.Light;
using Robust.Shared.GameObjects;

namespace Content.Server.LightDisablerSystem;

public sealed class LightDisablerSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    private readonly Dictionary<EntityUid, bool> _previousLightState = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var suppressors = new List<(LightDisablerComponent Comp, TransformComponent Xform)>();
        var q = EntityQueryEnumerator<LightDisablerComponent, TransformComponent>();
        while (q.MoveNext(out _, out var comp, out var xform))
            suppressors.Add((comp, xform));

        if (suppressors.Count == 0 && _previousLightState.Count == 0)
            return;

        foreach (var (comp, xform) in suppressors)
        {
            foreach (var entity in _entityLookup.GetEntitiesInRange(xform.Coordinates, comp.Radius))
            {
                if (!_pointLightSystem.TryGetLight(entity, out var light))
                    continue;

                if (!light.Enabled)
                    continue;

                _previousLightState.TryAdd(entity, true);
                _pointLightSystem.SetEnabled(entity, false, light);
            }
        }

        if (_previousLightState.Count == 0)
            return;

        var toEnable = new List<EntityUid>();
        foreach (var (entity, wasEnabled) in _previousLightState)
        {
            if (!wasEnabled)
                continue;

            if (!TryComp<TransformComponent>(entity, out var lightXform))
            {
                toEnable.Add(entity);
                continue;
            }

            var stillSuppressed = false;
            foreach (var (comp, xform) in suppressors)
            {
                if (lightXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var dist)
                    && dist <= comp.Radius)
                {
                    stillSuppressed = true;
                    break;
                }
            }

            if (!stillSuppressed)
                toEnable.Add(entity);
        }

        foreach (var entity in toEnable)
        {
            if (_pointLightSystem.TryGetLight(entity, out var light))
                _pointLightSystem.SetEnabled(entity, true, light);

            _previousLightState.Remove(entity);
        }
    }
}