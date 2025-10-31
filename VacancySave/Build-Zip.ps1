# Скрипт для сборки и упаковки C# функции для Yandex Cloud Functions

# Путь к проекту
$projectPath = ".\VacancySave.csproj"

# Папка для публикации
$publishFolder = ".\publish"

# Имя ZIP файла
$zipFile = ".\function.zip"

Write-Host "=== Удаляем старую папку publish и zip (если есть) ==="
Remove-Item $publishFolder -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zipFile -Force -ErrorAction SilentlyContinue

Write-Host "=== Публикуем проект в Release для linux-x64 ==="
dotnet publish $projectPath -c Release -r linux-x64 --self-contained false /p:PublishTrimmed=true -o $publishFolder

Write-Host "=== Чистим ненужные файлы (.pdb и .xml) ==="
Get-ChildItem $publishFolder -Include *.pdb,*.xml -Recurse | Remove-Item -Force

Write-Host "=== Создаём ZIP архивацию ==="
Set-Location $publishFolder
Compress-Archive -Path * -DestinationPath $zipFile -Force
Set-Location ..

Write-Host "=== Готово! ==="
Write-Host "ZIP файл для Yandex Cloud Functions: $zipFile"
