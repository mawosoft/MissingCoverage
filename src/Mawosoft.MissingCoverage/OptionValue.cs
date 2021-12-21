// Copyright (c) 2021 Matthias Wolf, Mawosoft.

namespace Mawosoft.MissingCoverage
{
    internal struct OptionValue<T>
    {
        private T _value;

        public T Value
        {
            get => _value;
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

        public override string ToString() => _value?.ToString() + (IsSet ? "!" : "");

        public static implicit operator T(OptionValue<T> @this) => @this.Value;
        public static implicit operator OptionValue<T>(T value) => new() { Value = value };
    }
}
