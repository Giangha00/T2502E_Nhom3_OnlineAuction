using System.Text.Json;
using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class CartStore
{
    public const string SessionKey = "BuyNowCart";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static List<CartItemViewModel> GetItems(ISession session)
    {
        var json = session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<CartItemViewModel>>(json, JsonOptions) ?? [];
    }

    public static int GetCount(ISession session) => GetItems(session).Count;

    public static void AddItem(ISession session, CartItemViewModel item)
    {
        var items = GetItems(session);
        if (items.Any(i => i.ProductId == item.ProductId))
        {
            return;
        }

        items.Add(item);
        session.SetString(SessionKey, JsonSerializer.Serialize(items, JsonOptions));
    }
}
