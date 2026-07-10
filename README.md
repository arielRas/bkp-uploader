# BkpUploader

`BkpUploader` es una aplicación de consola en **.NET 10** pensada para automatizar la gestión de backups de SQL Server en un servidor Windows.

## Qué hace

La aplicación:

- Detecta backups locales de SQL Server.
- Clasifica los archivos por tipo de backup.
- Los sube a almacenamiento cloud.
- Elimina backups vencidos en local y en cloud según la política configurada.
- Mantiene la lógica de negocio desacoplada del proveedor cloud.

## Estado actual

En este momento el único proveedor cloud habilitado es:

- **Cloudflare R2**, usando el **AWS SDK for .NET** y la API compatible con **S3**.

La integración con Google Drive fue reemplazada por esta implementación.

## Arquitectura

La solución está separada por capas:

- `BkpUploader.Console`
- `BkpUploader.Application`
- `BkpUploader.Infrastructure`
- `BkpUploader.Domain`

`Console` actúa como composition root y `Infrastructure` contiene las integraciones concretas.

## Requisitos

- Windows
- .NET 10 SDK
- Acceso a un bucket de Cloudflare R2

## Instalación y compilación

Ir a la carpeta de la consola:

```powershell
cd rutaLocal\BkpUploader\src\BkpUploader.Console\
```

Compilar el ejecutable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

## Configuración

La configuración se carga desde los archivos JSON de la consola.

La integración con R2 usa la sección:

```json
"CloudflareR2Options": {
  "ServiceUrl": "",
  "AccountId": "",
  "AccessKeyId": "",
  "SecretAccessKey": "",
  "BucketName": "",
  "RootPrefix": "",
  "FullPrefix": "Full",
  "DifferentialPrefix": "Differential",
  "TransactionLogPrefix": "TransactionLog"
}
```

### Campos principales

- `ServiceUrl`: endpoint S3 de Cloudflare R2. Si lo completás, se usa tal cual.
- `AccountId`: identificador de cuenta de R2. Se usa como fallback para construir el endpoint si `ServiceUrl` está vacío.
- `AccessKeyId` y `SecretAccessKey`: credenciales S3 de R2.
- `BucketName`: nombre del bucket de destino.
- `RootPrefix`: prefijo raíz opcional dentro del bucket.

Si en el bucket ya tenés carpetas por tipo en el root, podés dejar `RootPrefix` vacío.

## Notas sobre R2

Cloudflare R2 no usa carpetas reales, sino prefijos en el nombre del objeto. Por eso:

- `Full/archivo.bak`
- `Differential/archivo.bak`
- `TransactionLog/archivo.trn`

son solo claves dentro del bucket.

## Convenciones

- Todo el código está en inglés.
- La solución usa **.NET 10**.
- La lógica de aplicación no depende del proveedor cloud concreto.

## Seguridad

No se deben versionar secretos reales en el repositorio. Las credenciales de R2 deben tratarse como información sensible.

## Licencia

Uso interno o según la política del proyecto.
