using System.Net;

namespace QuizServer;

public static class HtmlPages
{
    private static string Encode(string text)
    {
        return WebUtility.HtmlEncode(text);
    }

    private static string Page(string title, string body)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="ru">
            <head>
                <meta charset="utf-8">
                <title>{Encode(title)}</title>
            </head>
            <body>
                <h1>{Encode(title)}</h1>
                {body}
            </body>
            </html>
            """;
    }

    public static string List(QuizListResult result, int minQuestions)
    {
        string list = "";

        foreach (var quiz in result.Items)
        {
            list += $"""
                <li>
                    <a href="/quizzes/details?id={quiz.Id}">
                        {Encode(quiz.Topic)}
                    </a>
                    — вопросов: {quiz.QuestionsCount},
                    статус: {Encode(quiz.Status)}
                </li>
                """;
        }

        string tests = result.Items.Count == 0
            ? "<p>Тестов нет.</p>"
            : $"<ul>{list}</ul>";

        string average = result.AverageQuestionsCount?.ToString("0.##")
            ?? "нет данных";

        string body = $"""
            <form method="get" action="/quizzes">
                Минимум вопросов:
                <input type="number"
                       name="minQuestions"
                       min="0"
                       value="{minQuestions}"
                       required>

                <button type="submit">Найти</button>
            </form>

            {tests}

            <p>Среднее количество вопросов: {average}</p>

            <h2>Создать тест</h2>

            <form method="post" action="/quizzes/create">
                <p>
                    Тема:
                    <input name="topic"
                           minlength="3"
                           maxlength="80"
                           required>
                </p>

                <p>
                    Количество вопросов:
                    <input type="number"
                           name="questionsCount"
                           min="5"
                           max="50"
                           required>
                </p>

                <button type="submit">Создать</button>
            </form>
            """;

        return Page("Учебные тесты", body);
    }

    public static string Details(Quiz quiz)
    {
        string body = $"""
            <p>Id: {quiz.Id}</p>
            <p>Тема: {Encode(quiz.Topic)}</p>
            <p>Количество вопросов: {quiz.QuestionsCount}</p>
            <p>Статус: {Encode(quiz.Status)}</p>

            <a href="/quizzes">Назад к списку</a>
            """;

        return Page("Информация о тесте", body);
    }

    public static string Error(int statusCode, string message)
    {
        string body = $"""
            <p>{Encode(message)}</p>
            <a href="/quizzes">Назад к списку</a>
            """;

        return Page($"Ошибка {statusCode}", body);
    }
}