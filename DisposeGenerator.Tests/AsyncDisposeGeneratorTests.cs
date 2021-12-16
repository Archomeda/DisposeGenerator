using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace DisposeGenerator.Tests
{
    public partial class AsyncDisposableWithMethods : IDisposable, IAsyncDisposable
    {
        public Action? OnDispose { get; set; }

        public Action? OnFinalize { get; set; }

        public Func<Task>? OnAsyncDispose { get; set; }

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

        [AsyncDisposer]
        private async ValueTask AdditionalAsyncDispose()
        {
            if (this.OnAsyncDispose is not null)
                await this.OnAsyncDispose().ConfigureAwait(false);
        }
    }

    [DisposeAll]
    public partial class AsyncDisposableWithMembers : IDisposable, IAsyncDisposable
    {
        public AsyncDisposableWithMethods? DisposableField;
    }

    [DisposeAll]
    public partial class AsyncDisposableWithDisposables : IDisposable, IAsyncDisposable
    {
        public IAsyncDisposable? DisposableField;
    }

    [DisposeAll]
    public partial class AsyncDisposableWithExcludes : IDisposable, IAsyncDisposable
    {
        public DisposableWithMethods? DisposableFieldIncluded = new();

        [ExcludeDispose]
        public DisposableWithMethods? DisposableFieldExcluded = new();
    }

    public partial class AsyncDisposableWithIncludes : IDisposable, IAsyncDisposable
    {
        [IncludeDispose]
        public DisposableWithMethods? DisposableFieldIncluded = new();

        public DisposableWithMethods? DisposableFieldExcluded = new();
    }

    public partial class AsyncDisposableWithProperties : IDisposable, IAsyncDisposable
    {
        [IncludeDispose]
        public DisposableWithMethods DisposablePropertyIncluded { get; set; } = new();

        [IncludeDispose]
        public DisposableWithMethods DisposablePropertyIncludedWithoutSetter { get; } = new();

        public DisposableWithMethods DisposablePropertyExcluded { get; set; } = new();
    }


    public class AsyncDisposeGeneratorTests
    {
        [Fact]
        public void DisposeTest()
        {
            bool disposed = false;
            bool asyncDisposed = false;
            bool finalized = false;
            var disposable = new AsyncDisposableWithMethods
            {
                OnDispose = () => disposed = true,
                OnAsyncDispose = async () => asyncDisposed = true,
                OnFinalize = () => finalized = true
            };
            disposable.Dispose();

            using (new AssertionScope())
            {
                disposed.Should().BeTrue();
                asyncDisposed.Should().BeFalse();
                finalized.Should().BeTrue();
            }
        }

        [Fact]
        public void DisposeOnlyOnceTest()
        {
            int disposed = 0;
            var disposable = new AsyncDisposableWithMethods
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
            bool cascadeAsyncDisposed = false;
            var disposable = new AsyncDisposableWithMembers
            {
                DisposableField = new AsyncDisposableWithMethods
                {
                    OnDispose = () => cascadeDisposed = true,
                    OnAsyncDispose = async () => cascadeAsyncDisposed = true
                }
            };
            disposable.Dispose();

            using (new AssertionScope())
            {
                cascadeDisposed.Should().BeTrue();
                cascadeAsyncDisposed.Should().BeFalse();
                disposable.DisposableField.Should().BeNull();
            }
        }

        [Fact]
        public void DisposeDisposablesTest()
        {
            bool disposed = false;
            bool asyncDisposed = false;
            var disposable = new AsyncDisposableWithDisposables
            {
                DisposableField = new AsyncDisposableWithMethods
                {
                    OnDispose = () => disposed = true,
                    OnAsyncDispose = async () => asyncDisposed = true
                }
            };
            disposable.Dispose();

            using (new AssertionScope())
            {
                disposed.Should().BeTrue();
                asyncDisposed.Should().BeFalse();
                disposable.DisposableField.Should().BeNull();
            }
        }

        [Fact]
        public void ExcludeDisposeTest()
        {
            // We just check for nulls here, the cascade test will check if included members are properly disposed of
            var disposable = new AsyncDisposableWithExcludes();
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
            var disposable = new AsyncDisposableWithIncludes();
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
            var disposable = new AsyncDisposableWithProperties();
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
            bool asyncDisposed = false;
            bool finalized = false;
            // We throw this in a separate task; as of 2021-07-26 in .NET 5, this seems to make the garbage collector call the finalizer
            Task.Run(() =>
            {
                var disposable = new AsyncDisposableWithMethods
                {
                    OnDispose = () => disposed = true,
                    OnAsyncDispose = async () => asyncDisposed = true,
                    OnFinalize = () => finalized = true
                };
            }).Wait();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            using (new AssertionScope())
            {
                disposed.Should().BeFalse();
                asyncDisposed.Should().BeFalse();
                finalized.Should().BeTrue();
            }
        }


        [Fact]
        public async Task AsyncDisposeTest()
        {
            bool disposed = false;
            bool asyncDisposed = false;
            bool finalized = false;
            var disposable = new AsyncDisposableWithMethods
            {
                OnDispose = () => disposed = true,
                OnAsyncDispose = async () => asyncDisposed = true,
                OnFinalize = () => finalized = true
            };
            await disposable.DisposeAsync();

            using (new AssertionScope())
            {
                disposed.Should().BeTrue();
                asyncDisposed.Should().BeTrue();
                finalized.Should().BeTrue();
            }
        }

        [Fact]
        public async Task CascadeAsyncDisposeTest()
        {
            bool cascadeDisposed = false;
            bool cascadeAsyncDisposed = false;
            var disposable = new AsyncDisposableWithMembers
            {
                DisposableField = new AsyncDisposableWithMethods
                {
                    OnDispose = () => cascadeDisposed = true,
                    OnAsyncDispose = async () => cascadeAsyncDisposed = true
                }
            };
            await disposable.DisposeAsync();

            using (new AssertionScope())
            {
                cascadeDisposed.Should().BeTrue();
                cascadeAsyncDisposed.Should().BeTrue();
                disposable.DisposableField.Should().BeNull();
            }
        }

        [Fact]
        public async Task ExcludeAsyncDisposeTest()
        {
            // We just check for nulls here, the cascade test will check if included members are properly disposed of
            var disposable = new AsyncDisposableWithExcludes();
            await disposable.DisposeAsync();

            using (new AssertionScope())
            {
                disposable.DisposableFieldIncluded.Should().BeNull();
                disposable.DisposableFieldExcluded.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task IncludeAsyncDisposeTest()
        {
            // We just check for nulls here, the cascade test will check if included members are properly disposed of
            var disposable = new AsyncDisposableWithIncludes();
            await disposable.DisposeAsync();

            using (new AssertionScope())
            {
                disposable.DisposableFieldIncluded.Should().BeNull();
                disposable.DisposableFieldExcluded.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task PropertyAsyncDisposeTest()
        {
            bool disposed = false;

            // We just check for nulls here, the cascade test will check if included members are properly disposed of
            var disposable = new AsyncDisposableWithProperties();
            disposable.DisposablePropertyIncludedWithoutSetter.OnDispose = () => disposed = true;
            await disposable.DisposeAsync();

            using (new AssertionScope())
            {
                disposable.DisposablePropertyIncluded.Should().BeNull();
                disposable.DisposablePropertyExcluded.Should().NotBeNull();
                disposed.Should().BeTrue();
            }
        }
    }
}
