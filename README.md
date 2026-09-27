# TISW_TUSSI_ERP

ERP académico desarrollado en **.NET MAUI** con patrón **MVVM**, base de datos
**MySQL** en Docker y módulos de Ventas, Compras, Inventario y Contabilidad.

## Estructura del repositorio

```
TISW_TUSSI_ERP/                     (raíz de la solución)
│
├── docker-compose.yml              # Levanta MySQL + Adminer
├── DbScripts/
│   └── init.sql                    # Tablas y datos
│
└── TISW_TUSSI_ERP/                  # Proyecto .NET MAUI
    ├── Models/
    │   ├── Auth/                   # Usuario.cs, Sesion.cs
    │   ├── Compras/
    │   ├── Ventas/
    │   ├── Inventario/
    │   └── Contabilidad/
    ├── ViewModels/
    │   ├── BaseViewModel.cs
    │   ├── Auth/                   # LoginViewModel.cs
    │   ├── Dashboard/               # DashboardViewModel.cs
    │   ├── Compras/ Ventas/ Inventario/ Contabilidad/
    ├── Views/
    │   ├── Auth/                   # LoginPage.xaml
    │   ├── Dashboard/               # DashboardPage.xaml
    │   ├── Compras/ Ventas/ Inventario/ Contabilidad/
    ├── Services/
    │   ├── Api/                    # DatabaseService.cs (conexión MySQL)
    │   └── Navigation/              # NavigationService.cs
    ├── App.xaml / App.xaml.cs
    ├── AppShell.xaml / AppShell.xaml.cs
    ├── MauiProgram.cs
    └── TISW_TUSSI_ERP.csproj
```

## 1. Levantar la base de datos (MySQL con Docker)

Requisitos: tener [Docker Desktop](https://www.docker.com/products/docker-desktop/)
instalado y corriendo.

```bash
# Desde la raíz del repositorio (donde está docker-compose.yml)
docker compose up -d
```

Esto crea:
- Un contenedor MySQL 8 en el puerto **3306**, con la base `erp_farmaceutico`
  (esquema completo: seguridad, clientes/convenios, productos/lotes,
  contabilidad, compras, ventas/cobranzas e inventario/mermas).
- Un contenedor **Adminer** (administrador visual) accesible en
  [http://localhost:8080](http://localhost:8080) — servidor `db_tussi`,
  usuario `erp_admin`, contraseña `admin_password`.
- Ejecuta automáticamente `DbScripts/init.sql` la primera vez que se crea
  el volumen, creando las tablas y un usuario de prueba:
  - correo: `admin@erp.cl`
  - contraseña: `admin123`

Para apagarlo: `docker compose down` (agrega `-v` si además quieres borrar los datos).

## 2. Abrir el proyecto MAUI

1. Abre `TISW_TUSSI_ERP.sln` en Visual Studio 2022 (con la carga de trabajo
   **.NET Multi-platform App UI development** instalada).
2. Verifica que `Services/Api/DatabaseService.cs` apunte a la conexión correcta:
   - Windows / macOS: `Server=localhost;...`
   - Emulador de Android: cambia `localhost` por `10.0.2.2`.
3. Ejecuta el proyecto (F5) eligiendo el destino Windows o Android.

## 3. Flujo de la aplicación

- La app siempre arranca en `LoginPage`.
- Al validar las credenciales contra MySQL, se reemplaza `MainPage` por
  `AppShell`, que contiene el `DashboardPage` y, a futuro, un `FlyoutItem`
  por cada módulo (Ventas, Compras, Inventario, Contabilidad).
- Cada integrante del equipo trabaja dentro de su propia subcarpeta de
  `Models`, `ViewModels`, `Views` y registra sus páginas en
  `MauiProgram.cs` y sus rutas en `AppShell.xaml.cs` / `AppShell.xaml`.

## 4. Convenciones de trabajo en equipo

- No modifiques la subcarpeta de módulo de otro integrante.
- Todo servicio de acceso a datos nuevo debe reutilizar `DatabaseService`
  (o seguir el mismo patrón) para no duplicar cadenas de conexión.
- Antes de hacer *push*, corre `docker compose up -d` y prueba tu módulo
  contra la base de datos real.
