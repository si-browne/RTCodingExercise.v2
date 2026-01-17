using System.Linq.Expressions;
using System.Reflection;
using Catalog.API.Auditing;
using Catalog.API.Data;
using Catalog.API.Services;
using Catalog.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuditUnitTests;
public class AuditTests
{
    // ==========================
    // PlateAuditInterceptor tests
    // ==========================
    public class PlateAuditInterceptorTests
    {
        [Fact]
        public void Capture_WhenContextNull_DoesNothing()
        {
            var bus = new Mock<IBus>(MockBehavior.Strict);
            var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
            var logger = new Mock<ILogger<PlateAuditInterceptor>>(MockBehavior.Loose);

            var interceptor = new PlateAuditInterceptor(bus.Object, currentUser.Object, logger.Object);

            interceptor.InvokePrivate("Capture", new object?[] { null });
        }

        [Fact]
        public void Flush_WhenContextNull_DoesNothing()
        {
            var bus = new Mock<IBus>(MockBehavior.Strict);
            var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
            var logger = new Mock<ILogger<PlateAuditInterceptor>>(MockBehavior.Loose);

            var interceptor = new PlateAuditInterceptor(bus.Object, currentUser.Object, logger.Object);

            interceptor.InvokePrivate("Flush", new object?[] { null });
        }

        [Fact]
        public void Capture_WhenCurrentUserThrows_LogsError_AndDoesNotPublish()
        {
            var bus = new Mock<IBus>(MockBehavior.Strict);
            bus.Setup(b => b.Publish(It.IsAny<AuditWorkItem>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
            currentUser.Setup(c => c.GetUserIdOrDefault()).Throws(new InvalidOperationException("boom"));

            var logger = new Mock<ILogger<PlateAuditInterceptor>>(MockBehavior.Loose);

            var interceptor = new PlateAuditInterceptor(bus.Object, currentUser.Object, logger.Object);

            // IMPORTANT: do NOT touch ChangeTracker/Attach/Entry without a provider.
            // This test still exercises the catch/log path because the exception happens first.
            var ctx = new DummyDbContext();

            interceptor.InvokePrivate("Capture", new object?[] { ctx });

            logger.VerifyLog(LogLevel.Error, Times.AtLeastOnce(), "Failed to capture audit changes");
            bus.Verify(b => b.Publish(It.IsAny<AuditWorkItem>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void Classify_WhenStatusToReserved_ReturnsPlateReserved()
        {
            var changes = new List<AuditFieldChange>
        {
            new AuditFieldChange(nameof(Plate.Status), PlateStatus.ForSale.ToString(), PlateStatus.Reserved.ToString())
        };

            var result = InvokeClassify(changes);
            Assert.Equal(AuditAction.PlateReserved, result);
        }

        [Fact]
        public void Classify_WhenStatusToForSale_ReturnsPlateUnreserved()
        {
            var changes = new List<AuditFieldChange>
        {
            new AuditFieldChange(nameof(Plate.Status), PlateStatus.Reserved.ToString(), PlateStatus.ForSale.ToString())
        };

            var result = InvokeClassify(changes);
            Assert.Equal(AuditAction.PlateUnreserved, result);
        }

        [Fact]
        public void Classify_WhenStatusToSold_ReturnsPlateSold()
        {
            var changes = new List<AuditFieldChange>
        {
            new AuditFieldChange(nameof(Plate.Status), PlateStatus.ForSale.ToString(), PlateStatus.Sold.ToString())
        };

            var result = InvokeClassify(changes);
            Assert.Equal(AuditAction.PlateSold, result);
        }

        [Fact]
        public void Classify_WhenPriceChangedWithoutStatus_ReturnsPlateUpdated()
        {
            var changes = new List<AuditFieldChange>
        {
            new AuditFieldChange(nameof(Plate.SalePrice), "100", "120")
        };

            var result = InvokeClassify(changes);
            Assert.Equal(AuditAction.PlateUpdated, result);
        }

        [Fact]
        public void Classify_WhenNonPriceNonStatusChange_ReturnsUnknown()
        {
            var changes = new List<AuditFieldChange>
        {
            new AuditFieldChange(nameof(Plate.PromoCodeUsed), null, "PROMO10")
        };

            var result = InvokeClassify(changes);
            Assert.Equal(AuditAction.Unknown, result);
        }

        // -----------------
        // helpers (no EF provider required)
        // -----------------

        // A DbContext instance we can use as a key in ConditionalWeakTable.
        // MUST NOT be used for tracking operations without a provider.
        private sealed class DummyDbContext : DbContext
        {
            public DummyDbContext() : base(new DbContextOptionsBuilder<DummyDbContext>().Options) { }
        }

        private static AuditAction InvokeClassify(IReadOnlyList<AuditFieldChange> changes)
        {
            var mi = typeof(PlateAuditInterceptor).GetMethod(
                "Classify",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (mi == null) throw new InvalidOperationException("Could not find PlateAuditInterceptor.Classify");

            return (AuditAction)mi.Invoke(null, new object?[] { changes })!;
        }


    }

  

    // ===========================
    // AuditWorkItemConsumer tests
    // ===========================
    public class AuditWorkItemConsumerTests
    {

        [Fact]
        public async Task Consume_WhenScopeFactoryThrows_LogsError_AndRethrows()
        {
            var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
            scopeFactory.Setup(sf => sf.CreateScope()).Throws(new InvalidOperationException("scope fail"));

            var logger = new Mock<ILogger<AuditWorkItemConsumer>>(MockBehavior.Loose);

            var consumer = new AuditWorkItemConsumer(scopeFactory.Object, logger.Object);

            var msg = new AuditWorkItem(
                PlateId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                TimestampUtc: DateTime.UtcNow,
                Status: AuditAction.Unknown,
                Changes: new List<AuditFieldChange>());

            var ctx = new Mock<ConsumeContext<AuditWorkItem>>(MockBehavior.Strict);
            ctx.SetupGet(c => c.Message).Returns(msg);
            ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(ctx.Object));

            logger.VerifyLog(LogLevel.Error, Times.AtLeastOnce(), "Failed to persist audit event");
        }

        /// <summary>
        /// Test double that can be returned as ApplicationDbContext, but gives us overridable behavior.
        /// IMPORTANT: No provider is configured or used; we only intercept Add + SaveChangesAsync.
        /// </summary>
        private sealed class TestApplicationDbContext : ApplicationDbContext
        {
            private readonly DbSet<AuditLogEvent> _auditLogEvents;

            public TestApplicationDbContext(DbSet<AuditLogEvent> auditLogEvents)
                : base(new DbContextOptionsBuilder<ApplicationDbContext>().Options)
            {
                _auditLogEvents = auditLogEvents;
            }

            // Hide the non-virtual property with a new virtual one we control.
            // Consumer accesses via compile-time type ApplicationDbContext, but it calls the runtime member.
            //public new virtual DbSet<AuditLogEvent> AuditLogEvents => _auditLogEvents;

            public int SaveChangesAsyncCalls { get; private set; }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveChangesAsyncCalls++;
                return Task.FromResult(1);
            }
        }
    }
}

// =====================
// Logger verify helper
// =====================
public static class MoqLoggerExtensions
{
    public static void VerifyLog<T>(
        this Mock<ILogger<T>> logger,
        LogLevel level,
        Times times,
        string? messageContains = null)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    messageContains == null || (v.ToString() ?? "").Contains(messageContains, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}

// =====================
// Reflection helper
// =====================
public static class ReflectionTestExtensions
{
    public static object? InvokePrivate(this object instance, string methodName, object?[] args)
    {
        var mi = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (mi == null) throw new InvalidOperationException($"Method '{methodName}' not found on {instance.GetType().FullName}");
        return mi.Invoke(instance, args);
    }

    public static void TrySetPropertyOrBackingField(this object obj, string propertyName, object? value)
    {
        var type = obj.GetType();

        var prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop?.SetMethod != null)
        {
            try { prop.SetValue(obj, value); return; } catch { /* ignore */ }
        }

        var field = type.GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            try { field.SetValue(obj, value); } catch { /* ignore */ }
        }
    }
}

// =====================================
// Async IQueryable test infrastructure
// (enables EF Core ToListAsync over LINQ-to-Objects)
// =====================================
internal sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

    public object Execute(Expression expression) => _inner.Execute(expression)!;
    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression)!;

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        => Execute<TResult>(expression);

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        => new TestAsyncEnumerable<TResult>(expression);
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
}
