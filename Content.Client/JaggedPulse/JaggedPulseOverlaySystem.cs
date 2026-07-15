using Content.Shared.JaggedPulse;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.JaggedPulse;

/// <summary>
/// Вешает/снимает JaggedPulseOverlay в зависимости от того, есть ли
/// JaggedPulseComponent на текущей локально управляемой сущности игрока.
///
/// ВАЖНО: JaggedPulseComponent висит не на моба-игроке, а на служебной
/// сущности самого статус-эффекта (архитектура Content.Shared.StatusEffectNew).
/// Поэтому подписываемся не на ComponentStartup/ComponentShutdown, а на
/// StatusEffectAppliedEvent/StatusEffectRemovedEvent, и сравниваем
/// локального игрока именно с args.Target, а не с ent (uid компонента).
///
/// StatusEffectRelayedEvent<LocalPlayerAttachedEvent/DetachedEvent> нужны
/// на случай, если игрок подключается/переключается на моба, у которого
/// эффект уже был активен (например, гостом заходит в тело с накопленным
/// эффектом) - иначе оверлей не появится/не исчезнет вовремя.
/// </summary>
public sealed class JaggedPulseOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private JaggedPulseOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JaggedPulseComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<JaggedPulseComponent, StatusEffectRemovedEvent>(OnRemoved);

        SubscribeLocalEvent<JaggedPulseComponent, StatusEffectRelayedEvent<LocalPlayerAttachedEvent>>(OnPlayerAttached);
        SubscribeLocalEvent<JaggedPulseComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnApplied(Entity<JaggedPulseComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        SyncOverlay(ent.Comp);
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnRemoved(Entity<JaggedPulseComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(Entity<JaggedPulseComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        SyncOverlay(ent.Comp);
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<JaggedPulseComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void SyncOverlay(JaggedPulseComponent comp)
    {
        _overlay.AppliedAt = comp.AppliedAt;
        _overlay.PulseInterval = comp.PulseInterval;
        _overlay.PulseDuration = comp.PulseDuration;
        _overlay.Intensity = comp.Intensity;
    }
}
