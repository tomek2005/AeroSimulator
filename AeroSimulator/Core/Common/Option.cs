using System;

namespace AeroSimulator.Core.Common;

// 1. Zastosowanie 'readonly struct' zamiast 'class'. 
// Gwarantuje to 100% niezmienność (immutability) i unika alokacji na stercie (wydajność).
public readonly struct Option<T>
{
    private readonly T _value;
    
    // Flaga informująca, czy monada posiada wartość
    public bool HasValue { get; }

    // Bezpieczny dostęp do wartości
    public T Value => HasValue ? _value : throw new InvalidOperationException("Próba odczytu z pustej monady (None).");

    // Prywatny konstruktor wymusza używanie funkcyjnych konstruktorów statycznych
    private Option(T value, bool hasValue)
    {
        _value = value;
        HasValue = hasValue;
    }

    // 2. Czyste funkcje tworzące: Some (jest wartość) oraz None (brak wartości)
    public static Option<T> Some(T value) => new(value, true);
    
    // Używamy default!, ponieważ w przypadku None wartość _value i tak nigdy nie będzie odczytana
    public static Option<T> None() => new(default!, false); 
}