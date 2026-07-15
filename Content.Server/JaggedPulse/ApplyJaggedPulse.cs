// ИСПРАВЛЕНО: в апстриме space-wizards/space-station-14 (от которого
// форкнут Goob-Station) класс ReagentEffect был переименован в EntityEffect
// в рамках рефакторинга "Entity Effects ECS". Смотрите как референс:
//   Content.Server/EntityEffects/Effects/Drunk.cs
// в текущем space-station-14 - именно оттуда взят паттерн ниже.
//
// Изменения:
//   - ReagentEffect            -> EntityEffect
//   - Content.Shared.Chemistry.Reagent -> Content.Shared.EntityEffects
//   - ReagentEffectArgs        -> EntityEffectBaseArgs
//   - args.SolutionEntity      -> args.TargetEntity
//
// Если у вас в Goob-Station этот рефакторинг ещё не подтянут (маловероятно,
// но форки иногда отстают от апстрима) - компилятор укажет на это же место,
// и тогда стоит поискать по репозиторию актуальное имя класса, например
// через "Find in Files" по "ReagentEffectGuidebookText" - это имя метода
// не менялось и есть в любой версии.

using Content.Shared.EntityEffects;
using Content.Shared.JaggedPulse;
using Robust.Shared.Prototypes;

namespace Content.Server.JaggedPulse;

public sealed partial class ApplyJaggedPulse : EntityEffect
{
    /// <summary>
    /// На сколько времени навешивается/продлевается эффект за один тик
    /// метаболизма.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Насколько сильный "укол" - 0..1.
    /// </summary>
    [DataField]
    public float EffectIntensity = 1f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var sys = args.EntityManager.EntitySysManager.GetEntitySystem<JaggedPulseSystem>();
        sys.ApplyJaggedPulse(args.TargetEntity, Duration, EffectIntensity);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-jagged-pulse", ("chance", Probability));
}
