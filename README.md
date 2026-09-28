# Time Capture — testable mock (not Aderant Expert)

Small **C# / ASP.NET Core** demo of a law-firm time-entry flow. Built to practise what a Test Automation intern would automate: **wrong matter = wrong invoice**, duplicate save under latency, PA restrictions, bad integration IDs.

This is a **mock target** plus **automated tests**. It is not Aderant Expert and not a production billing product.

## What to look at (GitHub)

| Path | What it is |
|---|---|
| `src/TimeCapture.Web/` | Server + web form (`data-testid` hooks) |
| `src/TimeCapture.Desktop/` | Tiny **WPF** window; Save posts to the same API |
| `tests/TimeCapture.Tests/` | Playwright UI + HTTP API tests (NUnit) |
| `tests/TimeCapture.Desktop.Tests/` | FlaUI finds `AutomationId` and clicks Save |

```text
Lawyer UI  ──►  POST /api/entries  ──►  TimeEntryStore (rules)
  web form         same door              reject bad bills
  WPF window
```

## Run

Web (needs a browser):

```powershell
dotnet run --project src/TimeCapture.Web
```

Open http://localhost:5288

Desktop + server: start the web command first, then:

```powershell
dotnet run --project src/TimeCapture.Desktop
```

Tests (stop `dotnet run` first so the `.exe` is not locked):

```powershell
dotnet test
```

If Playwright browsers are missing:

```powershell
powershell -File tests/TimeCapture.Tests/bin/Debug/net10.0/playwright.ps1 install
```

## Tests in one glance

**LearnChain** (`backend.Tests`, xUnit): unit tests on XP / due dates; controller tests against a test database. Product is a habit app.

**This repo** (NUnit): invoice-risk paths on a fake time-entry API + Playwright on the form + one WPF click-through.

Same language family (**C# / .NET 10**). Different question: LearnChain asks “did the habit logic work?”; this repo asks “would finance invoice the wrong client?”

## Interview one-liner

Web hooks: `data-testid`. Desktop hooks: `AutomationProperties.AutomationId` (same names). Expert in a firm would use **Leapwork** on those AutomationIds. I have not used Leapwork in production.
