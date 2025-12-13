 Smart Expense Tracker — Project Overview

📌 Purpose

Smart Expense Tracker is a cloud-native, production-grade portfolio project designed to showcase modern backend engineering, DevOps, and Azure platform skills.
The goal is to build an enterprise-quality system demonstrating:
	•	Clean Architecture in .NET 8
	•	Containerization & local Kubernetes
	•	IaC with Terraform
	•	AKS deployment
	•	Azure SQL + APIM + Key Vault
	•	Full observability with OpenTelemetry
	•	Azure DevOps pipelines & automation
	•	Secure authentication with Azure AD B2C

This project mirrors the patterns used in large-scale companies and is designed to be a centerpiece portfolio piece for senior developer or cloud engineering roles.

⸻

🗺️ High-Level Architecture

The system will eventually include:
	•	Backend API (ASP.NET Core 8 Web API, Clean Architecture)
	•	React Frontend (TypeScript + MSAL for B2C login)
	•	Azure SQL Database
	•	Containers (Docker) and Local Kubernetes (Minikube/Docker Desktop)
	•	Azure Kubernetes Service (AKS)
	•	API Management (APIM)
	•	Key Vault + Managed Identity
	•	Terraform IaC
	•	OpenTelemetry + Application Insights
	•	Azure DevOps CI/CD pipelines

⸻

📦 Core Functional Scope

The eventual full application will support:

✔ Expense Tracking (CRUD)
	•	Add expenses
	•	Get all expenses for a user
	•	Categorize spending
	•	Summaries & filtering (date range, category)

✔ Authentication (later phase)
	•	Azure AD B2C authentication
	•	Secure API endpoints
	•	User isolation / multi-tenant mindset

✔ Observability
	•	Distributed tracing
	•	Structured logging
	•	Metrics
	•	Cloud dashboards

✔ Deployment Ready
	•	Full Docker Compose environment
	•	Helm charts for AKS
	•	Automated Azure DevOps pipelines

⸻

🧩 Project Roadmap (Phases)

This project is structured into 6 major phases, each with its own epics, features, stories, and tasks.

⸻

Phase 1 — Backend Foundations (CURRENT PHASE)

Goal: Build a Clean Architecture .NET 8 API running locally.

Includes:
	•	Clean Architecture (Domain → Application → Infrastructure → API)
	•	Light DDD patterns (entities, basic validation, mapping)
	•	EF Core async operations + repository pattern
	•	SQL Server integration (local)
	•	Storage via stored procedures
	•	Health checks & structured logging
	•	Full Docker Compose environment (API + SQL Server)

This phase sets the foundation for all future cloud-native work.

⸻

Phase 2 — Containerization & Local Kubernetes

Goal: Package backend into containers and deploy to a local K8s cluster.

Includes:
	•	Dockerfile (multi-stage)
	•	Kubernetes manifests or Helm charts
	•	Deployments, Services, Secrets, ConfigMaps
	•	Local dev namespace
	•	Ingress routing

⸻

Phase 3 — Azure Infrastructure & AKS

Goal: Deploy API to Azure Kubernetes Service using Terraform.

Includes:
	•	Terraform modules for:
	•	VNet, Subnets
	•	AKS Cluster
	•	ACR
	•	Azure SQL
	•	Storage for Terraform state
	•	Deploy backend container images to AKS
	•	Secure connections & networking

⸻

Phase 4 — Security, Identity & API Management

Goal: Add modern cloud security layers.

Includes:
	•	Azure AD B2C for authentication
	•	APIM for API protection and routing
	•	Key Vault + Managed Identity
	•	No plaintext secrets
	•	JWT-protected API endpoints

⸻

Phase 5 — Observability & Reliability

Goal: Full enterprise-grade observability pipeline.

Includes:
	•	OpenTelemetry for distributed traces
	•	Metrics, logs, correlation
	•	Application Insights integration
	•	Dashboards and alerting
	•	K8s cluster logs & metrics

⸻

Phase 6 — Frontend & User Experience

Goal: Build a production-grade React frontend.

Includes:
	•	React + TS + Vite
	•	MSAL integration for B2C login
	•	Expense UI
	•	Backend calls via Axios
	•	Basic dashboards/graphs

```yaml
Project: SmartExpenseTracker
Timeline: Nov 24 2025 – Oct 10 2026
Sprints:
  - Sprint 1: { dates: "Nov 24 2025 – Dec 4 2025", phase: Phase 1 }
  - Sprint 2.1: { dates: "Dec 5 2025 – Dec 18 2025", phase: Phase 2 }
  - Sprint 2.2: { dates: "Dec 19 2025 – Jan 2 2026", phase: Phase 2 }
  - Sprint 2.3: { dates: "Jan 3 2026 – Jan 18 2026", phase: Phase 2 }
  - Sprint 3.1: { dates: "Jan 19 2026 – Feb 9 2026", phase: Phase 3 }
  - Sprint 3.2: { dates: "Feb 10 2026 – Mar 3 2026", phase: Phase 3 }
  - Sprint 3.3: { dates: "Mar 4 2026 – Mar 27 2026", phase: Phase 3 }
  - Sprint 3.4: { dates: "Mar 28 2026 – Apr 15 2026", phase: Phase 3 }
  - Sprint 4.1: { dates: "Apr 16 2026 – May 10 2026", phase: Phase 4 }
  - Sprint 4.2: { dates: "May 11 2026 – Jun 3 2026", phase: Phase 4 }
  - Sprint 4.3: { dates: "Jun 4 2026 – Jul 5 2026", phase: Phase 4 }
  - Sprint 5.1: { dates: "Jul 6 2026 – Jul 23 2026", phase: Phase 5 }
  - Sprint 5.2: { dates: "Jul 24 2026 – Aug 10 2026", phase: Phase 5 }
  - Sprint 6.1: { dates: "Aug 11 2026 – Aug 29 2026", phase: Phase 6 }
  - Sprint 6.2: { dates: "Aug 30 2026 – Sep 20 2026", phase: Phase 6 }
  - Sprint 6.3: { dates: "Sep 21 2026 – Oct 10 2026", phase: Phase 6 }

Epics:

  # ======================
  # PHASE 1 — BACKEND
  # ======================
  - Epic: Phase 1 – Backend Foundations
    Sprint: Sprint 1
    Description: >
      Build .NET 8 API using Clean Architecture, EF Core, local SQL, and Docker Compose.

    Features:

      - Feature: Clean Architecture & Core Domain
        Sprint: Sprint 1
        Stories:
          - Story: Setup Clean Architecture project
            Sprint: Sprint 1
            Tasks:
              - Create solution and projects
              - Add references
              - Configure appsettings
              - Add logging + health checks

          - Story: Define Core Domain
            Sprint: Sprint 1
            Tasks:
              - Create Expense entity
              - Create BaseEntity
              - Define IExpenseRepository interface

          - Story: Application Layer Setup
            Sprint: Sprint 1
            Tasks:
              - Add DTOs
              - Add Validators
              - Add AutoMapper profiles

          - Story: API Endpoints
            Sprint: Sprint 1
            Tasks:
              - Add ExpenseController
              - GET /expenses/{userId}
              - POST /expenses

      - Feature: EF Core Integration
        Sprint: Sprint 1
        Stories:
          - Story: Setup EF Core
            Sprint: Sprint 1
            Tasks:
              - Add DbContext
              - Configure DbSet
              - Add InMemory provider

          - Story: Implement Repository
            Sprint: Sprint 1
            Tasks:
              - Add ExpenseRepository
              - Async methods
              - Unit tests

          - Story: Async + Cancellation Tokens
            Sprint: Sprint 1
            Tasks:
              - Refactor methods
              - Pass CancellationToken
              - Test cancellation behavior

      - Feature: Local SQL + Stored Procedures
        Sprint: Sprint 1
        Stories:
          - Story: Configure local SQL Server
            Tasks:
              - Create DB
              - Create table

          - Story: Create Stored Procs
            Tasks:
              - sp_AddExpense
              - sp_GetExpensesByUser

          - Story: Integrate Stored Procs
            Tasks:
              - FromSqlRaw
              - Validate mapping

      - Feature: Docker Compose Environment
        Sprint: Sprint 1
        Stories:
          - Story: Containerize API
            Tasks:
              - Multi-stage Dockerfile

          - Story: Setup Docker Compose
            Tasks:
              - Services: api, sql
              - Ports, volumes, env vars

          - Story: Verify Local Environment
            Tasks:
              - Run compose
              - Validate endpoints
              - Document setup

  # ======================
  # PHASE 2 — K8S & HELM
  # ======================
  - Epic: Phase 2 – Kubernetes & Helm
    Description: >
      Deploy API to local Kubernetes using Deployments, Services, ConfigMaps, Secrets, Helm, and Ingress.

    Features:

      - Feature: Kubernetes Fundamentals
        Sprint: Sprint 2.1
        Stories:
          - Story: Install Cluster + Namespace
            Tasks:
              - Install Minikube
              - Create namespace

          - Story: Deployment & Service
            Tasks:
              - Deployment (2 replicas)
              - Liveness + readiness probes
              - Service (ClusterIP)

      - Feature: ConfigMaps & Secrets
        Sprint: Sprint 2.2
        Stories:
          - Story: Create ConfigMap
            Tasks:
              - Externalize DB name, env vars

          - Story: Create Secrets
            Tasks:
              - Store connection string

          - Story: Inject into Deployment
            Tasks:
              - Env vars
              - Validate pod startup

      - Feature: Helm + Ingress
        Sprint: Sprint 2.3
        Stories:
          - Story: Create Helm Chart
            Tasks:
              - helm create
              - Move templates
              - values.yaml

          - Story: Helm Deploy + Rollback
            Tasks:
              - helm upgrade/install
              - helm history

          - Story: Ingress Setup
            Tasks:
              - Install NGINX ingress controller
              - Add Ingress resource
              - Configure expense.local

  # ======================
  # PHASE 3 — AZURE INFRA
  # ======================
  - Epic: Phase 3 – Azure Infra & AKS
    Description: >
      Provision Azure using Terraform: VNet, Subnets, ACR, AKS, SQL, Private Link. Deploy API to AKS via Helm.

    Features:

      - Feature: Terraform Core + Networking
        Sprint: Sprint 3.1
        Stories:
          - Story: Terraform Backend
            Tasks:
              - Storage account
              - Remote state

          - Story: Networking
            Tasks:
              - VNet
              - Subnets
              - NSGs

      - Feature: ACR + AKS
        Sprint: Sprint 3.2
        Stories:
          - Story: Create ACR
            Tasks:
              - Registry creation
              - Push API image

          - Story: AKS Cluster
            Tasks:
              - Cluster deployment
              - Node pools
              - Managed identity

      - Feature: AKS Deployment with Helm
        Sprint: Sprint 3.3
        Stories:
          - Story: Prepare Helm for AKS
            Tasks:
              - Update image repo
              - Set resources

          - Story: Deploy to AKS
            Tasks:
              - helm upgrade --install
              - Verify pods + service + ingress

      - Feature: Azure SQL + Private Endpoint
        Sprint: Sprint 3.4
        Stories:
          - Story: Provision SQL
            Tasks:
              - SQL Server + DB

          - Story: Private Link
            Tasks:
              - Create Private Endpoint
              - Private DNS zone
              - Validate AKS→SQL access

  # ======================
  # PHASE 4 — SECURITY
  # ======================
  - Epic: Phase 4 – Security, Key Vault, APIM, B2C
    Description: >
      Secure workloads with Key Vault, Managed Identity, Azure AD B2C, and APIM.

    Features:

      - Feature: Key Vault + Managed Identity
        Sprint: Sprint 4.1
        Stories:
          - Story: Provision KV
            Tasks:
              - Create KV
              - Store secrets

          - Story: Configure MI for AKS
            Tasks:
              - Assign KV roles
              - Update app to load secrets

      - Feature: Azure AD B2C Authentication
        Sprint: Sprint 4.2
        Stories:
          - Story: Setup B2C Tenant
            Tasks:
              - User flows
              - App registrations

          - Story: Protect API with JWT
            Tasks:
              - Add JwtBearer
              - Validate tokens

      - Feature: API Management Integration
        Sprint: Sprint 4.3
        Stories:
          - Story: Provision APIM
            Tasks:
              - Developer tier
              - Diagnostics

          - Story: Import API
            Tasks:
              - Import OpenAPI
              - Configure backend

          - Story: Apply APIM Policies
            Tasks:
              - JWT validation
              - Rate limiting
              - Correlation ID

  # ======================
  # PHASE 5 — OBSERVABILITY
  # ======================
  - Epic: Phase 5 – Observability
    Description: >
      Full OpenTelemetry setup: tracing, metrics, logs, dashboards, alerts.

    Features:

      - Feature: OpenTelemetry Integration
        Sprint: Sprint 5.1
        Stories:
          - Story: Add OTEL SDK
            Tasks:
              - Tracing provider
              - Metrics provider

          - Story: Correlation + Logging
            Tasks:
              - Correlation ID middleware
              - Log enrichment

      - Feature: Dashboards + Alerts
        Sprint: Sprint 5.2
        Stories:
          - Story: Build dashboards
            Tasks:
              - KQL queries
              - Latency charts

          - Story: Alerts
            Tasks:
              - CPU
              - Errors
              - Restart loops

  # ======================
  # PHASE 6 — FRONTEND & CI/CD
  # ======================
  - Epic: Phase 6 – Frontend & CI/CD
    Description: >
      Build React SPA with B2C login, integrate with APIM, deploy, and document.

    Features:

      - Feature: React Frontend
        Sprint: Sprint 6.1
        Stories:
          - Story: React Setup
            Tasks:
              - Vite + TS
              - Tailwind

          - Story: UI Components
            Tasks:
              - Expense list
              - Add expense

      - Feature: B2C Integration
        Sprint: Sprint 6.2
        Stories:
          - Story: MSAL Auth
            Tasks:
              - Login/logout
              - Acquire tokens

          - Story: Protected Routing
            Tasks:
              - Token enforcement
              - Claims UI

      - Feature: CI/CD + Documentation
        Sprint: Sprint 6.3
        Stories:
          - Story: CI/CD Pipelines
            Tasks:
              - Build pipeline
              - Deploy pipeline

          - Story: Docs + Architecture
            Tasks:
              - System diagram
              - Infra diagram
              - README + RUNBOOK



```