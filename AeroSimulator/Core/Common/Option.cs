using System;

namespace AeroSimulator.Core.Common;

public readonly struct Option<T>
{
    private readonly T _value;
    
    public bool HasValue { get; }

    public T Value => HasValue ? _value : throw new InvalidOperationException("Próba odczytu z pustej monady (None).");

    private Option(T value, bool hasValue)
    {
        _value = value;
        HasValue = hasValue;
    }

    public static Option<T> Some(T value) => new(value, true);
    public static Option<T> None() => new(default!, false); 

    // ==========================================
    // NOWE METODY FUNKCYJNE
    // ==========================================

    // Zwraca wartość z monady lub bezpieczny fallback, jeśli monada jest pusta
    public T ValueOr(T defaultValue) => HasValue ? _value : defaultValue;

    // Wykonuje przekazaną akcję (efekt uboczny) tylko wtedy, gdy wartość istnieje
    public void IfPresent(Action<T> action)
    {
        if (HasValue) action(_value);
    }
}