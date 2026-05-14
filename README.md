# Hair Salon Booking

Backend aplikacji do umawiania wizyt w salonie fryzjerskim, przygotowany pod zasoby widoczne na screenie Azure:

- `booking-api` jako Azure App Service
- `booking-api-plan` jako App Service Plan
- `booking-db` jako Azure Cosmos DB account
- `booking-function` jako Azure Function App
- `bookingfryzjer` jako Storage Account z Blob Storage i Queue Storage

## Projekty

- `HairSalon.Booking.Api` - ASP.NET Core Web API z kontrolerami CRUD i Swaggerem.
- `HairSalon.Booking.Core` - modele domenowe, repozytoria, uslugi i wzorce projektowe.
- `HairSalon.Booking.Functions` - serverless Azure Function reagujaca na komunikat z kolejki.

## Endpointy CRUD

Po uruchomieniu API Swagger jest dostepny pod:

```bash
https://localhost:<port>/swagger
```

Kontrolery:

- `CustomersController` - klienci salonu
- `HairdressersController` - fryzjerzy oraz endpoint dostepnosci terminow
- `AppointmentsController` - wizyty, tworzone przez fasade rezerwacji
- `SalonServicesController` - uslugi salonu fryzjerskiego
- `SalonPhotosController` - zdjecia salonu zapisywane w Azure Blob Storage

API ma dane startowe w trybie lokalnym, jezeli nie podasz connection stringa Cosmos DB.

## Konfiguracja Azure

W `HairSalon.Booking.Api/appsettings.json` ustaw:

```json
{
  "Azure": {
    "Cosmos": {
      "ConnectionString": "<connection-string-do-booking-db>",
      "DatabaseName": "BookingApkaDB",
      "CustomersContainerName": "customers",
      "HairdressersContainerName": "barbers",
      "SalonServicesContainerName": "services",
      "AppointmentsContainerName": "appointments",
      "SalonPhotosContainerName": "salon-photos",
      "UsersContainerName": "users",
      "PartitionKeyPath": "/id"
    },
    "Storage": {
      "ConnectionString": "<connection-string-do-bookingfryzjer>",
      "AppointmentBlobContainer": "appointment-confirmations",
      "HairdresserPhotoBlobContainer": "hairdresser-photos",
      "SalonPhotoBlobContainer": "salon-photos",
      "AppointmentQueueName": "booking-notifications"
    },
    "SignalR": {
      "ConnectionString": "<connection-string-do-azure-signalr>"
    }
  }
}
```

Cosmos DB uzywa osobnych kontenerow dla glownych encji: `customers`, `barbers`, `services`, `appointments`, `users` oraz dodatkowego `salon-photos` dla metadanych zdjec salonu. Kazdy kontener powinien miec partition key `/id`. Blob Storage zapisuje tekstowe potwierdzenia wizyt, a Queue Storage wysyla komunikaty do Azure Function.
Blob Storage zapisuje takze zdjecia fryzjerow i salonu. Endpointy uploadu przyjmuja pliki `multipart/form-data` typu JPG, PNG albo WEBP.

## Easy Auth

Backend ma endpointy pomocnicze dla Azure App Service Authentication:

```text
GET /api/Auth/headers
GET /api/Auth/me
POST /api/Auth/register
```

Easy Auth obsluguje logowanie przez providera, np. GitHub. Backend czyta naglowki `X-MS-CLIENT-PRINCIPAL-*` przekazane przez App Service i zapisuje lokalny profil uzytkownika w kontenerze Cosmos DB `users`.

`POST /api/Auth/register` nie przyjmuje hasla. Rejestracja oznacza utworzenie profilu aplikacji dla juz zalogowanego uzytkownika Easy Auth.

## Azure SignalR

API udostepnia hub:

```text
/hubs/booking-notifications
```

Po utworzeniu wizyty backend wysyla zdarzenia:

```text
appointmentBooked
hairdresserAppointmentBooked
```

Do testu ze Swaggera mozna uzyc:

```text
POST /api/Notifications/test
```

W Azure App Service ustaw zmienna srodowiskowa:

```text
Azure__SignalR__ConnectionString
```

na connection string z Azure SignalR Service.

## Key Vault, Monitor i Email

API i Function moga pobierac sekrety z Azure Key Vault. Ustaw:

```text
Azure__KeyVault__VaultUri
```

w App Service oraz:

```text
KeyVault:VaultUri
```

w Function App. W Key Vault nazwy sekretow dla konfiguracji .NET zapisuj z `--`, np.:

```text
Azure--Cosmos--ConnectionString
Azure--Storage--ConnectionString
Azure--SignalR--ConnectionString
Email--ConnectionString
Email--SenderAddress
```

Application Insights / Azure Monitor wlaczysz przez connection string:

```text
APPLICATIONINSIGHTS_CONNECTION_STRING
```

dla Function App oraz przez:

```text
ApplicationInsights__ConnectionString
```

dla App Service.

Azure Communication Services Email wysyla potwierdzenie wizyty z Azure Function po odebraniu komunikatu z kolejki `booking-notifications`. Funkcja wymaga:

```text
Email:ConnectionString
Email:SenderAddress
```

`SenderAddress` to adres nadawcy skonfigurowany w Azure Communication Services Email, a nie zwykle haslo do Gmaila/Outlooka.

## Zdjecia

Zdjecie fryzjera dodasz w Swaggerze przez:

```text
POST /api/Hairdressers/{id}/photo
```

Zdjecie salonu dodasz przez:

```text
POST /api/SalonPhotos
```

Lista zdjec salonu:

```text
GET /api/SalonPhotos
```

Usuniecie zdjecia salonu z metadanych i Blob Storage:

```text
DELETE /api/SalonPhotos/{id}
```

## Wzorce projektowe z prezentacji

1. Factory Method - `BookingEntityFactory` tworzy encje domenowe z prawidlowa konfiguracja bazowa.
2. Facade - `AppointmentBookingFacade` ukrywa zlozonosc umawiania wizyty: waliduje klienta, fryzjera, usluge, kolizje terminow, zapis i publikacje zdarzen.
3. Iterator - `AvailabilitySlotCollection` udostepnia kolejne sloty godzinowe bez ujawniania wewnetrznej listy.
4. Observer/Event - `BookingEventPublisher` publikuje zdarzenie `AppointmentBooked`, a handlery Blob i Queue reaguja niezaleznie.
5. Asynchronous pattern - repozytoria, fasada i integracje Azure sa oparte o `Task`, `async` i `await`.

## Uruchomienie lokalne

```bash
dotnet restore
dotnet build
dotnet run --project HairSalon.Booking.Api
```

W Swaggerze mozna od razu przetestowac przykladowa rezerwacje:

```json
{
  "customerId": "customer-demo",
  "hairdresserId": "hairdresser-demo",
  "salonServiceId": "service-demo",
  "startAt": "2026-05-12T10:00:00Z",
  "status": "Booked",
  "notes": "Pierwsza wizyta"
}
```

## Azure Function

Funkcja `AppointmentBookedQueueFunction` odbiera komunikaty z kolejki `booking-notifications`. W Azure ustaw `AzureWebJobsStorage` na connection string storage account `bookingfryzjer`.
