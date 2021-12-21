// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Mawosoft.MissingCoverage.Tests
{
    public class OptionValueTests
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Generic test methods.")]
        private class OptionValueTests_T<T>
        {
            public void Ctor_Implicit()
            {
                OptionValue<T> v = new();
                Assert.Equal(default!, v.Value);
                Assert.False(v.IsSet);
            }

            public void Ctor_DefaultValue(T defaultValue)
            {
                OptionValue<T> v = new(defaultValue);
                Assert.Equal(defaultValue, v.Value);
                Assert.False(v.IsSet);
            }

            public void Value_Roundtrip(T value)
            {
                OptionValue<T> v = new();
                v.Value = value;
                Assert.Equal(value, v.Value);
                Assert.True(v.IsSet);
                v.Value = default!;
                Assert.Equal(default, v.Value);
                Assert.True(v.IsSet);
            }

            public void ToString_AddsIsSetMarker(T value)
            {
                OptionValue<T> v = new(value);
                // Returns empty string for null value
                Assert.Equal(value?.ToString() + "", v.ToString());
                v = value;
                Assert.Equal(value?.ToString() + "!", v.ToString());
            }

            public void ImplicitOperator_T(T value)
            {
                OptionValue<T> v = new();
                v.Value = value;
                T result = v;
                Assert.Equal(value, result);
                v = new(value);
                result = v;
                Assert.Equal(value, result);
            }

            public void ImplicitOperator_OptionValueT(T value)
            {
                OptionValue<T> v = value;
                Assert.True(v.IsSet);
                Assert.Equal(value, v.Value);
            }

            public void AssignStruct_CopiesStruct(T value)
            {
                OptionValue<T> v1 = new(value);
                OptionValue<T> v2 = v1;
                Assert.False(v1.IsSet); // Redundance for clarification
                Assert.Equal(v1.IsSet, v2.IsSet);
                Assert.Equal(v1.Value, v2.Value);
                Assert.Equal(v1, v2);
                v1 = value;
                v2 = v1;
                Assert.True(v1.IsSet); // Redundance for clarification
                Assert.Equal(v1.IsSet, v2.IsSet);
                Assert.Equal(v1.Value, v2.Value);
                Assert.Equal(v1, v2);
            }
        }

        private static void InvokeTypedTest(Type type, object?[]? parameters, [CallerMemberName] string caller = "")
        {
            Type t = typeof(OptionValueTests_T<>).MakeGenericType(type);
            object o = Activator.CreateInstance(t)!;
            Assert.NotNull(o);
            MethodInfo m = t.GetMethod(caller)!;
            Assert.NotNull(m);
            m.Invoke(o, parameters);
        }

        private class Type_TheoryData : TheoryData<Type>
        {
            public Type_TheoryData()
            {
                Add(typeof(bool));
                Add(typeof(int));
                Add(typeof(VerbosityLevel));
                Add(typeof(string)); // Not in use. Just to have a reference type
            }
        }

        private class TypeAndValue_TheoryData : TheoryData<Type, object>
        {
            public TypeAndValue_TheoryData()
            {
                Type_TheoryData types = new();
                foreach (object[] row in types)
                {
                    Type type = Assert.IsAssignableFrom<Type>(Assert.Single(row));
                    if (type == typeof(bool))
                    {
                        Add(type, false);
                        Add(type, true);
                    }
                    else if (type == typeof(int))
                    {
                        Add(type, 0);
                        Add(type, 1);
                        Add(type, -1);
                    }
                    else if (type == typeof(VerbosityLevel))
                    {
                        Add(type, default(VerbosityLevel));
                        Add(type, VerbosityLevel.Detailed);
                    }
                    else if (type == typeof(string))
                    {
                        Add(type, null!);
                        Add(type, "");
                        Add(type, "abc");
                    }
                    else
                    {
                        Assert.True(false, $"TypeAndValue_TheoryData missing for type {type.Name}.");
                    }

                }
            }
        }

        [Theory]
        [ClassData(typeof(Type_TheoryData))]
        public void Ctor_Implicit(Type type)
        {
            InvokeTypedTest(type, null);
        }

        [Theory]
        [ClassData(typeof(TypeAndValue_TheoryData))]
        public void Ctor_DefaultValue(Type type, object value)
        {
            InvokeTypedTest(type, new object[] { value });
        }

        [Theory]
        [ClassData(typeof(TypeAndValue_TheoryData))]
        public void Value_Roundtrip(Type type, object value)
        {
            InvokeTypedTest(type, new object[] { value });
        }

        [Theory]
        [ClassData(typeof(TypeAndValue_TheoryData))]
        public void ToString_AddsIsSetMarker(Type type, object value)
        {
            InvokeTypedTest(type, new object[] { value });
        }

        [Theory]
        [ClassData(typeof(TypeAndValue_TheoryData))]
        public void ImplicitOperator_T(Type type, object value)
        {
            InvokeTypedTest(type, new object[] { value });
        }

        [Theory]
        [ClassData(typeof(TypeAndValue_TheoryData))]
        public void ImplicitOperator_OptionValueT(Type type, object value)
        {
            InvokeTypedTest(type, new object[] { value });
        }

        [Theory]
        [ClassData(typeof(TypeAndValue_TheoryData))]
        public void AssignStruct_CopiesStruct(Type type, object value)
        {
            InvokeTypedTest(type, new object[] { value });
        }
    }
}
