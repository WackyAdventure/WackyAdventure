using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.JaggedPulse;

/// <summary>
/// Общая (shared) логика эффекта. Клиенту компонент прилетает/пропадает
/// через обычную репликацию состояния - клиентский оверлей вешается
/// отдельной системой в Content.Client (см. JaggedPulseOverlaySystem).
/// </summary>
public sealed class JaggedPulseSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JaggedPulseComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<JaggedPulseComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.AppliedAt = _timing.CurTime;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Снятие по таймеру - авторитетное действие, делает только сервер.
        // На клиенте компонент пропадёт сам через синхронизацию состояния.
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<JaggedPulseComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EndTime is { } end && _timing.CurTime >= end)
                RemComp<JaggedPulseComponent>(uid);
        }
    }

    /// <summary>
    /// Навешивает эффект на сущность или продлевает уже висящий.
    /// Вызывайте из ReagentEffect, StatusEffect-а, админ-команды и т.д.
    /// </summary>
    /// <param name="uid">Кому вешаем эффект (обычно - мобу/игроку).</param>
    /// <param name="duration">На сколько времени.</param>
    /// <param name="intensity">0..1, насколько сильный эффект.</param>
    /// <param name="pulseInterval">Необязательно: как часто "бьёт".</param>
    /// <param name="pulseDuration">Необязательно: ширина самого удара.</param>
    public void ApplyJaggedPulse(
        EntityUid uid,
        TimeSpan duration,
        float intensity = 1f,
        TimeSpan? pulseInterval = null,
        TimeSpan? pulseDuration = null)
    {
        var comp = EnsureComp<JaggedPulseComponent>(uid);

        comp.Intensity = MathF.Max(intensity, comp.Intensity);

        if (pulseInterval is { } interval)
            comp.PulseInterval = interval;

        if (pulseDuration is { } dur)
            comp.PulseDuration = dur;

        var newEnd = _timing.CurTime + duration;
        if (comp.EndTime is not { } currentEnd || newEnd > currentEnd)
            comp.EndTime = newEnd;

        Dirty(uid, comp);
    }
}
