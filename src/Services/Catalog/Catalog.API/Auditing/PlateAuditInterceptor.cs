using System.Runtime.CompilerServices;
using MassTransit;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Polly;

namespace Catalog.API.Auditing;

/// <summary>
/// Intercepting data, comparing and determining state.
/// </summary>
public class PlateAuditInterceptor : SaveChangesInterceptor
{
    private readonly IBus _bus;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PlateAuditInterceptor> _logger;

    // store pending work items per DbContext instance between SavingChanges and SavedChanges
    private static readonly ConditionalWeakTable<DbContext, List<AuditWorkItem>> Pending = new();

    public PlateAuditInterceptor(IBus bus, ICurrentUserService currentUser, ILogger<PlateAuditInterceptor> logger)
    {
        _bus = bus;
        _currentUser = currentUser;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    // awaitable read only struct
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Flush(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Flush(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context == null) return;

        try
        {
            var userId = _currentUser.GetUserIdOrDefault();
            var now = DateTime.UtcNow;

            var items = new List<AuditWorkItem>();

            // using EF to determine modified state
            var plateEntries = context.ChangeTracker.Entries<Plate>().Where(e => e.State == EntityState.Modified);

            foreach (var entry in plateEntries)
            {
                var changes = new List<AuditFieldChange>();

                // generic heelper that compares original andcurrent values, using x2 delegates
                // used below
                void Compare<T>(string fieldName, Func<Plate, T> current, Func<PropertyValues, T> original)
                {
                    var orig = original(entry.OriginalValues);
                    var curr = current(entry.Entity);

                    if (!Equals(orig, curr))
                    {
                        changes.Add(new AuditFieldChange(
                            fieldName,
                            orig?.ToString(),
                            curr?.ToString()
                        ));
                    }
                }

                // fields mapping comparison - to detecht changes logically
                Compare(nameof(Plate.Status), p => p.Status, ov => ov.GetValue<PlateStatus>(nameof(Plate.Status)));

                Compare(nameof(Plate.PurchasePrice), p => p.PurchasePrice, ov => ov.GetValue<decimal>(nameof(Plate.PurchasePrice)));

                Compare(nameof(Plate.SalePrice), p => p.SalePrice, ov => ov.GetValue<decimal>(nameof(Plate.SalePrice)));

                Compare(nameof(Plate.ReservedDate), p => p.ReservedDate, ov => ov.GetValue<DateTime?>(nameof(Plate.ReservedDate)));

                Compare(nameof(Plate.SoldDate), p => p.SoldDate, ov => ov.GetValue<DateTime?>(nameof(Plate.SoldDate)));

                Compare(nameof(Plate.SoldPrice), p => p.SoldPrice, ov => ov.GetValue<decimal?>(nameof(Plate.SoldPrice)));

                Compare(nameof(Plate.PromoCodeUsed), p => p.PromoCodeUsed, ov => ov.GetValue<string?>(nameof(Plate.PromoCodeUsed)));

                if (changes.Count == 0)
                {
                    continue;
                }
                    
                var statusLabel = Classify(changes);

                // final mapping
                items.Add(new AuditWorkItem(
                    PlateId: entry.Entity.Id,
                    UserId: userId,
                    TimestampUtc: now,
                    Status: statusLabel,
                    Changes: changes
                ));
            }

            if (items.Count > 0)
            {
                Pending.Remove(context);
                Pending.Add(context, items);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture audit changes");
        }
    }

    private void Flush(DbContext? context)
    {
        if (context == null) return;

        if (!Pending.TryGetValue(context, out var items) || items.Count == 0)
            return;

        Pending.Remove(context);

        foreach (var item in items)
        {
            // fire-and-forget,  to never block EF commit
            _ = _bus.Publish(item).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _logger.LogError(
                        t.Exception,
                        "Failed to publish audit event for PlateId={PlateId}",
                        item.PlateId);
                }
            });
        }
    }

    // passing in record, using enum helped a lot here
    private static AuditAction Classify(IReadOnlyList<AuditFieldChange> changes)
    {
        // looking for a status change first
        var statusChange = changes.FirstOrDefault(c => c.FieldName == nameof(Plate.Status));

        if (statusChange != null)
        {
            var newStatus = statusChange.NewValue;

            if (string.Equals(newStatus, PlateStatus.Reserved.ToString(), StringComparison.OrdinalIgnoreCase)) // relies quite a bit on data integrity
            {
                return AuditAction.PlateReserved;
            }

            if (string.Equals(newStatus, PlateStatus.ForSale.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return AuditAction.PlateUnreserved;
            }

            if (string.Equals(newStatus, PlateStatus.Sold.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return AuditAction.PlateSold;
            }

            return AuditAction.PlateUpdated;
        }

        // price change separate from status change
        if (changes.Any(c => c.FieldName == nameof(Plate.SalePrice) ||  c.FieldName == nameof(Plate.PurchasePrice)))
        {
            return AuditAction.PlateUpdated;
        }

        return AuditAction.Unknown;
    }
}
