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
      "DatabaseName": "booking-db",
      "ContainerName": "booking-items"
    },
    "Storage": {
      "ConnectionString": "<connection-string-do-bookingfryzjer>",
      "AppointmentBlobContainer": "appointment-confirmations",
      "HairdresserPhotoBlobContainer": "hairdresser-photos",
      "SalonPhotoBlobContainer": "salon-photos",
      "AppointmentQueueName": "appointment-booked"
    }
  }
}
```

Cosmos DB uzywa jednego kontenera z partition key `/partitionKey`. Blob Storage zapisuje tekstowe potwierdzenia wizyt, a Queue Storage wysyla komunikaty do Azure Function.
Blob Storage zapisuje takze zdjecia fryzjerow i salonu. Endpointy uploadu przyjmuja pliki `multipart/form-data` typu JPG, PNG albo WEBP.

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

Funkcja `AppointmentBookedQueueFunction` odbiera komunikaty z kolejki `appointment-booked`. W Azure ustaw `AzureWebJobsStorage` na connection string storage account `bookingfryzjer`.
