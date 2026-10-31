namespace QuizServer;

public sealed record Quiz(
    int Id,//номер теста
    string Topic,// тема теста
    int QuestionsCount,//кол-во вопрсоов
    string Status);//статус

public sealed record QuizListResult(
    IReadOnlyList<Quiz> Items,
    double? AverageQuestionsCount);