namespace SnapBlast.Models;
public sealed record MultipleChoiceQuestion(string Question, string CorrectAnswer, IReadOnlyList<string> WrongAnswers);