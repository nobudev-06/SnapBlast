using SnapBlast.Api.Models;

namespace SnapBlast.Api.Services;

public interface IQuizRepository
{
    Task<IReadOnlyList<QuizDocument>> ListAsync(string ownerId, CancellationToken cancellationToken);
    Task<QuizDocument?> GetAsync(string ownerId, string id, CancellationToken cancellationToken);
    Task CreateAsync(QuizDocument quiz, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(QuizDocument quiz, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string ownerId, string id, CancellationToken cancellationToken);
}