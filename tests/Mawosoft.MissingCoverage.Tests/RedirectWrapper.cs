// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mawosoft.MissingCoverage.Tests
{
    internal class RedirectWrapper
    {
        private readonly SyncLineWriter _out;
        private readonly SyncLineWriter _error;
        private readonly List<string> _lines;

        public TextWriter Out => _out;
        public TextWriter Error => _error;
        public Program Program { get; }

        public List<string> Lines
        {
            get
            {
                if (_out.IsOpen || _error.IsOpen)
                {
                    throw new InvalidOperationException(
                        nameof(RedirectWrapper) + " must be closed before accessing " + nameof(Lines)
                        + " property. Use " + nameof(CloneLines) + " to get a synced copy.");
                }
                return _lines;
            }
        }

        public List<string> CloneLines()
        {
            lock ((_lines as ICollection).SyncRoot)
            {
                return new List<string>(_lines);
            }
        }

        public RedirectWrapper() : this(new Program()) { }

        public RedirectWrapper(Program program)
        {
            _lines = new();
            _out = new(_lines, "1>");
            _error = new(_lines, "2>");
            program.Out = _out;
            program.Error = _error;
            Program = program;
        }

        public void Close()
        {
            _out.Close();
            _error.Close();
        }

        private class SyncLineWriter : TextWriter
        {
            private readonly List<string> _target;
            private readonly string _prefix;
            private volatile bool _isopen;

            public bool IsOpen => _isopen;

            public SyncLineWriter(List<string> target, string prefix)
            {
                _target = target;
                _prefix = prefix;
                _isopen = true;
            }

            public override Encoding Encoding => Encoding.Unicode;

            // Ensure only WriteLine() is used
            public override void Write(char value) => throw new NotImplementedException();

            public override void WriteLine(string? value)
            {
                IEnumerable<string> lines =
                    (value ?? string.Empty).Split(Environment.NewLine).Select(s => _prefix + s);
                lock ((_target as ICollection).SyncRoot)
                {
                    if (!_isopen)
                    {
                        throw new ObjectDisposedException(nameof(SyncLineWriter));
                    }
                    _target.AddRange(lines);
                }
            }

            protected override void Dispose(bool disposing)
            {
                lock ((_target as ICollection).SyncRoot)
                {
                    _isopen = false;
                }
                base.Dispose(disposing);
            }
        }
    }
}
