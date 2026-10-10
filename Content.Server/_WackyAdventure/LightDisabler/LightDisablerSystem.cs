using Content.Shared.Light;
using Robust.Shared.GameObjects;

namespace Content.Server.LightDisablerSystem;

public sealed class LightDisablerSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    private readonly Dictionary<EntityUid, float> _originalEnergy = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var suppressors = new List<(LightDisablerComponent Comp, TransformComponent Xform)>();
        var q = EntityQueryEnumerator<LightDisablerComponent, TransformComponent>();
        while (q.MoveNext(out _, out var comp, out var xform))
            suppressors.Add((comp, xform));

        if (suppressors.Count == 0 && _originalEnergy.Count == 0)
            return;

        foreach (var (comp, xform) in suppressors)
        {
            foreach (var entity in _entityLookup.GetEntitiesInRange(xform.Coordinates, comp.Radius))
            {

                if (!_pointLightSystem.TryGetLight(entity, out var light))
                    continue;
                if (!light.Enabled)
                    continue;
                if (!_originalEnergy.TryGetValue(entity,out var original))
                {
                    original = light.Energy;
                    _originalEnergy[entity] = original;
                }

                var target = MathF.Max(0f, original - comp.Power);
                var newEnergy = MathF.Max(target, light.Energy - comp.DecaySpeed * frameTime);
                _pointLightSystem.SetEnergy(entity, newEnergy, light);
            }
        }

        if (_originalEnergy.Count == 0)
            return;

        var toRestore = new List<EntityUid>();
        foreach (var entity in _originalEnergy.Keys)
        {
            if (!TryComp<TransformComponent>(entity, out var lightXform))
            {
                toRestore.Add(entity);
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
                toRestore.Add(entity);
        }

        foreach (var entity in toRestore)
        {
            if (_pointLightSystem.TryGetLight(entity, out var light))
                _pointLightSystem.SetEnergy(entity, _originalEnergy[entity], light);

            _originalEnergy.Remove(entity);
        }
    }
}