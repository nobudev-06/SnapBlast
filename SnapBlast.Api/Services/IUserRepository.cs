using SnapBlast.Api.Models;

namespace SnapBlast.Api.Services;

public interface IUserRepository
{
    Task<UserDocument?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<UserDocument?> FindByIdAsync(string id, CancellationToken cancellationToken);
    Task CreateAsync(UserDocument user, CancellationToken cancellationToken);
}