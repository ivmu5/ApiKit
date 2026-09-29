namespace ApiKit.Crud.Attributes;

/// <summary>
/// Внутренние метаданные стандартной CRUD-операции.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal sealed class CrudOperationAttribute(CrudOperationKind operation) : Attribute
{
    public CrudOperationKind Operation { get; } = operation;
}

internal enum CrudOperationKind
{
    Read,
    Create,
    Update,
    Delete
}
