using System.Threading;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
namespace Content.Goobstation.Server._WackyAdventure.Bed;
[RegisterComponent]
public sealed partial class BedInjectingBedComponent : Component
{
    [DataField("reagent")]
    public string Reagent = "Sarin2";

    [DataField("amount")]
    public float Amount = 5f;

    // Токен для отмены фонового таймера впрыска
    public CancellationTokenSource? CancelToken;
}