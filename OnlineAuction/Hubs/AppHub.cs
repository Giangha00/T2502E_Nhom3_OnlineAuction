using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace OnlineAuction.Hubs;

public class AppHub : Hub
{
    public static string UserGroup(int userId) => $"user:{userId}";

    public static string AuctionGroup(int auctionId) => $"auction:{auctionId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userId, out var id))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(id));
        }

        await base.OnConnectedAsync();
    }

    public Task JoinAuction(int auctionId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, AuctionGroup(auctionId));

    public Task LeaveAuction(int auctionId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, AuctionGroup(auctionId));
}
