// Структура и стиль намеренно повторяют существующий в репозитории
// Content.Shared/Traits/Assorted/PermanentBlindnessComponent.cs,
// чтобы не выбиваться из конвенций кодовой базы.

using Robust.Shared.GameStates;

namespace Content.Shared.JaggedPulse;

/// <summary>
/// Висит на сущности, пока действует эффект "рваного пульса".
/// Раз в <see cref="PulseInterval"/> клиентский оверлей рисует резкую
/// зубчатую вспышку по краям экрана, которая быстро гаснет.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class JaggedPulseComponent : Component
{
    /// <summary>
    /// Момент времени (curTime на сервере), когда эффект был применён
    /// или последний раз продлён. От него на клиенте отсчитывается фаза
    /// пульсации, поэтому отдельный тикающий таймер не нужен.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AppliedAt;

    /// <summary>
    /// Когда эффект нужно снять. Null = снимается вручную (RemComp) извне,
    /// а не по внутреннему таймеру.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? EndTime;

    /// <summary>
    /// Как часто "бьёт" эффект. По ТЗ - раз в секунду.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PulseInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Сколько длится сама вспышка: резкая атака -> быстрое линейное
    /// затухание. Не путать с PulseInterval - это именно "ширина удара".
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PulseDuration = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// 0..1 - насколько глубоко "зубья" вонзаются в экран и насколько
    /// они непрозрачны. Удобно завязать на дозу вещества / стадию передоза.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Intensity = 1f;
}
