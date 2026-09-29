using System.ComponentModel.DataAnnotations;

namespace ApiKit.Crud.Models;

/// <summary>
/// Представляет параметры стандартной offset-пагинации CRUD collection endpoint.
/// </summary>
/// <remarks>
/// Базовый CRUD намеренно не определяет универсальный язык фильтрации или сортировки.
/// Предметные условия следует задавать обычным LINQ в переопределяемом query pipeline
/// либо полностью переопределять collection action.
/// </remarks>
public class CrudPageOptions
{
    /// <summary>
    /// Номер страницы, начиная с 1.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Размер страницы. Если значение не задано, контроллер использует размер по умолчанию.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? PageSize { get; set; }
}
