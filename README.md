# Real Estate Platform

Fullstack-платформа для работы с недвижимостью с фокусом на **Backend-разработку, микросервисную архитектуру, аутентификацию и интеграцию внешних сервисов**.

Backend разработан на **ASP.NET Core / C#**, frontend — на **Next.js / React / TypeScript**.

## О проекте

Проект представляет собой real estate-платформу с системой регистрации и авторизации пользователей, управлением аккаунтом и сессиями, а также интеграцией внешних API.

Backend построен с использованием **микросервисной архитектуры**. Для аутентификации используется JWT, для асинхронного взаимодействия между сервисами — **Kafka**, для кеширования и временных данных — **Redis**, для хранения постоянных данных — **PostgreSQL**.

## Основной функционал

### Аутентификация и управление аккаунтом

- Регистрация и авторизация по `email/password`
- **JWT**-аутентификация с использованием **`Access Token`** и **`Refresh Token`**
- Автоматическое обновление `Access Token`
- Подтверждение email
- Двухэтапная верификация email
- Восстановление и смена пароля
- Авторизация через **Google OAuth**
- Привязка и отвязка OAuth-аккаунта к существующему пользователю
- Безопасная смена email
- **Управление активными сессиями и устройствами**
- Просмотр активных сессий
- Выход из всех активных сессий
- JWT-аутентификация и авторизация между микросервисами
- Передача и валидация `claims` между сервисами

## Backend

Backend реализован на **ASP.NET Core / C#** и построен с использованием микросервисного подхода.

Основные направления работы:

- Разработка REST API
- **JWT** Authentication & Authorization
- **OAuth**
- **EntityFramework**
- Service-to-Service Authentication
- Работа с **PostgreSQL**
- Кеширование и временные данные через Redis
- Асинхронное взаимодействие микросервисов через **Kafka**
- Transactional emails через **SMTP**
- Работа с **S3**-compatible storage
- **Интеграция внешних REST API**
- Обработка отмены асинхронных операций через **CancellationToken**
- **Мультиязычность** (русский/английский)

## Микросервисная архитектура

Микросервисы взаимодействуют между собой двумя основными способами:

- синхронное взаимодействие через REST API;
- асинхронное взаимодействие через Kafka.

Для межсервисной аутентификации используется **JWT**.

JWT применяется не только для авторизации пользователей, но и для проверки запросов между микросервисами.

При межсервисном взаимодействии выполняется:

- валидация JWT;
- проверка `issuer` и `audience`;
- проверка claims;
- определение вызывающего сервиса;

## Скриншоты:
- Сраница логина
<img width="974" height="484" alt="image" src="https://github.com/user-attachments/assets/eab943d5-bbbe-4ae4-96f3-4649c750a0f7" />

- Настройки-профиль
<img width="974" height="463" alt="image" src="https://github.com/user-attachments/assets/cc5431b2-a268-463a-adaa-fa93c3aa4dd0" />
<img width="974" height="236" alt="image" src="https://github.com/user-attachments/assets/048ae790-2480-4552-a948-2b976b991f9a" />
<img width="974" height="358" alt="image" src="https://github.com/user-attachments/assets/92429563-bbac-4653-b1d6-f6cbe596dc10" />
<img width="974" height="345" alt="image" src="https://github.com/user-attachments/assets/025c7920-0fdb-4c02-9c88-0f1c98247606" />

- Настройки-безопасность
<img width="974" height="488" alt="image" src="https://github.com/user-attachments/assets/4a713acb-ad94-40d5-967a-247ddfa4e95f" />
<img width="974" height="493" alt="image" src="https://github.com/user-attachments/assets/f03b6bee-958d-47f8-9045-964614cdb529" />
<img width="737" height="246" alt="image" src="https://github.com/user-attachments/assets/873cea28-0a59-4c0e-bb67-79b37ac2f4ce" />
<img width="759" height="296" alt="image" src="https://github.com/user-attachments/assets/7049d21f-36fe-40e6-a164-d61b9e06f8d9" />

- Автоподсказки населенных пунктов
  <img width="699" height="576" alt="image" src="https://github.com/user-attachments/assets/0d07d60d-86bb-4396-ba21-bceaaba536b5" />
- Перевод автоподсказок населенных пунктов
  <img width="699" height="409" alt="image" src="https://github.com/user-attachments/assets/672728ed-93c6-46b3-9043-ece49b99398b" />
- Пример мультиязычности
  <img width="761" height="827" alt="image" src="https://github.com/user-attachments/assets/e36977cd-8d87-43db-b450-ccb7b3ba950b" />



