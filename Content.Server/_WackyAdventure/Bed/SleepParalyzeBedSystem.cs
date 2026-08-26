using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Goobstation.Server._WackyAdventure.Bed;
public sealed class BedInjectingSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SleepParalyzeBedComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<SleepParalyzeBedComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnStrapped(EntityUid uid, SleepParalyzeBedComponent component, StrappedEvent args)
    {
        EntityUid target = args.Buckle;
//        Logger.InfoS("SleepBed", $"Кто-то пристегнулся к кровати! Цель: {target}");

        // Отменяем старый токен, если вдруг остался
        component.CancelToken?.Cancel();
        component.CancelToken = new CancellationTokenSource();
        var token = component.CancelToken.Token;

        // Запускаем асинхронный цикл впрыска каждую 1 секунду
        _ = InjectLoopAsync(uid, target, component, token);
    }

    private void OnUnstrapped(EntityUid uid, SleepParalyzeBedComponent component, UnstrappedEvent args)
    {
//        Logger.InfoS("SleepBed", "Кто-то встал с кровати.");
        // Останавливаем таймер впрыска
        component.CancelToken?.Cancel();
        component.CancelToken = null;
    }

    private async Task InjectLoopAsync(EntityUid bedUid, EntityUid target, SleepParalyzeBedComponent component, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);

                if (token.IsCancellationRequested)
                    break;

                if (Deleted(bedUid) || Deleted(target))
                    break;

                if (_solutionContainer.TryGetInjectableSolution(target, out var solEnt, out var solution))
                {
                    var success = _solutionContainer.TryAddReagent(solEnt.Value, component.Reagent, component.Amount);
//                    Logger.InfoS("SleepBed", $"Периодическая инъекция '{component.Reagent}' ({component.Amount}) для {target}: {success}");
                }
                else
                {
//                    Logger.ErrorS("SleepBed", $"У цели {target} нет доступного для инъекций раствора!");
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Штатное завершение при вставании
        }
    }
}