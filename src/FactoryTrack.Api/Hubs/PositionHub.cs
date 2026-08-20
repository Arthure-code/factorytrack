using FactoryTrack.Contracts;
using FactoryTrack.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace FactoryTrack.Api.Hubs;

public class PositionHub : Hub
{
    private readonly ILogger<PositionHub> _journal;

    public PositionHub(ILogger<PositionHub> journal) => _journal = journal;

    public async Task RejoindreEtage(int etage) =>
    await Groups.AddToGroupAsync(Context.ConnectionId, NomsHub.Groupes.Etage(etage));

    public async Task QuitterEtage(int etage) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, NomsHub.Groupes.Etage(etage));

    public async Task DiffuserPosition(PositionDto position) =>
    await Clients.Group(NomsHub.Groupes.Etage(position.Etage))
                 .SendAsync(NomsHub.Methodes.PositionMiseAJour, position);

    public override Task OnConnectedAsync()
    {
        _journal.LogDebug("Client connecte : {Id}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }
}
