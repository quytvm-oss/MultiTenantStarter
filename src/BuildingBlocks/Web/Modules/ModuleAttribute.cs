namespace Web.Modules;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class ModuleAttribute: Attribute
{
    public Type ModuleType { get; }

    /// <summary>
    /// Optional ordering hint that allows hosts to control module startup sequencing.
    /// Lower numbers execute first.
    /// </summary>
    public int Order { get; }

    public ModuleAttribute(Type moduleType, int order = 0)
    {
        ModuleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
        Order = order;
    }
}