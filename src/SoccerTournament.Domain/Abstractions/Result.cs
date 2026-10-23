namespace SoccerTournament.Domain.Abstractions;

/// <summary>
/// Resultado de una operación sin valor de retorno. Reemplaza el uso de excepciones como control de flujo.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        // Protección de invariantes del propio tipo: indica un error de programación, no un flujo de negocio
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Un resultado exitoso no puede contener un error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Un resultado fallido debe contener un error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public string? ErrorMessage => IsFailure ? Error.Message : null;

    public string? ErrorCode => IsFailure ? Error.Code : null;

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Resultado de una operación que, si es exitosa, produce un valor.
/// </summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Valor del resultado. Acceder a él en un resultado fallido es un error de programación;
    /// siempre debe verificarse <see cref="Result.IsSuccess"/> antes.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede acceder al valor de un resultado fallido.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
