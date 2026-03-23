# Deck Duel 🃏⚔️

**Deck Duel** is a full-stack demo application built to showcase modern development practices across frontend, backend, real-time communication, cloud infrastructure, and AI integration.

The project is designed as a portfolio piece while pursuing senior developer roles, demonstrating clean architecture, CI/CD, and scalable cloud deployment.

---

## 🎮 Overview

Deck Duel is a **Top Trumps-style card game** where players compete using dynamically generated decks.

What makes it unique is the use of **AI-generated decks** — users provide a prompt, and the system generates themed card decks on the fly.

---

## 🚀 Features

* AI-generated card decks based on user prompts
* Real-time game updates using SignalR
* Full-stack architecture (React + .NET + SQL Server)
* Cloud-native deployment on Azure
* Automated CI/CD pipelines via GitHub Actions
* Unit tested backend

---

## 🏗️ Architecture

### Frontend

* React (with Vite)
* Chakra UI
* Built in VS Code

### Backend

* .NET 10 Minimal API
* Entity Framework Core (Model-first approach)
* Built in Visual Studio

### Database

* SQL Server

### Real-time Communication

* SignalR for live game state updates

### Testing

* .NET Unit Test project

---

## ☁️ Azure Deployment

| Component | Azure Service                           |
| --------- | --------------------------------------- |
| Client    | Azure Static Web Apps                   |
| API       | Azure App Service                       |
| Database  | Azure SQL Database                      |
| AI        | Azure OpenAI Service + Azure AI Foundry |

---

## 🤖 AI Integration

Deck Duel uses Azure AI services to:

* Generate themed card decks from user prompts
* Dynamically create game content
* Enhance replayability and creativity

---

## 🔄 CI/CD

The project uses GitHub Actions workflows to:

* Build frontend and backend projects
* Run tests
* Deploy to Azure automatically

---

## 📁 Repository Structure

```
DeckDuel/
│
├── DeckDuelClient/   # React frontend
├── DeckDuelAPI/      # .NET API
├── database/         # SQL scripts / models
├── .github/workflows # CI/CD pipelines
└── README.md
```

---

## 🧠 What This Project Demonstrates

* Full-stack development across modern technologies
* Clean separation of concerns (client, API, data)
* Real-time application design
* Cloud deployment and infrastructure design
* CI/CD pipeline implementation
* AI integration into a production-style app

---

## 📌 Future Enhancements

* Persistent game history
* Player chat.
* Performance optimisation and scaling
* Multiple environments (dev/staging/prod)

---

## 🔗 Repository

GitHub: https://github.com/ChrisHeaps/DeckDuel

---

## 👤 Author

Chris Heaps
Senior Software Developer (seeking opportunities)

---

## 💬 Summary

Deck Duel is a modern, cloud-based, AI-powered demo application that brings together frontend, backend, real-time systems, and cloud services into a cohesive and production-style solution.
