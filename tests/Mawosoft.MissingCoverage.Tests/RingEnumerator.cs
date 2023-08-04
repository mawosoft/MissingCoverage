// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Mawosoft.MissingCoverage.Tests
{
    internal sealed class RingEnumerator<T> : IEnumerator<T>
    {
        private IEnumerator<T> _enumerator;
        private readonly IEnumerable<T>? _enumerable;

        public RingEnumerator(IEnumerator<T> source)
            => _enumerator = source ?? throw new ArgumentNullException(nameof(source));

        public RingEnumerator(IEnumerable<T> source)
        {
            _enumerable = source ?? throw new ArgumentNullException(nameof(source));
            _enumerator = source.GetEnumerator();
        }

        public T Current => _enumerator.Current;

        object IEnumerator.Current => Current!;

        public void Dispose() => _enumerator?.Dispose();

        public bool MoveNext()
        {
            if (_enumerator.MoveNext()) return true;
            Reset();
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            if (_enumerable == null)
            {
                _enumerator.Reset();
            }
            else
            {
                _enumerator?.Dispose();
                _enumerator = _enumerable.GetEnumerator();
            }
        }
    }
}
