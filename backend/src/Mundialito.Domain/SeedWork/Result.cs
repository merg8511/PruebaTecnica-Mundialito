namespace Mundialito.Domain.SeedWork;

/// <summary>
/// Resultado de una operación sin valor de retorno.
/// Sustituye el uso de excepciones como control de flujo de negocio.
/// </summary>
public class Result
{
    /// <summary>La operación fue exitosa.</summary>
    public bool IsSuccess { get; }

    /// <summary>La operación falló.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Código de error semántico del catálogo cerrado de <see cref="DomainErrors"/>.
    /// Null cuando <see cref="IsSuccess"/> es true.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Mensaje de error legible. Null cuando <see cref="IsSuccess"/> es true.
    /// </summary>
    public string? ErrorMessage { get; }

    // ─── Constructores privados ───────────────────────────────────────────────
    protected Result(bool isSuccess, string? errorCode, string? errorMessage)
    {
        IsSuccess    = isSuccess;
        ErrorCode    = errorCode;
        ErrorMessage = errorMessage;
    }

    // ─── Factories estáticas ──────────────────────────────────────────────────

    /// <summary>Crea un resultado exitoso.</summary>
    public static Result Ok() => new(true, null, null);

    /// <summary>Crea un resultado de fallo con código y mensaje.</summary>
    public static Result Fail(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage);
}

/// <summary>
/// Resultado de una operación que devuelve un valor de tipo <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Tipo del valor retornado en caso de éxito. Must be non-null.</typeparam>
public sealed class Result<T> : Result where T : notnull
{
    private readonly T? _value;

    /// <summary>
    /// Valor del resultado. Solo válido cuando <see cref="Result.IsSuccess"/> es true.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si se accede al valor en un resultado fallido.</exception>
    public T? Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException(
                    $"Cannot access Value on a failed Result. ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}");
            return _value;
        }
    }

    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage)
        : base(isSuccess, errorCode, errorMessage)
    {
        _value = value;
    }

    /// <summary>
    /// Crea un resultado exitoso con el valor especificado.
    /// Lanza <see cref="InvalidOperationException"/> si <paramref name="value"/> es null,
    /// ya que Result&lt;T&gt; where T : notnull no puede contener un Value null en éxito.
    /// </summary>
    public static Result<T> Ok(T value)
    {
        if (value is null)
            throw new InvalidOperationException(
                $"Result<{typeof(T).Name}>.Ok(value) cannot be called with a null value. " +
                "This is a programming error in the factory method.");

        return new(true, value, null, null);
    }

    /// <summary>Crea un resultado de fallo con código y mensaje.</summary>
    public new static Result<T> Fail(string errorCode, string errorMessage) =>
        new(false, default, errorCode, errorMessage);
}
