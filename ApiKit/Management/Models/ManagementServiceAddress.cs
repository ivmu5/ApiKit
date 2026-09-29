namespace ApiKit.Management.Models;

/// <summary>
/// Описывает адрес, на котором текущий экземпляр сервиса принимает подключения.
/// </summary>
/// <param name="Address">Адрес в формате, предоставленном ASP.NET Core server.</param>
public sealed record ManagementServiceAddress(string Address);
