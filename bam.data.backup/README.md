# bam.data.backup

Database backup and restore functionality with DAO-to-DTO generation for portable data serialization.

## Overview

`bam.data.backup` provides infrastructure for backing up and restoring Bam framework databases. The core workflow involves extracting data from a source database via DAO (Data Access Object) types, converting each DAO instance to a lightweight DTO (Data Transfer Object) that contains only property values, and persisting those DTOs to a backup repository. When restoring, DTOs are loaded from the backup repository, converted back to DAO instances, and inserted into a target database with foreign key references corrected to reflect new auto-generated IDs.

The `DaoToDtoGenerator` handles the code generation side, producing DTO source code from DAO assemblies. It scans for types with `[Table]` attributes, extracts properties with `[Column]` attributes, and generates simple C# classes that mirror the DAO column structure without any database methods. The `Dto` class provides static factory methods for dynamically creating DTO types and assemblies from DAO assemblies, including a Roslyn-based runtime compilation pipeline.

The `BackedupDatabase` class acts as a proxy `Database` that wraps an underlying database while maintaining a backup repository, delegating all database operations to the wrapped instance.

## Key Classes

| Class | Description |
|---|---|
| `DaoBackup` | Orchestrates database backup and restore operations. Backs up all DAO instances as DTOs to a repository and restores them to a target database with foreign key correction. |
| `DaoToDtoGenerator` | Code generator that produces DTO (Data Transfer Object) source code and assemblies from DAO assemblies, including only column-attributed properties. |
| `Dto` | Static utility class for creating DTO types and instances from DAO types. Provides runtime assembly generation via Roslyn compilation. |
| `DtoModel` | Model class used to render DTO source code from templates. Contains namespace, type name, properties, and metadata reference resolution. |
| `DtoPropertyModel` | Describes a single DTO property: name, type, and whether it is a key or foreign key. |
| `BackedupDatabase` | A `Database` proxy that delegates all operations to an underlying database while maintaining an associated backup `IRepository`. |
| `OldToNewIdMapping` | Tracks the mapping between old IDs (from the source database) and new IDs (in the restored database) for a given type and UUID. |

## Dependencies

### Project References
- bam.base
- bam.data.repositories
- bam.data
- bam.generators

### Target Framework
- net10.0

## Usage Examples

### Backing up a database
```csharp
Assembly daoAssembly = typeof(MyDaoType).Assembly;
IDatabase sourceDatabase = myDatabase;
IRepository backupRepo = myBackupRepository;

var backup = new DaoBackup(daoAssembly, sourceDatabase, backupRepo);
backup.Backup(); // Saves all DAO data as DTOs to the backup repository
```

### Restoring a database
```csharp
Database restoreTarget = new SQLiteDatabase("./restore", "RestoredDb");
HashSet<OldToNewIdMapping> idMappings = backup.Restore(restoreTarget, logger);
// idMappings contains the old-to-new ID mappings for each restored record
```

### Generating DTO source code
```csharp
var generator = new DaoToDtoGenerator(daoAssembly);

// Write DTO source files to a directory
generator.WriteDtoSource("MyApp.Dtos", "./GeneratedDtos/");

// Or generate a compiled DTO assembly
GeneratedAssemblyInfo assemblyInfo = generator.GenerateDtoAssembly();
```

### Getting DTO types from a DAO assembly
```csharp
Type[] dtoTypes = Dto.GetTypesFromDaos(daoAssembly);

// Copy a DAO instance as a DTO
object dtoInstance = Dto.Copy(myDaoInstance);
```

## Known Gaps / Not Yet Implemented

- **`DaoBackup.Backup`**: Has a TODO noting that `LoadAll` is used which could cause memory issues with large datasets. A batched approach is needed.
- **`BackedupDatabase`**: Several methods use `new` keyword hiding rather than `override`, which may cause unexpected behavior when used polymorphically.
- **Live backup hooking**: `DaoBackup` subscribes to `Dao.AfterCommitAny` in its constructor for live backup, but this global static event subscription could lead to issues with multiple backup instances.
