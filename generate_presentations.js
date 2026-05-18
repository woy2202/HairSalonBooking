const pptxgen = require("pptxgenjs");
const fs = require("fs");
const path = require("path");

const ROOT = __dirname;
const azureImage = "C:\\Users\\woy22\\Downloads\\azure.jpg";

const C = {
  navy: "183A59",
  blue: "2474A6",
  cyan: "49B6D6",
  green: "5AA469",
  orange: "E28A3B",
  red: "C75146",
  ink: "1F2933",
  muted: "64748B",
  paleBlue: "EAF4FB",
  paleGreen: "EAF6ED",
  paleOrange: "FFF2E5",
  line: "D8E1EA",
  white: "FFFFFF",
  dark: "0F172A",
};

function pptBase() {
  const pptx = new pptxgen();
  pptx.layout = "LAYOUT_WIDE";
  pptx.author = "Wiktor Gochnio";
  pptx.company = "University of Gdansk";
  pptx.subject = "HairSalon Booking";
  pptx.theme = {
    headFontFace: "Aptos Display",
    bodyFontFace: "Aptos",
    lang: "pl-PL",
  };
  pptx.defineLayout({ name: "CUSTOM_WIDE", width: 13.333, height: 7.5 });
  pptx.layout = "CUSTOM_WIDE";
  return pptx;
}

function addFooter(slide, index, label) {
  slide.addShape("line", {
    x: 0.55, y: 7.08, w: 12.25, h: 0,
    line: { color: C.line, width: 0.8 },
  });
  slide.addText(label, {
    x: 0.6, y: 7.15, w: 8.5, h: 0.18,
    fontSize: 7.5, color: C.muted, margin: 0,
  });
  slide.addText(String(index).padStart(2, "0"), {
    x: 12.15, y: 7.12, w: 0.65, h: 0.22,
    fontSize: 8.5, color: C.muted, bold: true, align: "right", margin: 0,
  });
}

function title(slide, text, subtitle) {
  slide.addText(text, {
    x: 0.62, y: 0.35, w: 8.2, h: 0.55,
    fontFace: "Aptos Display", fontSize: 24, bold: true,
    color: C.navy, margin: 0,
  });
  if (subtitle) {
    slide.addText(subtitle, {
      x: 0.64, y: 0.93, w: 9.5, h: 0.3,
      fontSize: 10.5, color: C.muted, margin: 0,
    });
  }
}

function cover(pptx, deckTitle, subtitle, kicker, color = C.navy) {
  const slide = pptx.addSlide();
  slide.background = { color: C.white };
  slide.addShape("rect", {
    x: 0, y: 0, w: 13.333, h: 7.5,
    fill: { color: C.white }, line: { color: C.white },
  });
  slide.addShape("rect", {
    x: 0, y: 0, w: 4.2, h: 7.5,
    fill: { color }, line: { color },
  });
  slide.addText("HairSalon\nBooking", {
    x: 0.55, y: 0.55, w: 2.7, h: 0.8,
    fontSize: 18, bold: true, color: C.white, margin: 0, breakLine: false,
  });
  slide.addShape("arc", {
    x: 2.45, y: 4.72, w: 2.2, h: 2.2,
    line: { color: C.cyan, width: 2.2, transparency: 25 },
    adjustPoint: 0.3,
  });
  slide.addShape("line", {
    x: 0.55, y: 6.3, w: 2.6, h: 0,
    line: { color: C.cyan, width: 2 },
  });
  slide.addText(kicker, {
    x: 5.05, y: 1.05, w: 5.8, h: 0.25,
    fontSize: 10, color: C.blue, bold: true, margin: 0,
  });
  slide.addText(deckTitle, {
    x: 5.0, y: 1.55, w: 7.2, h: 1.5,
    fontFace: "Aptos Display", fontSize: 32, bold: true,
    color: C.ink, margin: 0, fit: "shrink",
  });
  slide.addText(subtitle, {
    x: 5.05, y: 3.25, w: 6.5, h: 0.65,
    fontSize: 15, color: C.muted, margin: 0, breakLine: false,
  });
  slide.addShape("line", {
    x: 5.05, y: 4.4, w: 2.2, h: 0,
    line: { color: C.orange, width: 3 },
  });
  slide.addText("Projekt C# / .NET 8 / Azure\n18.05.2026", {
    x: 5.05, y: 5.3, w: 3.8, h: 0.55,
    fontSize: 10.5, color: C.muted, margin: 0,
  });
}

function bullets(slide, items, x, y, w, h, opts = {}) {
  const runs = [];
  items.forEach((item) => {
    runs.push({ text: item, options: { bullet: { type: "bullet" } } });
  });
  slide.addText(runs, {
    x, y, w, h,
    fontSize: opts.fontSize || 15,
    color: opts.color || C.ink,
    breakLine: false,
    fit: "shrink",
    paraSpaceAfterPt: 8,
    margin: 0.08,
  });
}

function pill(slide, text, x, y, w, color = C.paleBlue, textColor = C.navy) {
  slide.addShape("roundRect", {
    x, y, w, h: 0.34,
    rectRadius: 0.08,
    fill: { color },
    line: { color, transparency: 100 },
  });
  slide.addText(text, {
    x: x + 0.1, y: y + 0.075, w: w - 0.2, h: 0.12,
    fontSize: 8.5, bold: true, color: textColor, margin: 0, align: "center",
  });
}

function card(slide, heading, body, x, y, w, h, accent = C.blue) {
  slide.addShape("roundRect", {
    x, y, w, h,
    rectRadius: 0.06,
    fill: { color: C.white },
    line: { color: C.line, width: 1 },
  });
  slide.addShape("rect", {
    x, y, w: 0.08, h,
    fill: { color: accent },
    line: { color: accent },
  });
  slide.addText(heading, {
    x: x + 0.22, y: y + 0.18, w: w - 0.35, h: 0.25,
    fontSize: 13.2, bold: true, color: C.navy, margin: 0,
  });
  slide.addText(body, {
    x: x + 0.22, y: y + 0.55, w: w - 0.35, h: h - 0.65,
    fontSize: 10.2, color: C.ink, margin: 0, fit: "shrink",
    breakLine: false,
  });
}

function flowNode(slide, text, x, y, w, color) {
  slide.addShape("roundRect", {
    x, y, w, h: 0.75, rectRadius: 0.08,
    fill: { color }, line: { color },
  });
  slide.addText(text, {
    x: x + 0.08, y: y + 0.18, w: w - 0.16, h: 0.25,
    fontSize: 10.3, bold: true, color: C.white, align: "center", margin: 0,
    fit: "shrink",
  });
}

function arrow(slide, x, y, w) {
  slide.addShape("rightArrow", {
    x, y, w, h: 0.18,
    fill: { color: C.line }, line: { color: C.line },
  });
}

function placeholder(slide, text, x, y, w, h) {
  slide.addShape("roundRect", {
    x, y, w, h, rectRadius: 0.06,
    fill: { color: "F8FAFC" },
    line: { color: C.line, dash: "dash", width: 1.2 },
  });
  slide.addText("[Zrzut ekranu]", {
    x: x + 0.15, y: y + 0.25, w: w - 0.3, h: 0.25,
    fontSize: 13, bold: true, color: C.blue, align: "center", margin: 0,
  });
  slide.addText(text, {
    x: x + 0.35, y: y + 0.75, w: w - 0.7, h: h - 1,
    fontSize: 10.3, color: C.muted, align: "center", valign: "mid",
    margin: 0.05, fit: "shrink",
  });
}

function section(slide, text, x = 0.65, y = 1.35, w = 2.2) {
  pill(slide, text, x, y, w, C.paleOrange, C.orange);
}

function addTwoColumnTable(slide, rows, x, y, w, rowH = 0.48) {
  const h = rows.length * rowH;
  slide.addShape("roundRect", {
    x, y, w, h, rectRadius: 0.04,
    fill: { color: C.white }, line: { color: C.line },
  });
  rows.forEach((row, i) => {
    const yy = y + i * rowH;
    if (i > 0) slide.addShape("line", { x, y: yy, w, h: 0, line: { color: C.line, width: 0.5 } });
    slide.addText(row[0], { x: x + 0.15, y: yy + 0.12, w: w * 0.35, h: 0.16, fontSize: 9.5, bold: true, color: C.navy, margin: 0, fit: "shrink" });
    slide.addText(row[1], { x: x + w * 0.38, y: yy + 0.12, w: w * 0.57, h: 0.16, fontSize: 9.2, color: C.ink, margin: 0, fit: "shrink" });
  });
}

async function buildAzureDeck() {
  const pptx = pptBase();
  cover(pptx, "Raport projektu:\nchmura Azure", "Architektura i funkcjonalność backendu salonu fryzjerskiego", "RAPORT 1", C.navy);

  let s = pptx.addSlide();
  title(s, "Cel projektu", "Co aplikacja ma pokazać z perspektywy chmury");
  bullets(s, [
    "Backend salonu fryzjerskiego do umawiania i zarządzania wizytami.",
    "Pełny CRUD dla klientów, fryzjerów, usług i wizyt testowany w Swaggerze.",
    "Dane przechowywane w Azure Cosmos DB w osobnych kontenerach.",
    "Zadania poboczne, takie jak email, przeniesione do kolejki i Azure Functions.",
    "Powiadomienia realtime przez Azure SignalR Service.",
  ], 0.85, 1.55, 6.1, 4.1);
  card(s, "Efekt końcowy", "Po utworzeniu wizyty klient otrzymuje mail z potwierdzeniem, a aplikacja może równolegle wysłać powiadomienie realtime do panelu/fryzjera.", 7.45, 1.55, 4.8, 2.0, C.green);
  card(s, "Zakres demonstracji", "Projekt pokazuje App Service, Cosmos DB, Storage Queue, Blob Storage, Function App, SignalR, Easy Auth, Key Vault i monitoring.", 7.45, 3.9, 4.8, 1.8, C.blue);
  addFooter(s, 2, "HairSalon Booking / raport chmurowy");

  s = pptx.addSlide();
  title(s, "Architektura rozwiązania", "Jak komponenty Azure współpracują po utworzeniu wizyty");
  flowNode(s, "Swagger / frontend", 0.65, 2.0, 1.55, C.blue);
  arrow(s, 2.28, 2.28, 0.55);
  flowNode(s, "App Service\nbooking-api", 2.95, 1.88, 1.55, C.navy);
  arrow(s, 4.58, 2.28, 0.55);
  flowNode(s, "Cosmos DB", 5.25, 0.95, 1.35, C.green);
  flowNode(s, "Blob Storage", 5.25, 1.9, 1.35, C.green);
  flowNode(s, "Queue Storage", 5.25, 2.85, 1.35, C.green);
  arrow(s, 6.78, 3.13, 0.65);
  flowNode(s, "Azure Function\nbooking-function", 7.55, 2.72, 1.8, C.orange);
  arrow(s, 9.48, 3.13, 0.65);
  flowNode(s, "ACS Email", 10.25, 2.72, 1.45, C.red);
  flowNode(s, "SignalR", 7.55, 1.45, 1.8, C.cyan);
  slideNote(s, "W przepływie mailowym API nie wysyła maila bezpośrednio. API zapisuje wizytę i wrzuca komunikat do kolejki, a Function App wysyła email w tle.");
  addFooter(s, 3, "Architektura Azure");

  s = pptx.addSlide();
  title(s, "Wybrane technologie", "Dobór usług był podporządkowany wymaganiom projektu");
  addTwoColumnTable(s, [
    ["ASP.NET Core", "Kontrolery REST i Swagger dla pełnego CRUD."],
    ["Azure App Service", "Hosting API bez zarządzania serwerem."],
    ["Cosmos DB", "Dokumentowa baza JSON dla klientów, wizyt i usług."],
    ["Storage Queue", "Oddzielenie rezerwacji od wysyłki maila."],
    ["Azure Functions", "Serverless obsługujący komunikaty z kolejki."],
    ["SignalR", "Powiadomienia realtime po utworzeniu wizyty."],
    ["ACS Email", "Potwierdzenia wizyt wysyłane do klienta."],
  ], 0.75, 1.45, 5.7, 0.55);
  placeholder(s, "Wstaw opcjonalnie zrzut z listą zasobów w Azure Portal.", 7.15, 1.45, 4.85, 3.9);
  addFooter(s, 4, "Technologie i uzasadnienie");

  s = pptx.addSlide();
  title(s, "Zasoby w Azure", "Grupa zasobów Booking");
  if (fs.existsSync(azureImage)) {
    s.addImage({ path: azureImage, x: 0.7, y: 1.25, w: 7.2, h: 4.05 });
  } else {
    placeholder(s, "Zrzut grupy zasobów Booking.", 0.7, 1.25, 7.2, 4.05);
  }
  bullets(s, [
    "booking-api jako App Service.",
    "booking-db jako konto Azure Cosmos DB.",
    "booking-function jako Function App.",
    "bookingfryzjer jako Storage Account.",
    "Azure SignalR i ACS Email jako usługi dodatkowe.",
  ], 8.35, 1.5, 3.75, 3.2, { fontSize: 12.2 });
  addFooter(s, 5, "Azure Portal / grupa zasobów");

  s = pptx.addSlide();
  title(s, "Cosmos DB", "Baza dokumentowa dopasowana do modelu aplikacji");
  addTwoColumnTable(s, [
    ["customers", "Klienci salonu i dane kontaktowe."],
    ["barbers", "Fryzjerzy, specjalizacja, status aktywności."],
    ["services", "Usługi, czas trwania, cena, dostępność."],
    ["appointments", "Wizyty i powiązania z klientem/fryzjerem/usługą."],
    ["users", "Konta aplikacji oraz role klient/fryzjer/admin."],
    ["Partition key", "We wszystkich kontenerach: /id."],
  ], 0.8, 1.35, 5.85, 0.6);
  placeholder(s, "Wstaw zrzut Cosmos DB Data Explorer z kontenerami.", 7.2, 1.35, 4.75, 3.9);
  addFooter(s, 6, "Cosmos DB / Data Explorer");

  s = pptx.addSlide();
  title(s, "Logowanie i role", "Easy Auth jako warstwa uwierzytelniania");
  card(s, "Easy Auth", "App Service Authentication przechwytuje request przed API i sprawdza, czy użytkownik jest zalogowany przez Google/GitHub.", 0.8, 1.45, 3.7, 1.6, C.blue);
  card(s, "Backend", "API odczytuje nagłówki X-MS-CLIENT-PRINCIPAL i tworzy konto AppUser oraz Customer.", 4.85, 1.45, 3.7, 1.6, C.green);
  card(s, "Role", "Klient rezerwuje wizyty, fryzjer przegląda swoje wizyty, admin zarządza użytkownikami i danymi.", 8.9, 1.45, 3.4, 1.6, C.orange);
  placeholder(s, "Wstaw zrzut /api/Auth/me albo konfiguracji Authentication w App Service.", 1.0, 3.65, 10.9, 1.65);
  addFooter(s, 7, "Easy Auth / role");

  s = pptx.addSlide();
  title(s, "Kolejka, funkcja i email", "Najważniejszy scenariusz chmurowy");
  flowNode(s, "POST\n/Appointments", 0.7, 2.1, 1.45, C.blue);
  arrow(s, 2.25, 2.38, 0.55);
  flowNode(s, "Save\nCosmos DB", 2.9, 2.1, 1.35, C.green);
  arrow(s, 4.35, 2.38, 0.55);
  flowNode(s, "Queue\nbooking-notifications", 5.0, 2.1, 1.75, C.orange);
  arrow(s, 6.85, 2.38, 0.55);
  flowNode(s, "Function\ntrigger", 7.5, 2.1, 1.45, C.navy);
  arrow(s, 9.05, 2.38, 0.55);
  flowNode(s, "Email\nDoNotReply", 9.7, 2.1, 1.45, C.red);
  card(s, "Dlaczego tak?", "API szybko zapisuje wizytę, a mail jest zadaniem w tle. Jeśli email chwilowo nie działa, komunikat może trafić do poison queue i zostać zdiagnozowany.", 1.0, 4.05, 10.2, 1.3, C.green);
  addFooter(s, 8, "Queue Storage + Azure Functions + ACS Email");

  s = pptx.addSlide();
  title(s, "Blob Storage i SignalR", "Dwa różne typy funkcji chmurowych");
  card(s, "Blob Storage", "Przechowuje zdjęcia salonu i fryzjerów oraz pliki tekstowe z podsumowaniem wizyty. To miejsce na trwałe pliki.", 0.85, 1.5, 5.15, 2.0, C.green);
  card(s, "SignalR", "Wysyła natychmiastowe powiadomienie appointmentBooked do podłączonych klientów. To komunikacja realtime.", 6.75, 1.5, 5.15, 2.0, C.cyan);
  placeholder(s, "Wstaw zrzut strony /api/Notifications/signalr-test z działającym SignalR.", 0.85, 4.0, 5.15, 1.55);
  placeholder(s, "Wstaw zrzut kontenera Blob Storage lub zdjęcia dodanego w Swaggerze.", 6.75, 4.0, 5.15, 1.55);
  addFooter(s, 9, "Blob Storage / SignalR");

  s = pptx.addSlide();
  title(s, "Funkcjonalności aplikacji", "Lista funkcji widocznych w Swaggerze i Azure");
  bullets(s, [
    "CRUD klientów, fryzjerów, usług salonu i wizyt.",
    "Rezerwacja wizyty z walidacją danych i kolizji terminów.",
    "Historia wizyt klienta oraz widok wizyt fryzjera.",
    "Role użytkowników: Customer, Hairdresser, Admin.",
    "Dodawanie zdjęć fryzjerów i salonu do Blob Storage.",
    "Email z potwierdzeniem po utworzeniu wizyty.",
    "Powiadomienia realtime przez SignalR.",
  ], 0.85, 1.35, 5.8, 4.6, { fontSize: 13.2 });
  placeholder(s, "Wstaw zrzut Swaggera z listą kontrolerów.", 7.1, 1.35, 4.8, 3.85);
  addFooter(s, 10, "Funkcjonalność");

  s = pptx.addSlide();
  title(s, "Przygotowanie projektu", "Proces konfiguracji od kodu do chmury");
  addTwoColumnTable(s, [
    ["1", "Utworzenie projektów API, Core i Functions w Visual Studio."],
    ["2", "Dodanie modeli domenowych i kontrolerów CRUD."],
    ["3", "Konfiguracja Cosmos DB i kontenerów z partition key /id."],
    ["4", "Podłączenie Storage Account: Blob + Queue."],
    ["5", "Wdrożenie App Service i Function App."],
    ["6", "Dodanie Easy Auth, SignalR, Key Vault, Monitor i ACS Email."],
    ["7", "Testy w Swaggerze, Log Stream, Queue Storage i skrzynce email."],
  ], 0.85, 1.35, 6.2, 0.55);
  placeholder(s, "Wstaw zrzut Visual Studio: Solution Explorer lub publikowanie do Azure.", 7.55, 1.35, 4.55, 3.75);
  addFooter(s, 11, "Przygotowanie projektu");

  s = pptx.addSlide();
  title(s, "Testy i diagnostyka", "Jak potwierdzono działanie komponentów");
  card(s, "Swagger", "CRUD i scenariusz POST /api/Appointments.", 0.85, 1.45, 3.4, 1.35, C.blue);
  card(s, "Queue Storage", "Widoczna wiadomość w booking-notifications oraz poison queue przy błędach.", 4.85, 1.45, 3.4, 1.35, C.orange);
  card(s, "Log Stream", "Logi App Service i Function App pokazują przepływ requestów.", 8.85, 1.45, 3.4, 1.35, C.green);
  card(s, "Email", "Potwierdzenie dostarczone na prawdziwą skrzynkę użytkownika.", 2.2, 3.75, 3.7, 1.35, C.red);
  card(s, "SignalR", "Strona testowa potwierdziła odbiór appointmentBooked.", 7.1, 3.75, 3.7, 1.35, C.cyan);
  addFooter(s, 12, "Testy");

  s = pptx.addSlide();
  title(s, "Podsumowanie", "Co pokazuje projekt");
  bullets(s, [
    "Backend C# został wdrożony w Azure App Service.",
    "Dane aplikacji działają w Cosmos DB w logicznie podzielonych kontenerach.",
    "Wysyłka maila jest zrobiona chmurowo przez Queue + Function + ACS Email.",
    "SignalR dodaje powiadomienia realtime.",
    "Projekt pokazuje pełną integrację kilku usług Azure w jednym scenariuszu biznesowym.",
  ], 1.1, 1.6, 8.2, 3.4, { fontSize: 17 });
  slideNote(s, "Najważniejsze zdanie: aplikacja nie jest tylko CRUD-em, bo używa kilku usług chmurowych współpracujących w jednym przepływie rezerwacji.");
  addFooter(s, 13, "Podsumowanie");

  await pptx.writeFile({ fileName: path.join(ROOT, "Prezentacja_1_Chmura_Azure_HairSalonBooking.pptx") });
}

function slideNote(slide, text) {
  slide.addNotes(text);
}

async function buildPatternsDeck() {
  const pptx = pptBase();
  cover(pptx, "Raport projektu:\nwzorce projektowe", "Jak design patterns porządkują backend salonu fryzjerskiego", "RAPORT 2", C.green);

  let s = pptx.addSlide();
  title(s, "Cel prezentacji", "Pokazać, gdzie wzorce występują w kodzie i po co zostały użyte");
  bullets(s, [
    "Projekt nie używa wzorców jako teorii, tylko jako organizacji realnego backendu.",
    "Najważniejsze wzorce: Facade, Repository, Observer/Event, Strategy/Handler, Factory Method.",
    "Dodatkowo: Dependency Injection, Adapter, Singleton, Iterator i Options Pattern.",
    "Każdy wzorzec rozwiązuje konkretny problem w aplikacji rezerwacyjnej.",
  ], 0.9, 1.5, 6.2, 3.6);
  card(s, "Główna zasada", "Kontrolery mają być proste. Logika biznesowa, dane i integracje Azure są przeniesione do osobnych klas.", 7.6, 1.65, 4.2, 1.75, C.green);
  addFooter(s, 2, "Wzorce projektowe / cel");

  s = pptx.addSlide();
  title(s, "Mapa wzorców w projekcie", "Jeden backend, kilka ról architektonicznych");
  addTwoColumnTable(s, [
    ["Facade", "AppointmentBookingFacade"],
    ["Repository", "IBookingRepository, CosmosBookingRepository"],
    ["Observer/Event", "BookingEventPublisher"],
    ["Strategy/Handler", "IAppointmentBookedHandler i implementacje"],
    ["Factory Method", "BookingEntityFactory"],
    ["Adapter", "BlobPhotoStorageService, EasyAuthCurrentUserService, EmailSender"],
    ["Singleton", "CosmosClient przez Dependency Injection"],
  ], 0.85, 1.35, 5.9, 0.55);
  placeholder(s, "Wstaw zrzut Solution Explorer z projektami Api/Core/Functions.", 7.25, 1.35, 4.7, 3.9);
  addFooter(s, 3, "Mapa wzorców");

  s = pptx.addSlide();
  title(s, "Facade", "AppointmentBookingFacade ukrywa złożoność rezerwacji");
  add_code_slide(s,
    "public async Task<Appointment> BookAsync(Appointment appointment, CancellationToken ct)\n" +
    "{\n" +
    "    var customer = await customers.GetAsync(appointment.CustomerId, ct);\n" +
    "    var hairdresser = await hairdressers.GetAsync(appointment.HairdresserId, ct);\n" +
    "    var service = await services.GetAsync(appointment.SalonServiceId, ct);\n" +
    "    appointment.EndAt = appointment.StartAt.AddMinutes(service.DurationMinutes);\n" +
    "    await EnsureSlotIsFreeAsync(appointment, ct);\n" +
    "    var created = await appointments.CreateAsync(appointment, ct);\n" +
    "    await eventPublisher.PublishAppointmentBookedAsync(created, ct);\n" +
    "    return created;\n" +
    "}", 0.75, 1.35, 6.15, 4.55);
  bullets(s, [
    "Kontroler wywołuje jedną metodę BookAsync.",
    "Facade pobiera dane, waliduje, zapisuje wizytę i publikuje zdarzenie.",
    "Dzięki temu logika biznesowa nie jest rozbita po kontrolerach.",
  ], 7.35, 1.65, 4.7, 2.7, { fontSize: 13.2 });
  addFooter(s, 4, "Facade");

  s = pptx.addSlide();
  title(s, "Repository", "Kontrolery nie pracują bezpośrednio z Cosmos DB");
  add_code_slide(s,
    "public interface IBookingRepository<T> where T : BookingEntity\n" +
    "{\n" +
    "    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct);\n" +
    "    Task<T?> GetAsync(string id, CancellationToken ct);\n" +
    "    Task<T> CreateAsync(T entity, CancellationToken ct);\n" +
    "    Task<T> UpsertAsync(T entity, CancellationToken ct);\n" +
    "    Task DeleteAsync(string id, CancellationToken ct);\n" +
    "}", 0.8, 1.45, 5.65, 3.15);
  card(s, "CosmosBookingRepository", "Implementacja ukrywa Container, PartitionKey, QueryDefinition i CosmosException.", 7.05, 1.45, 4.6, 1.45, C.blue);
  card(s, "InMemoryBookingRepository", "Alternatywna implementacja dla lokalnego działania bez Cosmos DB.", 7.05, 3.25, 4.6, 1.45, C.orange);
  addFooter(s, 5, "Repository");

  s = pptx.addSlide();
  title(s, "Observer / Event", "Po utworzeniu wizyty wiele komponentów reaguje niezależnie");
  flowNode(s, "BookAsync\ncreated", 0.8, 2.0, 1.55, C.green);
  arrow(s, 2.45, 2.28, 0.65);
  flowNode(s, "BookingEvent\nPublisher", 3.2, 2.0, 1.65, C.navy);
  arrow(s, 4.95, 2.28, 0.65);
  flowNode(s, "Blob\nhandler", 5.75, 1.25, 1.35, C.blue);
  flowNode(s, "Queue\nhandler", 5.75, 2.2, 1.35, C.orange);
  flowNode(s, "SignalR\nhandler", 5.75, 3.15, 1.35, C.cyan);
  card(s, "Dlaczego?", "Rezerwacja nie musi znać szczegółów każdej reakcji. Można dodać SMS albo raport jako kolejny handler.", 7.9, 1.55, 3.9, 2.2, C.green);
  add_code_slide(s,
    "foreach (var handler in handlers)\n{\n    await handler.HandleAsync(appointment, cancellationToken);\n}", 7.9, 4.25, 3.9, 0.9);
  addFooter(s, 6, "Observer / Event");

  s = pptx.addSlide();
  title(s, "Strategy / Handler", "Ten sam interfejs, różne zachowania");
  add_code_slide(s,
    "public interface IAppointmentBookedHandler\n" +
    "{\n" +
    "    Task HandleAsync(Appointment appointment, CancellationToken cancellationToken);\n" +
    "}", 0.8, 1.4, 5.25, 1.5);
  card(s, "AzureBlobAppointmentSummaryHandler", "Zapisuje podsumowanie wizyty do Blob Storage.", 0.95, 3.35, 3.55, 1.35, C.blue);
  card(s, "AzureQueueAppointmentBookedHandler", "Wrzuca wiadomość do kolejki, aby Function wysłała mail.", 4.9, 3.35, 3.55, 1.35, C.orange);
  card(s, "SignalRAppointmentBookedHandler", "Wysyła powiadomienie realtime do podłączonych klientów.", 8.85, 3.35, 3.35, 1.35, C.cyan);
  bullets(s, [
    "Publisher nie wie, która implementacja jest aktualnie wykonywana.",
    "Każdy handler ma wspólny kontrakt, ale inne zadanie.",
  ], 6.75, 1.45, 4.9, 1.3, { fontSize: 12.4 });
  addFooter(s, 7, "Strategy / Handler");

  s = pptx.addSlide();
  title(s, "Factory Method", "BookingEntityFactory centralizuje tworzenie encji");
  add_code_slide(s,
    "public BookingEntity Create(EntityKind kind) => kind switch\n" +
    "{\n" +
    "    EntityKind.Customer => new Customer(),\n" +
    "    EntityKind.Hairdresser => new Hairdresser(),\n" +
    "    EntityKind.SalonService => new SalonService(),\n" +
    "    EntityKind.Appointment => new Appointment(),\n" +
    "    EntityKind.SalonPhoto => new SalonPhoto(),\n" +
    "    EntityKind.AppUser => new AppUser(),\n" +
    "    _ => throw new ArgumentOutOfRangeException(...)\n" +
    "};", 0.8, 1.35, 6.15, 4.05);
  card(s, "Po co?", "Tworzenie encji domenowych jest w jednym miejscu. Łatwiej dodać nowy typ i utrzymać spójność modeli.", 7.55, 1.65, 4.2, 1.55, C.green);
  card(s, "Przykładowe encje", "Customer, Hairdresser, SalonService, Appointment, SalonPhoto, AppUser.", 7.55, 3.75, 4.2, 1.15, C.blue);
  addFooter(s, 8, "Factory Method");

  s = pptx.addSlide();
  title(s, "Adapter / Wrapper", "Integracje Azure są ukryte za interfejsami aplikacji");
  addTwoColumnTable(s, [
    ["IPhotoStorageService", "BlobPhotoStorageService ukrywa BlobContainerClient i upload plików."],
    ["ICurrentUserService", "EasyAuthCurrentUserService ukrywa nagłówki Easy Auth."],
    ["IEmailSender", "AzureCommunicationEmailSender ukrywa EmailClient i ACS Email."],
  ], 0.8, 1.4, 6.1, 0.75);
  card(s, "Korzyść", "Gdyby technologia zewnętrzna się zmieniła, reszta aplikacji nadal mogłaby używać tego samego interfejsu.", 7.55, 1.55, 4.25, 1.65, C.orange);
  add_code_slide(s,
    "public interface IPhotoStorageService\n{\n    Task<UploadedPhoto> UploadAsync(...);\n    Task DeleteAsync(...);\n}", 7.55, 3.85, 4.25, 1.15);
  addFooter(s, 9, "Adapter");

  s = pptx.addSlide();
  title(s, "Dependency Injection i Singleton", "Wzorce spinające całą aplikację");
  add_code_slide(s,
    "services.AddScoped<IAppointmentBookingFacade, AppointmentBookingFacade>();\n" +
    "services.AddScoped<IBookingEventPublisher, BookingEventPublisher>();\n" +
    "services.AddScoped(typeof(IBookingRepository<>), typeof(CosmosBookingRepository<>));\n" +
    "services.AddSingleton(_ => new CosmosClient(options.Cosmos.ConnectionString));\n" +
    "services.AddSingleton<ICosmosContainerResolver, CosmosContainerResolver>();", 0.8, 1.45, 6.6, 2.35);
  bullets(s, [
    "Klasy dostają zależności przez konstruktory.",
    "Implementacje można podmieniać przez interfejsy.",
    "CosmosClient jest singletonem, bo powinien być współdzielony.",
    "DI rejestruje też wiele handlerów tego samego interfejsu.",
  ], 8.0, 1.55, 3.85, 2.85, { fontSize: 12.2 });
  addFooter(s, 10, "Dependency Injection / Singleton");

  s = pptx.addSlide();
  title(s, "Iterator i Options Pattern", "Mniejsze wzorce wspierające czytelność kodu");
  card(s, "Iterator", "AvailabilitySlotCollection implementuje IEnumerable<DateTimeOffset>, dzięki czemu sloty wizyt można filtrować LINQ.", 0.9, 1.55, 5.0, 1.7, C.green);
  add_code_slide(s,
    "public sealed class AvailabilitySlotCollection : IEnumerable<DateTimeOffset>\n{\n    public IEnumerator<DateTimeOffset> GetEnumerator() => _slots.GetEnumerator();\n}", 0.9, 3.7, 5.0, 1.05);
  card(s, "Options Pattern", "AzureBookingOptions i EmailOptions mapują konfigurację z appsettings lub zmiennych Azure na klasy C#.", 6.8, 1.55, 5.0, 1.7, C.blue);
  add_code_slide(s,
    "services.Configure<AzureBookingOptions>(configuration.GetSection(\"Azure\"));\nservices.Configure<EmailOptions>(context.Configuration.GetSection(\"Email\"));", 6.8, 3.7, 5.0, 0.85);
  addFooter(s, 11, "Iterator / Options Pattern");

  s = pptx.addSlide();
  title(s, "Jak wzorce współpracują", "Przepływ POST /api/Appointments");
  flowNode(s, "Controller\nDI", 0.65, 2.1, 1.25, C.blue);
  arrow(s, 2.0, 2.38, 0.45);
  flowNode(s, "Facade\nBookAsync", 2.55, 2.1, 1.45, C.green);
  arrow(s, 4.1, 2.38, 0.45);
  flowNode(s, "Repository\nCosmos", 4.65, 2.1, 1.45, C.navy);
  arrow(s, 6.2, 2.38, 0.45);
  flowNode(s, "Observer\nPublisher", 6.75, 2.1, 1.45, C.orange);
  arrow(s, 8.3, 2.38, 0.45);
  flowNode(s, "Handlers\nStrategy", 8.85, 2.1, 1.45, C.cyan);
  arrow(s, 10.4, 2.38, 0.45);
  flowNode(s, "Azure\nAdapters", 10.95, 2.1, 1.35, C.red);
  card(s, "Wniosek", "Każdy wzorzec ma konkretne miejsce w procesie: od requestu HTTP, przez logikę biznesową, po reakcje chmurowe.", 1.15, 4.1, 10.35, 1.25, C.green);
  addFooter(s, 12, "Przepływ wzorców");

  s = pptx.addSlide();
  title(s, "Co powiedzieć prowadzącemu", "Krótka odpowiedź obronna");
  bullets(s, [
    "Facade: kontroler wywołuje BookAsync, a proces rezerwacji jest ukryty w jednej klasie.",
    "Repository: kontrolery nie znają szczegółów Cosmos DB.",
    "Observer/Event: po utworzeniu wizyty wiele komponentów może zareagować niezależnie.",
    "Strategy/Handler: Blob, Queue i SignalR mają ten sam interfejs, ale różne zachowania.",
    "Factory Method: tworzenie encji domenowych jest scentralizowane.",
    "Adapter: szczegóły Azure SDK są ukryte za interfejsami aplikacji.",
  ], 0.95, 1.35, 9.8, 4.7, { fontSize: 13.8 });
  addFooter(s, 13, "Odpowiedzi na pytania");

  s = pptx.addSlide();
  title(s, "Podsumowanie", "Wzorce porządkują projekt, a nie tylko ozdabiają kod");
  card(s, "Czytelność", "Kontrolery pozostają krótkie i odpowiadają za HTTP.", 0.9, 1.55, 3.4, 1.35, C.blue);
  card(s, "Rozszerzalność", "Nową reakcję po wizycie można dodać jako nowy handler.", 4.95, 1.55, 3.4, 1.35, C.green);
  card(s, "Integracje", "Azure jest ukryty za adapterami i konfiguracją Options.", 9.0, 1.55, 3.15, 1.35, C.orange);
  slideNote(s, "Najważniejszy przekaz: wzorce rozdzielają odpowiedzialności i ułatwiają rozwój aplikacji.");
  addFooter(s, 14, "Podsumowanie");

  await pptx.writeFile({ fileName: path.join(ROOT, "Prezentacja_2_Wzorce_Projektowe_HairSalonBooking.pptx") });
}

function add_code_slide(slide, text, x, y, w, h) {
  slide.addShape("roundRect", {
    x, y, w, h,
    rectRadius: 0.04,
    fill: { color: "0B1220" },
    line: { color: "0B1220" },
  });
  slide.addText(text, {
    x: x + 0.18, y: y + 0.18, w: w - 0.36, h: h - 0.32,
    fontFace: "Cascadia Mono",
    fontSize: 8.6,
    color: "D1FAE5",
    margin: 0,
    fit: "shrink",
    breakLine: false,
  });
}

(async () => {
  await buildAzureDeck();
  await buildPatternsDeck();
  console.log("Created PPTX decks.");
})();
