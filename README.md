# 441_babin
Ярослав Бабин - лабораторные работы

**Лабораторная работа 1.**</br>
Вариант 1

**Что потребуется для запуска:**
1) Загрузить arcfaceresnet100-8.onnx в папку **lib**
2) Из корневой директории проекта (**lab1/**) запустить следующие команды:</br>
```
dotnet pack lib/
dotnet build app/
dotnet run --project app/
```
**Особенности:**</br>
Изображения берутся из папки images (по умолчанию face1.jpg и face2.jpg)

**Лабораторная работа 2.**</br>
Вариант 1б: приложение отображает два списка с изображениями.</br> Пользователь может выбрать изображения из каждого из списков и увидеть пару значений (distance, similarity). 

**Что потребуется для запуска:**
1) dotnet pack lab1/lib/
2) dotnet run --project .\lab2\wpfapp\

Интерфейс программы:</br>
![GQe6-O6q7wY](https://user-images.githubusercontent.com/33328562/208581594-cee904a8-71ec-411f-bbc0-369e54f88b2f.jpg)

**Лабораторная работа 3.**</br>
**Что потребуется для запуска:**
1) dotnet pack lab1/lib/
2) dotnet add package Microsoft.EntityFrameworkCore.Sqlite
3) dotnet tool install --global dotnet-ef
4) dotnet add package Microsoft.EntityFrameworkCore.Design
5) dotnet ef migrations add InitialCreate
6) dotnet ef database update
7) dotnet run --project .\lab3\wpfapp\

Интерфейс программы (lab3):</br>
![zMmKV5ybYBc](https://user-images.githubusercontent.com/33328562/209270706-1169dc52-7245-40ae-8f7f-d934ca57d219.jpg)
