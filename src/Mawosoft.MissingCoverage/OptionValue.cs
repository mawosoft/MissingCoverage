// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

namespace Mawosoft.MissingCoverage;

[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not needed.")]
internal struct OptionValue<T>(T defaultValue)
{
    private T _value = defaultValue;

    public T Value
    {
        readonly get => _value;
        set
        {
            _value = value;
            IsSet = true;
        }
    }

    public bool IsSet { get; private set; } = false;

    public override readonly string ToString() => _value?.ToString() + (IsSet ? "!" : "");

    public static implicit operator T(OptionValue<T> @this) => @this.Value;
    public static implicit operator OptionValue<T>(T value) => new() { Value = value };
}
