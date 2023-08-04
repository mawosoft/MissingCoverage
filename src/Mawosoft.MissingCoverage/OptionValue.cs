// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System.Diagnostics.CodeAnalysis;

namespace Mawosoft.MissingCoverage;

[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not needed.")]
internal struct OptionValue<T>
{
    private T _value;

    public T Value
    {
        readonly get => _value;
        set
        {
            _value = value;
            IsSet = true;
        }
    }

    public bool IsSet { get; private set; }

    public OptionValue(T defaultValue)
    {
        _value = defaultValue;
        IsSet = false;
    }

    public override readonly string ToString() => _value?.ToString() + (IsSet ? "!" : "");

    public static implicit operator T(OptionValue<T> @this) => @this.Value;
    public static implicit operator OptionValue<T>(T value) => new() { Value = value };
}
