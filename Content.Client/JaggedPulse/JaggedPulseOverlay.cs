using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.JaggedPulse;

public sealed class JaggedPulseOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Нужен ScreenTexture, чтобы шейдер мог наложить рваные "зубья" поверх
    // уже отрисованной сцены, а не просто залить экран сплошным цветом.
    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    // Эти поля синкает JaggedPulseOverlaySystem из JaggedPulseComponent
    // (компонент лежит на служебной сущности статус-эффекта, не на игроке -
    // см. комментарий в JaggedPulseOverlaySystem.cs).
    public TimeSpan AppliedAt;
    public TimeSpan PulseInterval = TimeSpan.FromSeconds(1);
    public TimeSpan PulseDuration = TimeSpan.FromSeconds(0.18);
    public float Intensity = 1f;

    public JaggedPulseOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("JaggedPulse").InstanceUnique();
    }

    // Как в RainbowOverlay: рисуем только в вьюпорте, который смотрит
    // именно глазами текущего локального игрока (а не в любом другом,
    // например при спектейте/мультивью).
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        return args.Viewport.Eye == eyeComp.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        // amount - общая тяжесть состояния: насколько сильно виньетка
        // стянута к центру и насколько сильно всё обесцвечено. Берём
        // напрямую из Intensity (0..1), никакого затухания по времени -
        // это фон, который держится всё время действия эффекта.
        var amount = Intensity;

        if (amount <= 0f)
            return; // нечего рисовать

        // pulse - лёгкий "удар сердца": та же резкая атака + линейное
        // затухание, что раньше двигала зубья, теперь чуть покачивает
        // радиус виньетки в такт PulseInterval/PulseDuration.
        var interval = PulseInterval.TotalSeconds;
        if (interval <= 0)
            interval = 1;

        var elapsed = (_timing.CurTime - AppliedAt).TotalSeconds;
        if (elapsed < 0)
            elapsed = 0;

        var phase = elapsed % interval;
        var duration = Math.Max(PulseDuration.TotalSeconds, 0.001);

        var pulse = phase < duration
            ? (float) (1.0 - phase / duration)
            : 0f;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("amount", amount);
        _shader.SetParameter("pulse", pulse);

        var handle = args.WorldHandle;
        var viewport = args.WorldBounds;

        handle.UseShader(_shader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }
}
