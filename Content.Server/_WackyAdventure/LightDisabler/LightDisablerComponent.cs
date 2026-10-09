using Robust.Shared.GameObjects;

using Component = Robust.Shared.GameObjects.Component;

namespace Content.Server.LightDisablerSystem;

[RegisterComponent]
public sealed partial class LightDisablerComponent : Component
{
    [DataField]
    public float Radius = 5f;
}