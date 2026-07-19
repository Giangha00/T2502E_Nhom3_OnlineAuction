using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

/// <summary>
/// Shared seller/buyer order notification helpers used across checkout and expiry flows.
/// </summary>
public static class OrderNotificationHelper
{
    public static async Task NotifySellerAwaitingPaymentAsync(
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        AuctionHouseDbContext dbContext,
        AuctionOrder order,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveSellerContextAsync(dbContext, order, cancellationToken);
        if (context is null)
        {
            return;
        }

        await notificationService.CreateAndPushAsync(
            context.SellerId,
            notifyLocalizer[NotificationKeys.SellerAwaitingPaymentTitle],
            notifyLocalizer.Format(NotificationKeys.SellerAwaitingPaymentMessage, context.ProductName, context.OrderReference),
            NotificationType.Auction,
            "/Sell/MyAuctions",
            NotificationReferenceTypes.SellerAwaitingPayment,
            order.Id,
            cancellationToken: cancellationToken);
    }

    public static async Task NotifySellerPaymentReceivedAsync(
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        AuctionHouseDbContext dbContext,
        int orderId,
        string paymentMethodLabel,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == orderId && item.DeletedAt == null, cancellationToken);

        if (order is null)
        {
            return;
        }

        var context = await ResolveSellerContextAsync(dbContext, order, cancellationToken);
        if (context is null)
        {
            return;
        }

        await notificationService.CreateAndPushAsync(
            context.SellerId,
            notifyLocalizer[NotificationKeys.SellerPaymentReceivedTitle],
            notifyLocalizer.Format(NotificationKeys.SellerPaymentReceivedMessage, context.ProductName, context.OrderReference, paymentMethodLabel),
            NotificationType.Payment,
            "/Sell/MyAuctions",
            NotificationReferenceTypes.SellerPaymentReceived,
            order.Id,
            cancellationToken: cancellationToken);
    }

    public static async Task NotifyPaymentOverdueCancelledAsync(
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        AuctionHouseDbContext dbContext,
        AuctionOrder order,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveSellerContextAsync(dbContext, order, cancellationToken);
        var productName = context?.ProductName
            ?? order.Items.FirstOrDefault()?.ItemName
            ?? "your item";
        var orderReference = order.OrderReference;

        await notificationService.CreateAndPushAsync(
            order.BuyerId,
            notifyLocalizer[NotificationKeys.OrderCancelledTitle],
            notifyLocalizer.Format(NotificationKeys.OrderCancelledBuyerMessage, orderReference, productName),
            NotificationType.Payment,
            "/Order",
            NotificationReferenceTypes.OrderCancelledPaymentOverdue,
            order.Id,
            cancellationToken: cancellationToken);

        if (context is null)
        {
            return;
        }

        await notificationService.CreateAndPushAsync(
            context.SellerId,
            notifyLocalizer[NotificationKeys.OrderCancelledTitle],
            notifyLocalizer.Format(NotificationKeys.OrderCancelledSellerMessage, orderReference, productName),
            NotificationType.Auction,
            "/Sell/MyAuctions",
            NotificationReferenceTypes.OrderCancelledPaymentOverdue,
            order.Id,
            cancellationToken: cancellationToken);
    }

    private static async Task<SellerOrderContext?> ResolveSellerContextAsync(
        AuctionHouseDbContext dbContext,
        AuctionOrder order,
        CancellationToken cancellationToken)
    {
        var firstItem = order.Items.FirstOrDefault();
        if (firstItem is null)
        {
            return null;
        }

        var seller = await dbContext.Auctions
            .AsNoTracking()
            .Where(auction => auction.Id == firstItem.AuctionId)
            .Select(auction => new
            {
                SellerId = auction.Product.SellerId,
                ProductName = auction.Product.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (seller is null || seller.SellerId <= 0)
        {
            return null;
        }

        return new SellerOrderContext(
            seller.SellerId,
            string.IsNullOrWhiteSpace(seller.ProductName) ? firstItem.ItemName : seller.ProductName,
            order.OrderReference);
    }

    private sealed record SellerOrderContext(int SellerId, string ProductName, string OrderReference);
}
