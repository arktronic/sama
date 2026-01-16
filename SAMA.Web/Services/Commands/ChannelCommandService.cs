using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SAMA.Data;
using SAMA.Data.Entities;

namespace SAMA.Web.Services.Commands;

public class ChannelCommandService(SamaDbContext _dbContext, ILogger<ChannelCommandService> _logger)
{
    public virtual async Task<Guid> CreateChannelAsync(
        Guid workspaceId,
        string name,
        string channelType,
        Dictionary<string, JsonElement> configuration,
        bool enabled,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        var channel = new NotificationChannel
        {
            WorkspaceId = workspaceId,
            Name = name,
            ChannelType = channelType,
            ConfigurationJson = configuration,
            Enabled = enabled
        };

        _dbContext.NotificationChannels.Add(channel);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {User} created notification channel {ChannelName} (Type: {ChannelType}, WorkspaceId: {WorkspaceId})",
            performedBy,
            channel.Name,
            channel.ChannelType,
            channel.WorkspaceId);

        return channel.Id;
    }

    public virtual async Task<bool> UpdateChannelAsync(
        Guid channelId,
        string name,
        string channelType,
        Dictionary<string, JsonElement> configuration,
        bool enabled,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        var channel = await _dbContext.NotificationChannels
            .FirstOrDefaultAsync(nc => nc.Id == channelId, cancellationToken);

        if (channel == null)
        {
            return false;
        }

        channel.Name = name;
        channel.ChannelType = channelType;
        channel.ConfigurationJson = configuration;
        channel.Enabled = enabled;
        channel.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {User} updated notification channel {ChannelName} (Id: {ChannelId})",
            performedBy,
            channel.Name,
            channel.Id);

        return true;
    }

    public virtual async Task<bool> DeleteChannelAsync(
        Guid channelId,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        var channel = await _dbContext.NotificationChannels
            .FirstOrDefaultAsync(nc => nc.Id == channelId, cancellationToken);

        if (channel == null)
        {
            return false;
        }

        var channelName = channel.Name;

        _dbContext.NotificationChannels.Remove(channel);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {User} deleted notification channel {ChannelName} (Id: {ChannelId})",
            performedBy,
            channelName,
            channelId);

        return true;
    }
}
