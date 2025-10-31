# Тестовое задание: C# backend для сбора и чтения статистики по вакансиям

## Цель
Необходимо реализовать **две serverless-функции** (на платформе Yandex Cloud Functions) на C#:

1. **`SaveVacancyStats`**  
   - Делает запрос на [HeadHunter API](https://api.hh.ru/) для получения количества вакантных позиций по одной профессии: **C# Developer**.  
   - Сохраняет полученные данные в **Yandex Object Storage** (совместимый с AWS S3) в файл `vacancies_stats.json`.
     - Формат данных (массив JSON):
       ```json
       [
         {
           "date": "2025-04-16",
           "vacancies": 123
         }
       ]
       ```
     - Если на конкретную дату запись уже существует, она перезаписывается.

2. **`GetVacancyStats`**  
   - Считывает файл `vacancies_stats.json` из того же **S3-бакета**.
   - Возвращает данные в формате JSON.

## Детали реализации
1. **HeadHunter API**  
   - Использовать GET-запрос:  
     `https://api.hh.ru/vacancies?text=C%23%20Developer&schedule=remote&per_page=100`
   - Из ответа брать значение поля `found`.

2. **Авторизация в Yandex Object Storage**  
   - Использовать AWS S3-совместимый доступ (ключи брать из переменных окружения).

3. **Технологии**  
   - Язык: **C# (.NET 8)**  
   - Рекомендуемые пакеты:
     - `HttpClient` для запросов к HeadHunter API.
     - `AWSSDK.S3` (или аналог) для работы с Yandex Object Storage.

4. **Структура проекта**  
   - Минимум два файла для каждой функции:
     - `SaveVacancyStats.cs`
     - `GetVacancyStats.cs`

5. **Формат ответа**  
   - При успешном чтении `GetVacancyStats` возвращает JSON-массив с датами и количеством вакансий.

## Критерии оценки
- Чистота и читаемость кода.
- Корректность работы с S3 и HeadHunter API.
- Умение работать с JSON.
