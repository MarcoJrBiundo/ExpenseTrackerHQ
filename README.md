# ExpenseTrackerHQ

**ExpenseTrackerHQ** is a cloud-native, production-style expense tracking platform built as a **portfolio-grade system** to demonstrate modern backend development, Kubernetes deployment, and Azure infrastructure using Infrastructure as Code.

This repository intentionally mirrors how real-world teams structure and operate cloud platforms — it is **not** a toy project.

---

## Project Goals

This project is designed to showcase:

- Clean, maintainable backend clean architecture (.NET 8)
- Containerized local development with Docker
- Kubernetes fundamentals (local + cloud - AKS)
- Helm-based application deployment
- Azure infrastructure provisioning using Terraform
- Secure cloud patterns (private networking, managed identity)
- A scalable monorepo layout suitable for multi-service systems

---

## High-Level Architecture

ExpenseTrackerHQ consists of the following layers:

### Backend API
- .NET 8 Web API
- Clean Architecture (Domain / Application / Infrastructure / API)
- CQRS-ready design
- Containerized for Docker and Kubernetes

### Frontend Applications
- Multiple React clients
- One primary hand-built UI
- Additional AI-generated UI variants for comparison and experimentation

### Platform & Infrastructure
- Docker & Docker Compose (local dev)
- Kubernetes (Minikube locally, AKS in Azure)
- Helm for application deployment
- Terraform for Azure infrastructure (AKS, ACR, SQL, networking)

### Cloud Provider
- Microsoft Azure
- Azure Kubernetes Service (AKS)
- Azure Container Registry (ACR)
- Azure SQL with Private Endpoints
- Managed Identity for authentication

---

## Repository Structure (Monorepo)

```text
ExpenseTrackerHQ/
├── apps/
│   ├── api/                 # .NET backend (ExpenseTracker.Api, Domain, Infra)
│   ├── web-main/            # Primary React frontend
│   ├── web-gemini-a/        # AI-generated frontend variant
│   └── web-gemini-b/        # AI-generated frontend variant
│
├── infra/
│   ├── helm/                # Helm charts for Kubernetes deployments
│   ├── terraform/           # Azure IaC (AKS, ACR, SQL, networking)
│   └── k8s/                 # Optional raw Kubernetes manifests (local testing)
│
├── ops/
│   ├── postman/             # Postman collections for API testing
│   ├── sql/                 # SQL scripts, stored procedures, seed data
│   └── runbooks/            # Operational notes and procedures
│
├── docs/
│   ├── architecture/        # Architecture diagrams & explanations
│   ├── phase-1/             # API & local infrastructure notes
│   ├── phase-2/             # Kubernetes & Helm notes
│   ├── phase-3/             # Azure & Terraform notes
│   └── terraform/           # Terraform learning & reference material
│
├── scripts/                 # Helper scripts (local dev, CI/CD)
└── README.md