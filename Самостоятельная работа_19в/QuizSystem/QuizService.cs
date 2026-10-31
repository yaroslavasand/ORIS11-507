namespace QuizServer;

public sealed class QuizService
{
    private readonly List<Quiz> _quizzes = new();
    private int _nextId = 1;

    public QuizListResult GetAll(int minQuestions)
    {
        if (minQuestions < 0)
        {
            throw new ArgumentException(
                "minQuestions должен быть неотрицательным.");
        }

        var items = _quizzes
            .Where(q => q.QuestionsCount >= minQuestions)
            .OrderBy(q => q.Id)
            .ToList();

        double? average = items.Count == 0
            ? null
            : items.Average(q => q.QuestionsCount);

        return new QuizListResult(items, average);
    }

    public Quiz GetById(int id)
    {
        return _quizzes.FirstOrDefault(q => q.Id == id)
            ?? throw new KeyNotFoundException(
                $"Тест с Id={id} не найден.");
    }

    public Quiz Create(string? topic, int questionsCount)
    {
        string validTopic = ValidateTopic(topic);
        ValidateQuestionsCount(questionsCount);

        var quiz = new Quiz(
            _nextId++,
            validTopic,
            questionsCount,
            "open");

        _quizzes.Add(quiz);

        return quiz;
    }

    public void Update(
        int id,
        string? topic,
        int questionsCount,
        string? status)
    {
        int index = _quizzes.FindIndex(q => q.Id == id);

        if (index < 0)
        {
            throw new KeyNotFoundException(
                $"Тест с Id={id} не найден.");
        }

        string validTopic = ValidateTopic(topic);
        ValidateQuestionsCount(questionsCount);

        if (status is not ("open" or "closed"))
        {
            throw new ArgumentException(
                "Статус должен быть open или closed.");
        }

        
        _quizzes[index] = new Quiz(
            id,
            validTopic,
            questionsCount,
            status);
    }

    private static string ValidateTopic(string? topic)
    {
        string value = topic?.Trim() ?? "";

        if (value.Length is < 3 or > 80)
        {
            throw new ArgumentException(
                "Тема должна содержать от 3 до 80 символов.");
        }

        return value;
    }

    private static void ValidateQuestionsCount(int questionsCount)
    {
        if (questionsCount is < 5 or > 50)
        {
            throw new ArgumentException(
                "Количество вопросов должно быть от 5 до 50.");
        }
    }
}