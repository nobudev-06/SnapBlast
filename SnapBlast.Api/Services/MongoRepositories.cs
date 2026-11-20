using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SnapBlast.Api.Models;

namespace SnapBlast.Api.Services;

public sealed class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<UserDocument> users;

    public MongoUserRepository(IMongoClient client, IOptions<MongoOptions> options)
    {
        var database = client.GetDatabase(options.Value.Database);
        users = database.GetCollection<UserDocument>(options.Value.UserCollection);
        users.Indexes.CreateOne(new CreateIndexModel<UserDocument>(Builders<UserDocument>.IndexKeys.Ascending(user => user.Email), new CreateIndexOptions { Unique = true }));
    }

    public async Task<UserDocument?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        await users.Find(user => user.Email == email).FirstOrDefaultAsync(cancellationToken);

    public async Task<UserDocument?> FindByIdAsync(string id, CancellationToken cancellationToken) =>
        await users.Find(user => user.Id == id).FirstOrDefaultAsync(cancellationToken);

    public Task CreateAsync(UserDocument user, CancellationToken cancellationToken) => users.InsertOneAsync(user, cancellationToken: cancellationToken);
}

public sealed class MongoQuizRepository : IQuizRepository
{
    private readonly IMongoCollection<QuizDocument> quizzes;

    public MongoQuizRepository(IMongoClient client, IOptions<MongoOptions> options)
    {
        var database = client.GetDatabase(options.Value.Database);
        quizzes = database.GetCollection<QuizDocument>(options.Value.QuizCollection);
        quizzes.Indexes.CreateOne(new CreateIndexModel<QuizDocument>(Builders<QuizDocument>.IndexKeys.Ascending(quiz => quiz.OwnerId)));
    }

    public async Task<IReadOnlyList<QuizDocument>> ListAsync(string ownerId, CancellationToken cancellationToken) =>
        await quizzes.Find(quiz => quiz.OwnerId == ownerId).SortByDescending(quiz => quiz.UpdatedAt).ToListAsync(cancellationToken);

    public async Task<QuizDocument?> GetAsync(string ownerId, string id, CancellationToken cancellationToken) =>
        await quizzes.Find(quiz => quiz.OwnerId == ownerId && quiz.Id == id).FirstOrDefaultAsync(cancellationToken);

    public Task CreateAsync(QuizDocument quiz, CancellationToken cancellationToken) => quizzes.InsertOneAsync(quiz, cancellationToken: cancellationToken);

    public async Task<bool> UpdateAsync(QuizDocument quiz, CancellationToken cancellationToken) =>
        (await quizzes.ReplaceOneAsync(item => item.OwnerId == quiz.OwnerId && item.Id == quiz.Id, quiz, cancellationToken: cancellationToken)).ModifiedCount == 1;

    public async Task<bool> DeleteAsync(string ownerId, string id, CancellationToken cancellationToken) =>
        (await quizzes.DeleteOneAsync(quiz => quiz.OwnerId == ownerId && quiz.Id == id, cancellationToken)).DeletedCount == 1;
}