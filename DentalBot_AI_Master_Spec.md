# DentalBot AI - Especificación Funcional y Técnica (MVP → SaaS)

## Visión

Construir una plataforma SaaS para automatizar la atención por WhatsApp
de clínicas dentales mediante Evolution API, un backend .NET y un
asistente de IA local (Ollama + Qwen).

## Objetivos del MVP

-   Agenda de citas.
-   Gestión de pacientes.
-   Configuración completa de la clínica.
-   Integración con Evolution API.
-   IA para responder preguntas y asistir en el agendamiento.
-   Panel administrativo moderno.

## Stack

### Frontend

-   React
-   Vite
-   TypeScript
-   Tailwind CSS
-   React Router
-   React Query
-   React Hook Form
-   Zod

### Backend

-   ASP.NET Core 9
-   Entity Framework Core
-   SQLite (MVP)
-   JWT
-   FluentValidation
-   BackgroundService
-   Swagger

### IA

-   Ollama
-   Qwen3 4B

### Mensajería

-   Evolution API

## Arquitectura

Frontend -\> API -\> Application -\> Domain -\> Infrastructure

Servicios: - WhatsAppService - AIService - AppointmentService -
PatientService - NotificationService

## Módulos

1.  Autenticación
2.  Dashboard
3.  Clínica
4.  Sucursales
5.  Doctores
6.  Horarios
7.  Servicios
8.  Pacientes
9.  Agenda
10. Conversaciones
11. IA
12. WhatsApp
13. Reportes
14. Configuración

## Entidades principales

-   Company
-   Branch
-   User
-   Role
-   Doctor
-   Patient
-   Service
-   Appointment
-   Conversation
-   Message
-   KnowledgeArticle
-   BusinessHour
-   Holiday
-   AISettings
-   WhatsAppInstance

## Roles

-   Administrador
-   Recepción
-   Doctor
-   Solo lectura

## Flujo del bot

Paciente -\> Evolution API -\> Webhook -\> API

La API: 1. Guarda mensaje. 2. Busca paciente. 3. Consulta contexto. 4.
Decide si requiere IA. 5. Ejecuta funciones. 6. Devuelve respuesta.

## Reglas de IA

La IA: - Interpreta lenguaje natural. - Redacta respuestas. - Detecta
intención. - Detecta urgencias.

Nunca: - Inventa horarios. - Inventa precios. - Crea citas directamente.

Debe usar herramientas: - BuscarHorarios - CrearCita - ReagendarCita -
CancelarCita - BuscarPaciente - ConsultarServicios - ConsultarFAQ -
TransferirHumano

## Configuración editable

-   Horarios
-   Servicios
-   Precios
-   Métodos de pago
-   Promociones
-   Preguntas frecuentes
-   Mensajes automáticos

## Roadmap

### Fase 1

-   Login
-   Dashboard
-   Clínica
-   Horarios
-   Servicios
-   Pacientes

### Fase 2

-   Agenda
-   WhatsApp
-   Conversaciones

### Fase 3

-   IA local
-   FAQ
-   Detección de intención

### Fase 4

-   Recordatorios
-   Encuestas
-   Estadísticas

### Fase 5

-   Multiempresa
-   Suscripciones
-   Facturación

## Estructura Backend

/src - Api - Application - Domain - Infrastructure - Shared

## Estructura Frontend

/src - components - pages - layouts - hooks - services - store -
routes - types - utils

## Principios

-   SOLID
-   Clean Code
-   DTOs
-   Repository Pattern
-   Dependency Injection
-   Interfaces
-   Validaciones
-   Paginación
-   Soft Delete
-   Auditoría

## Instrucciones para la IA desarrolladora

Construye el proyecto por módulos. No avances al siguiente módulo hasta
terminar el actual. Explica cada decisión técnica. Genera código limpio,
documentado y listo para producción. Prioriza componentes reutilizables
y una arquitectura preparada para SaaS.
