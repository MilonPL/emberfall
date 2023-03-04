namespace Content.Server.Emp.Components;

/// <summary>
/// Upon being triggered will EMP area around it.
/// </summary>
[RegisterComponent]
sealed class EmpOnTriggerComponent : Component
{
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    float Range = 1.0f;
}
