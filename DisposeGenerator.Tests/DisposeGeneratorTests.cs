using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace DisposeGenerator.Tests
{
    public partial class DisposableWithMethods : IDisposable
    {
        public Action? OnDispose { get; set; }

        public Action? OnFinalize { get; set; }

        [Disposer]
        private void AdditionalDispose()
        {
            this.OnDispose?.Invoke();
        }

        [Finalizer]
        private void AdditionalFinalize()
        {
            this.OnFinalize?.Invoke();
        }
    }

    [DisposeAll]
    public partial class DisposableWithMembers : IDisposable
    {
        public DisposableWithMethods? DisposableField;
    }

    [DisposeAll]
    public partial class DisposableWithDisposables : IDisposable
    {
        public IDisposable? DisposableField;
    }

    [DisposeAll]
    public partial class DisposableWithExcludes : IDisposable
    {
        public DisposableWithMethods? DisposableFieldIncluded = new();

        [ExcludeDispose]
        public DisposableWithMethods? DisposableFieldExcluded = new();
    }

    public partial class DisposableWithIncludes : IDisposable
    {
        [IncludeDispose]
        public DisposableWithMethods? DisposableFieldIncluded = new();

        public DisposableWithMethods? DisposableFieldExcluded = new();
    }

    public partial class DisposableWithProperties : IDisposable
    {
        [IncludeDispose]
        public DisposableWithMethods DisposablePropertyIncluded { get; set; } = new();

        [IncludeDispose]
        public DisposableWithMethods DisposablePropertyIncludedWithoutSetter { get; } = new();

        public DisposableWithMethods DisposablePropertyExcluded { get; set; } = new();
    }

    public class DisposeGeneratorTests
    {
        [Fact]
        public void DisposeTest()
        {
            bool disposed = false;
            bool finalized = false;
            var disposable = new DisposableWithMethods
            {
                OnDispose = () => disposed = true,
                OnFinalize = () => finalized = true
            };
            disposable.Dispose();

            using (new AssertionScope())
            {
                disposed.Should().BeTrue();
                finalized.Should().BeTrue();
            }
        }

        [Fact]
        public void DisposeOnlyOnceTest()
        {
            int disposed = 0;
            var disposable = new DisposableWithMethods
            {
                OnDispose = () => disposed++
            };
            disposable.Dispose();
            disposable.Dispose();

            disposed.Should().Be(1);
        }

        [Fact]
        public void CascadeDisposeTest()
        {
            bool cascadeDisposed = false;
            var disposable = new DisposableWithMembers
            {
                DisposableField = new DisposableWithMethods
                {
                    OnDispose = () => cascadeDisposed = true
                }
            };
            disposable.Dispose();

            using (new AssertionScope())
            {
                cascadeDisposed.Should().BeTrue();
                disposable.DisposableField.Should().BeNull();
            }
        }

        [Fact]
        public void DisposeDisposablesTest()
        {
            bool disposed = false;
            var disposable = new DisposableWithDisposables
            {
                DisposableField = new DisposableWithMethods
                {
                    OnDispose = () => disposed = true
                }
            };
            disposable.Dispose();

            using (new AssertionScope())
            {
                disposed.Should().BeTrue();
                disposable.DisposableField.Should().BeNull();
            }
        }

        [Fact]
        public void ExcludeDisposeTest()
        {
            // We just check for nulls here, the cascade test will check if included members are properly disposed of
            var disposable = new DisposableWithExcludes();
            disposable.Dispose();

            using (new AssertionScope())
            {
                disposable.DisposableFieldIncluded.Should().BeNull();
                disposable.DisposableFieldExcluded.Should().NotBeNull();
            }
        }

        [Fact]
        public void IncludeDisposeTest()
        {
            // We just check for nulls here, the cascade test will check if included members are properly disposed of
            var disposable = new DisposableWithIncludes();
            disposable.Dispose();

            using (new AssertionScope())
            {
                disposable.DisposableFieldIncluded.Should().BeNull();
                disposable.DisposableFieldExcluded.Should().NotBeNull();
            }
        }

        [Fact]
        public void PropertyDisposeTest()
        {
            bool disposed = false;

            // We just check for nulls here, the cascade test will check if included members are properly disposed of
            var disposable = new DisposableWithProperties();
            disposable.DisposablePropertyIncludedWithoutSetter.OnDispose = () => disposed = true;
            disposable.Dispose();

            using (new AssertionScope())
            {
                disposable.DisposablePropertyIncluded.Should().BeNull();
                disposable.DisposablePropertyExcluded.Should().NotBeNull();
                disposed.Should().BeTrue();
            }
        }


        [Fact]
        public void FinalizeTest()
        {
            bool disposed = false;
            bool finalized = false;
            // We throw this in a separate task; as of 2021-07-26 in .NET 5, this seems to make the garbage collector call the finalizer
            Task.Run(() =>
            {
                var disposable = new DisposableWithMethods
                {
                    OnDispose = () => disposed = true,
                    OnFinalize = () => finalized = true
                };
            }).Wait();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            using (new AssertionScope())
            {
                disposed.Should().BeFalse();
                finalized.Should().BeTrue();
            }
        }
    }
}
