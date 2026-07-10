# AGENTS.md

Guia de trabajo para Codex en el proyecto **Backup SQL Server**.

## Objetivo

Construir una aplicacion de consola en **C# / .NET 10 LTS** que:

- Detecte backups de SQL Server en el servidor local.
- Copie esos backups a un almacenamiento cloud.
- Elimine backups vencidos tanto en local como en la nube.
- Permita cambiar el proveedor de cloud sin reescribir la logica de negocio.

El proyecto debe ser simple, mantenible y apto para ejecutarse mediante **Tareas Programadas de Windows Server**.

## Alcance inicial

El proveedor cloud inicial es **Google Drive**.

Sin embargo, el diseno debe quedar preparado para sumar luego:

- Amazon S3.
- Dropbox.
- Otros proveedores compatibles con un contrato comun.

No crear dependencias innecesarias sobre Google Drive en la logica de aplicacion. Toda integracion especifica va en Infrastructure.

## Principios de trabajo

- Priorizar claridad sobre abstraccion excesiva.
- Evitar sobrearquitectura.
- Mantener las responsabilidades separadas.
- Usar nombres en ingles para codigo, clases, metodos, interfaces, propiedades y variables.
- Mantener el dominio simple y expresivo.
- Hacer el flujo idempotente para poder reejecutar la tarea sin romper el estado.
- No acoplar la aplicacion a detalles de infraestructura externos.

## Arquitectura propuesta

Usar una separacion por capas:

- `Console`
- `Application`
- `Infrastructure`
- `Domain` si realmente hace falta

La solucion debe seguir una direccion de dependencias desde Console hacia Application, y desde Application hacia interfaces de puertos, nunca hacia implementaciones concretas.

## Inyeccion de dependencias

`Console` sera el composition root, es decir, el gran inyector de dependencias de la aplicacion.

La construccion del contenedor debe hacerse de forma desacoplada, creando clases estaticas por feature dentro de una carpeta `DependencyInjection`.

Reglas:

- Cada feature o modulo debe tener su propia clase de extensiones.
- Ejemplos: `SecurityServiceExtensions`, `BackupServiceExtensions`, `CloudServiceExtensions`.
- Cada clase debe exponer metodos de extension sobre `IServiceCollection`.
- Si hace falta, los metodos tambien pueden recibir `IConfiguration`.
- `Program.cs` debe quedar limpio y expresivo, por ejemplo con llamadas como `services.AddBackupServices(configuration)`.
- No concentrar todo el registro en un solo archivo gigante.
- Mantener el registro de servicios cerca de la feature que los usa.

Esto permite mantener el arranque claro, desacoplado y facil de extender.

### Console

Responsabilidades:

- Punto de entrada de la app.
- Carga de configuracion.
- Inicializacion de dependencias.
- Ejecucion del caso de uso principal.
- Manejo final de exit codes.

No debe contener reglas de negocio.

### Application

Responsabilidades:

- Casos de uso.
- Coordinacion del flujo de backup, subida y limpieza.
- Definicion de interfaces necesarias para abstraer infraestructura.
- Validaciones de negocio simples.
- Orquestacion de servicios.

Debe ser independiente del proveedor cloud y del filesystem concreto.

### Infrastructure

Responsabilidades:

- Implementaciones concretas de acceso al filesystem.
- Integracion con Google Drive.
- Futuras integraciones con S3, Dropbox u otros servicios.
- Lectura/escritura de archivos locales.
- Persistencia tecnica si se necesitara.
- Logging tecnico si se integra a nivel de adaptador.

Toda API externa vive aqui.

### Domain

Usar esta capa solo si aporta valor real.

Podria contener:

- Entidades de negocio simples.
- Value Objects.
- Tipos para politica de retencion.
- Modelos del dominio del backup.

Si el modelo es pequeno, no inventar una capa extra innecesaria.

## Interfaces sugeridas

Definir interfaces en `Application` para desacoplar la logica de infraestructura.

Interfaces recomendadas:

- `IBackupDiscoveryService`
- `IBackupUploadService`
- `IBackupDeletionService`
- `IBackupRetentionService`
- `ICloudStorageProvider`
- `IFileSystemService`
- `IClock`
- `IApplicationLogger` o usar `ILogger<T>` con DI si el proyecto ya lo resuelve bien

Interfaces auxiliares posibles:

- `IBackupNameParser`
- `IBackupMetadataRepository` si luego hace falta recordar estados o sincronizacion
- `ITransactionLogPruner` si el manejo de logs transaccionales termina siendo muy especifico

Regla practica:

- Crear interfaces solo cuando reduzcan acoplamiento real.
- No crear interfaces por anticipacion sin una necesidad concreta.

## Configuracion en appsettings.json

La configuracion del proyecto se maneja con dos archivos:

- `appsettings.json`
- `appsettings.Development.json`

Regla general:

- `appsettings.json` debe mantener la estructura completa de configuracion.
- `appsettings.Development.json` contiene los valores de desarrollo, incluidos secretos de prueba en texto plano.
- Cuando se agregue una clave nueva, debe agregarse en ambos archivos.
- En `appsettings.json` la nueva clave debe quedar vacia, por ejemplo: `"DatabaseConfiguration": ""`.
- En `appsettings.Development.json` debe cargarse el valor real de desarrollo.

Esto permite mantener la configuracion base actualizada y al mismo tiempo trabajar con valores locales durante el desarrollo.

Ejemplo conceptual:

```json
{
  "BackupSettings": {
    "LocalBackupRootPath": "D:\\SQLBackups",
    "CloudRootPath": "backups/sqlserver",
    "AllowedExtensions": [".bak", ".diff", ".trn"],
    "FileNamePattern": "^(?<database>[A-Za-z0-9_]+)_(?<type>FULL|DIFF|TRANSAC)_(?<date>\\d{8})_(?<time>\\d{6})$",
    "DeleteLocalAfterUpload": false,
    "DeleteCloudExpiredBackups": true,
    "DryRun": false,
    "MaxParallelUploads": 2,
    "Retention": {
      "FullDays": 30,
      "DiffDays": 7,
      "TransactionLogDays": 3
    }
  }
}
```

Nota:

- En desarrollo, los secretos pueden vivir en `appsettings.Development.json` porque el archivo no se versiona ni se distribuye al entorno productivo.
- En produccion, los valores sensibles se cargan manualmente o por el mecanismo seguro definido para el servidor.
- `appsettings.json` nunca debe quedar desactualizado respecto de las claves disponibles.

## Options Pattern

Usar el **Options Pattern** de .NET para mapear configuraciones a clases tipadas.

Requisitos:

- Crear clases de opciones por bloque de configuracion, por ejemplo `BackupSettings`.
- Registrar esas opciones con `IOptions<T>`, `IOptionsSnapshot<T>` o `IOptionsMonitor<T>` segun el caso.
- Preferir acceso tipado a la configuracion dentro de Application e Infrastructure.
- Evitar leer valores directamente desde `IConfiguration` en toda la app salvo en el composition root o en extensiones de DI.

La idea es que la configuracion se lea una vez, se mapee a tipos concretos y luego el resto del codigo trabaje con esos tipos.

## Convencion de commits

Cuando se haga un commit, usar el formato:

`BkpUploader.NombreDeCapa: mensaje breve`

Reglas:

- Mantener los commits sectorizados por capa cuando sea posible.
- Usar `Application`, `Infrastructure`, `Console`, `Domain` o un nombre de capa equivalente para el prefijo.
- El mensaje debe estar en español, ser breve y descriptivo.
- Si un cambio cruza varias capas, separar en commits distintos antes de mezclar todo en uno solo.

## Modelo de retencion

La politica de retencion debe ser configurable por tipo de backup.

Tipos base:

- `Full`
- `Diff`
- `TransactionLog`

Reglas de negocio:

- Cada tipo puede tener una ventana de retencion distinta.
- La eliminacion local y la eliminacion en cloud no tienen por que ocurrir al mismo tiempo, pero deben seguir la misma politica.
- Un backup vencido en cloud no debe depender de que exista o no el archivo local.
- Si el nombre no permite identificar el tipo, el archivo se considera invalido y se maneja de forma controlada.

Ejemplo de politica:

- Full: 30 dias.
- Diff: 7 dias.
- TransactionLog: 3 dias.

Si mas adelante se requiere retencion por base de datos, por entorno o por prioridad, agregarlo sin romper la estructura base.

## Convencion de nombres

Los backups estan normalizados con esta forma:

`DatabaseName_BACKUPTYPE_yyyyMMdd_HHmmss`

Ejemplo:

`MuOnline_DIFF_20260709_200504`

Reglas:

- `DatabaseName` puede incluir letras, numeros y guion bajo.
- `BACKUPTYPE` debe identificarse de forma consistente: `FULL`, `DIFF`, `TRANSAC` o el nombre que se defina como canonico, pero debe mapearse de forma estable a `Full`, `Diff` y `TransactionLog`.
- La fecha y hora deben poder parsearse de manera determinista.
- La logica de parseo debe estar centralizada, no duplicada.

## Flujo de ejecucion

Flujo recomendado:

1. Cargar configuracion.
2. Validar que las rutas y credenciales minimas existan.
3. Descubrir backups locales candidatos.
4. Parsear nombre, tipo, fecha y metadata util.
5. Decidir cuales estan vigentes y cuales vencidos segun la politica.
6. Subir a cloud los archivos que deban persistirse.
7. Verificar exito de la subida antes de borrar localmente, si esa politica esta habilitada.
8. Eliminar backups vencidos en cloud.
9. Registrar resumen final y salir con codigo apropiado.

Si el proyecto incorpora sincronizacion incremental o estado persistido, mantener el flujo compatible con reejecuciones.

## Idempotencia

El proceso debe poder correrse varias veces sin generar duplicados ni efectos secundarios incorrectos.

Requisitos:

- Si un archivo ya existe en cloud y coincide por nombre/tamano/hash o criterio equivalente, no volver a subirlo salvo que se detecte diferencia real.
- Borrar solo despues de confirmar la operacion que lo justifica.
- Tolerar reintentos.
- Si la ejecucion se interrumpe, la proxima corrida debe poder continuar de forma segura.

Si hace falta un estado minimo de sincronizacion, guardarlo de manera simple y explicita, no como una solucion compleja prematura.

## Logging

Registrar informacion suficiente para operar el sistema en un servidor real.

Debe loguearse:

- Inicio y fin de ejecucion.
- Cantidad de archivos detectados.
- Archivos subidos.
- Archivos eliminados.
- Archivos omitidos y motivo.
- Errores de conexion o permisos.
- Resumen final con exito o falla.

Buenas practicas:

- Usar logging estructurado si el stack lo permite.
- No loguear secretos.
- No loguear rutas o datos innecesarios si contienen informacion sensible.
- Incluir contexto util para diagnostico.

## Manejo de errores

El manejo de errores debe ser claro y predecible.

Reglas:

- Separar errores recuperables de errores fatales.
- Fallas por red, API o timeout deben poder reintentarse si tiene sentido.
- Fallas de configuracion deben detener la ejecucion temprano.
- Archivos corruptos o con nombre invalido deben registrarse y seguir con el resto si el proceso puede continuar.
- El programa debe devolver exit codes utiles para tareas programadas.

No ocultar excepciones importantes. Capturar solo donde aporte contexto o recuperacion.

## Seguridad de secretos

Nunca hardcodear:

- API keys.
- Refresh tokens.
- Secrets de servicio.
- Passwords.


## Pruebas

Agregar pruebas desde el inicio en la medida razonable.

Prioridades:

- Parseo de nombres de backup.
- Calculo de retencion por tipo.
- Decision de expiracion.
- Idempotencia del flujo.
- Manejo de casos invalidos.

Tipos sugeridos:

- Unitarias para dominio y aplicacion.
- Integracion para adaptadores de infraestructura cuando sea viable.

No depender de Google Drive real para las pruebas unitarias.
Usar mocks, stubs o fakes en Application.

## Convenciones de codigo

- Todo el codigo debe estar en ingles.
- Clases y metodos con nombres descriptivos.
- Evitar abreviaturas oscuras.
- Mantener metodos pequenos y con una sola responsabilidad.
- Preferir tipos fuertemente tipados antes que cadenas magicas.
- Usar `async` y `await` donde haya I/O real.
- No introducir patrones complejos si el problema no lo exige.
- Seguir las convenciones de formato de .NET y el analizador configurado por el proyecto.

## Criterios de aceptacion

Considerar el trabajo aceptable cuando:

- La aplicacion corre desde consola sin pasos manuales raros.
- La configuracion sale de `appsettings.json` y variables de entorno cuando corresponde.
- El proveedor cloud esta abstraido y Google Drive es solo una implementacion.
- El proceso identifica backups por nombre y tipo de forma confiable.
- La retencion por tipo funciona.
- Se pueden borrar backups vencidos localmente y en cloud.
- El flujo es idempotente.
- Hay logging util.
- Hay pruebas basicas de las reglas principales.
- El codigo queda listo para ser ejecutado por tarea programada.

## Restricciones

- No agregar UI.
- No agregar base de datos salvo necesidad justificada.
- No atar la logica de negocio a Google Drive.
- No sobrecomplicar con patrones de arquitectura sin beneficio real.
- No mezclar responsabilidades entre capas.
- No usar nombres en espanol para el codigo.
- No borrar archivos sin validar antes la regla que lo habilita.

## Regla operativa para Codex

Antes de cambiar codigo, revisar la estructura existente y respetar la direccion de dependencias.

Antes de introducir nuevas abstracciones, preguntarse:

- Resuelve un problema real?
- Reduce acoplamiento?
- Mejora testabilidad?
- Mantiene el proyecto simple?

Si la respuesta es no, no agregarla.
