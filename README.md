# FoodOrdering Platform

A multi-tenant food ordering platform built with **ASP.NET Core 10 Web API** and **Angular 20**.

---

## 🛠️ Tech Stack & Cloud Architecture

| Layer | Technology | Hosting | Cost |
| :--- | :--- | :--- | :--- |
| **Frontend** | Angular 20, Tailwind CSS, SignalR Client | [Cloudflare Pages](https://pages.cloudflare.com) | Free |
| **Backend API** | .NET 10 Web API, EF Core, SignalR, Scalar API | [Railway](https://railway.com) | ~$2-$5 / mo |
| **Database** | PostgreSQL 16+ | [Supabase](https://supabase.com) | Free tier |

---

## 📖 Deployment Documentation

For the complete architectural diagrams, configurations, and step-by-step instructions to set up Staging and Production from scratch, see:

👉 **[docs/DEPLOYMENT_PLAYBOOK.md](docs/DEPLOYMENT_PLAYBOOK.md)**

---

## 🚀 Running Locally

### 1. Start Backend API
```powershell
dotnet run --project src/FoodOrdering.Api
```
* API listens on `http://localhost:5230`
* Interactive API documentation: `http://localhost:5230/scalar/v1`

### 2. Start Frontend Web
```powershell
cd src/FoodOrdering.Web
npm start
```
* Web app opens at `http://localhost:4200`
