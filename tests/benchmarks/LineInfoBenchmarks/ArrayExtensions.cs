// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Runtime.CompilerServices;

namespace LineInfoBenchmarks
{
    public static class ArrayExtensions
    {
        // TODO ref this T[] array is not allowed
        public static void EnsureSize<T>(ref T[] array, int index)
        {
            if (index < array.Length) return;
            Grow(ref array, index + 1);

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Grow(ref T[] array, int capacity)
            {
                int newCapacity = array.Length == 0 ? 4 : 2 * array.Length;
                // net60 has Array.MaxLength which is lower than int.MaxValue.
                if ((uint)newCapacity > int.MaxValue) newCapacity = int.MaxValue;
                if (newCapacity < capacity) newCapacity = capacity;
                Array.Resize(ref array, newCapacity);
            }
        }
    }
}
