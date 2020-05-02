using System;
using System.Threading.Tasks;
using Xbehave;
using Xbehave.Sdk;
using Xunit;

namespace Inspiring.Json.Tests {
    public abstract class Feature {
        protected Keyword GIVEN { get; } = new Keyword("GIVEN");
        protected Keyword WHEN { get; } = new Keyword("WHEN");
        protected Keyword THEN { get; } = new Keyword("THEN");
        protected Keyword AND { get; } = new Keyword("AND");
        protected Keyword ON { get; } = new Keyword("ON");
        protected Keyword CUSTOM { get; } = new Keyword(null);
        protected UsingKeyword USING { get; } = new UsingKeyword();

        protected AsyncKeyword Given { get; } = new AsyncKeyword("GIVEN");
        protected AsyncKeyword When { get; } = new AsyncKeyword("WHEN");
        protected AsyncKeyword Then { get; } = new AsyncKeyword("THEN");
        protected AsyncKeyword And { get; } = new AsyncKeyword("AND");
        protected AsyncKeyword On { get; } = new AsyncKeyword("ON");
        protected AsyncKeyword Custom { get; } = new AsyncKeyword(null);

        protected class Keyword {
            private readonly string _prefix;

            public Keyword(string prefix) => _prefix = prefix;

            public Action this[string text] {
                set { DefineStep(_prefix, text, body: value); }
            }

            public Action this[string text, ExceptionExpectation ex] {
                set => this[ex.FormatStepName(text)] = ex.Wrap(value);
            }
        }

        protected class AsyncKeyword {
            private readonly string _prefix;

            public AsyncKeyword(string prefix) => _prefix = prefix;

            public Func<Task> this[string text] {
                set { DefineStep(_prefix, text, body: value); }
            }

            public Func<Task> this[string text, ExceptionExpectation ex] {
                set => this[ex.FormatStepName(text)] = ex.Wrap(value);
            }
        }

        public class UsingKeyword {
            public Func<IDisposable> this[string text] {
                set {
                    GetStepName("USING", text).x(c => value().Using(c));
                }
            }
        }

        protected ExceptionExpectation ThrowsA<T>() where T : Exception {
            return new ExceptionExpectation<T>("{0} throws an exception");
        }

        protected ExceptionExpectation ThenIsThrown<T>() where T : Exception {
            return new ExceptionExpectation<T>("{0} THEN an exception is thrown");
        }

        protected abstract class ExceptionExpectation {
            public abstract Action Wrap(Action body);

            public abstract Func<Task> Wrap(Func<Task> body);

            public abstract string FormatStepName(string originalName);
        }

        private class ExceptionExpectation<T> : ExceptionExpectation where T : Exception {
            private readonly string _textTemplate;

            public ExceptionExpectation(string textTemplate)
                => _textTemplate = textTemplate;

            public override string FormatStepName(string originalName) {
                return String.Format(_textTemplate, originalName);
            }

            public override Action Wrap(Action body) {
                return () => Assert.Throws<T>(body);
            }

            public override Func<Task> Wrap(Func<Task> body) {
                return () => Assert.ThrowsAsync<T>(body);
            }
        }

        private static IStepBuilder DefineStep(string prefix, string text, Action body) {
            return GetStepName(prefix, text).x(body);
        }

        private static IStepBuilder DefineStep(string prefix, string text, Func<Task> body) {
            return GetStepName(prefix, text).x(body);
        }

        private static string GetStepName(string prefix, string text) {
            return prefix != null ?
                $"{prefix} {text}" :
                text;
        }
    }
}
