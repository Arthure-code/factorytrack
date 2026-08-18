using FactoryTrack.Contracts;
using FactoryTrack.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace FactoryTrack.Api.Hubs;

/// <summary>
/// Le hub diffuse, il ne calcule pas. Toute logique metier appartient au domaine.
/// </summary>
public class PositionHub : Hub
{
    private readonly ILogger<PositionHub> _journal;

    public PositionHub(ILogger<PositionHub> journal) => _journal = journal;

    /// <summary>Le client ne recoit que l'etage qu'il regarde, pas toute l'usine.</summary>
    public async Task RejoindreEtage(int etage) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, NomsHub.Groupes.Etage(etage));

    public async Task QuitterEtage(int etage) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, NomsHub.Groupes.Etage(etage));

    /// <summary>
    /// Appele par le service d'ingestion, pas par les clients finaux.
    /// A proteger par une politique d'autorisation des l'ajout de JWT (V2).
    /// </summary>
    public async Task DiffuserPosition(PositionDto position) =>
        await Clients.Group(NomsHub.Groupes.Etage(position.Etage))
                     .SendAsync(NomsHub.Methodes.PositionMiseAJour, position);

    public override Task OnConnectedAsync()
    {
        _journal.LogDebug("Client connecte : {Id}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }
}
