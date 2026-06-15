using System.Text.Json;
using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class WonOrderStore
{
    public const string SessionKey = "WonOrders";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static List<WonOrderItem> GetOrders(ISession session)
    {
        var json = session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<WonOrderItem>>(json, JsonOptions) ?? [];
    }

    public static int GetCount(ISession session) => GetOrders(session).Count;

    public static void AddOrder(ISession session, WonOrderItem item)
    {
        var orders = GetOrders(session);
        var existing = orders.FirstOrDefault(o => o.AuctionId == item.AuctionId);
        if (existing is not null)
        {
            existing.WinningBid = item.WinningBid;
            existing.PaymentDeadline = item.PaymentDeadline;
            existing.OrderReference = item.OrderReference;
        }
        else
        {
            orders.Add(item);
        }

        SaveOrders(session, orders);
    }

    public static void Clear(ISession session) => session.Remove(SessionKey);

    private static void SaveOrders(ISession session, List<WonOrderItem> orders)
    {
        session.SetString(SessionKey, JsonSerializer.Serialize(orders, JsonOptions));
    }
}
