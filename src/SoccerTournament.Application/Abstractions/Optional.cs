namespace SoccerTournament.Application.Abstractions;

/// <summary>
/// Valor opcional para PATCH: distingue "no enviado" (no modificar) de "enviado como null" (limpiar el campo).
/// </summary>
public readonly record struct Optional<T>
{
    private Optional(T value)
    {
        HasValue = true;
        Value = value;
    }

    public bool HasValue { get; }

    public T Value { get; }

    public static Optional<T> Unspecified => default;

    public static Optional<T> Of(T value) => new(value);

    /// <summary>
    /// Devuelve el valor enviado o, si no se envió, el valor actual.
    /// </summary>
    public T GetValueOrDefault(T currentValue) => HasValue ? Value : currentValue;
}
