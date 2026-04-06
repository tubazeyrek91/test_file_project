# Distributed Storage Case Study

Bu proje, büyük dosyaların parçalara ayrılarak (chunk),
farklı depolama sağlayıcılarına dağıtılması ve
gerektiğinde tekrar birleştirilerek dosya bütünlüğünün
korunmasını amaçlayan bir .NET Console Application'dır.

## Amaç

Bu çalışmanın amacı, gerçek hayatta dosya yedekleme ve
dağıtık depolama sistemlerinde kullanılan temel yapı taşlarını
soyutlanmış, genişletilebilir ve test edilebilir bir mimari ile
modellemektir.

Sonuçtan çok mimari yaklaşım ve tasarım kararları
ön planda tutulmuştur.

---

## Mimari Genel Bakış

Proje **Clean Architecture** prensiplerine uygun olarak
5 bağımsız katmandan (proje) oluşmaktadır:

```
src/
├── DistributedStorage.Domain/            # Entity'ler, domain exception'ları (sıfır bağımlılık)
├── DistributedStorage.Application/       # Interface'ler, servisler, stratejiler (→ Domain)
├── DistributedStorage.Infrastructure/    # Chunker, hashing, storage provider'lar (→ Application)
├── DistributedStorage.Persistence/       # EF Core DbContext, repository, migration'lar (→ Application)
└── DistributedStorage.ConsoleApp/        # Composition Root, DI, menüler (→ hepsi)
```

### Katman Bağımlılık Kuralı

```
Domain ← Application ← Infrastructure
                     ← Persistence
                     ← ConsoleApp (Composition Root)
```

- **Domain** hiçbir şeye bağımlı değildir.
- **Application** sadece Domain'e bağımlıdır. Tüm interface'ler (`IStorageProvider`, `IHashService`, `IMetadataRepository`, `IChunker` vb.) bu katmanda tanımlıdır.
- **Infrastructure** ve **Persistence** Application interface'lerini implement eder, birbirlerine bağımlı **değildir**.
- **ConsoleApp** tüm katmanları birleştirir (Composition Root).

Bu yapı sayesinde katman bağımlılıkları **compile-time**'da enforce edilir.

---

## Chunking Yaklaşımı

Dosyalar, boyutlarına göre dinamik olarak belirlenen
chunk size ile parçalara ayrılır.

| Dosya Boyutu | Chunk Boyutu |
|-------------|-------------|
| < 10 MB     | 512 KB      |
| < 1 GB      | 5 MB        |
| ≥ 1 GB      | 20 MB       |

Bu amaçla `IChunkSizeStrategy` (Strategy Pattern) kullanılmıştır.
Böylece chunk hesaplama algoritması değiştirilebilir hale getirilmiştir.

`IChunker` interface'i sayesinde chunking mekanizması da
soyutlanmış ve test edilebilir yapıdadır.

---

## Storage Provider Abstraction

Tüm depolama işlemleri `IStorageProvider` üzerinden yapılmaktadır.

Mevcut implementasyonlar:
- **FileSystemStorageProvider** — Dosya sistemine chunk yazar/okur
- **DatabaseStorageProvider** — SQLite veritabanına BLOB olarak chunk yazar/okur

Chunk'lar upload sırasında **round-robin** algoritmasıyla
provider'lara dağıtılır.

Yeni bir storage eklemek için core kodda
herhangi bir değişiklik yapılmasına gerek yoktur.

---

## Metadata Yönetimi

Her dosya ve chunk bilgisi aşağıdaki metadata ile saklanır:
- Dosya adı
- FileId
- Chunk sırası
- Storage provider bilgisi
- SHA256 checksum

Metadata kalıcı olarak SQLite veritabanında tutulur (EF Core).

`IDbContextFactory<MetadataDbContext>` kullanılarak
Singleton servislerle DbContext lifetime uyumsuzluğu giderilmiştir.

---

## Dosya Bütünlüğü

Dosya birleştirme sonrası SHA256 checksum hesaplanır
ve upload sırasında alınan checksum ile karşılaştırılır.

Uyuşmazlık durumunda `IntegrityException` fırlatılır.

---

## Kullanılan Design Pattern'ler

| Pattern | Uygulama Yeri |
|---------|--------------|
| **Strategy** | `IChunkSizeStrategy` / `DefaultChunkSizeStrategy` |
| **Repository** | `IMetadataRepository` / `MetadataRepository` |
| **Provider (Adapter)** | `IStorageProvider` / `FileSystemStorageProvider`, `DatabaseStorageProvider` |
| **Dependency Injection** | `ServiceCollection` ile IoC container |
| **Composition Root** | `Program.cs` — tek noktada DI yapılandırması |
| **Factory** | `IDbContextFactory<MetadataDbContext>` |

---

## Logging & Monitoring Pipeline

Tüm işlemler **7 kategori** altında loglanmaktadır:
`Upload`, `Download`, `Chunking`, `Storage`, `Hashing`, `Metadata`, `System`

### Pipeline

```
Uygulama (Serilog) → RabbitMQ (logs_exchange, routing key: log.{kategori})
                           ↓
                    LogConsumer (batch, 60sn aralık)
                           ↓
                    MongoDB (koleksiyon: logs_{kategori})
```

- **Serilog File Sink** — Günlük dönen dosya logları (`logs/` dizini)
- **Custom RabbitMQ Sink** — Her log mesajı JSON olarak `logs_exchange` direct exchange'e publish edilir
- **LogConsumer** — Standalone EXE (`src/DistributedStorage.LogConsumer`), RabbitMQ kuyruklarından batch okur ve MongoDB'ye yazar

### Gereksinimler

- **RabbitMQ** — localhost:5672 (varsayılan guest/guest)
- **MongoDB** — localhost:27017

Log seviyeleri ve bağlantı bilgileri `appsettings.json` üzerinden yönetilmektedir.

---

## Unit Tests

Proje **xUnit** ve **Moq** ile test edilmektedir. Toplam **29 test** bulunmaktadır:

| Test Sınıfı | Test Sayısı | Kapsam |
|---|---|---|
| `FileUploadServiceTests` | 7 | Upload, round-robin, checksum, metadata |
| `FileDownloadServiceTests` | 5 | Chunk birleştirme, sıralama, integrity |
| `ChunkerTests` | 5 | Parçalama, veri bütünlüğü, boş dosya |
| `Sha256HashServiceTests` | 5 | Determinizm, hex format, dosya/byte eşleşme |
| `DefaultChunkSizeStrategyTests` | 7 | Tüm boyut aralıkları ve sınır değerleri |

```bash
dotnet test
```

---

## Teknolojiler & Paketler

| Teknoloji | Versiyon | Kullanım |
|---|---|---|
| .NET | 8.0 | Runtime |
| Entity Framework Core | 8.0 | Metadata persistence (SQLite) |
| Microsoft.Data.Sqlite | 8.0 | BLOB chunk storage |
| Serilog | 4.3 | Structured logging |
| RabbitMQ.Client | 7.1 | Log message broker |
| MongoDB.Driver | 3.4 | Log persistence |
| xUnit | 2.9 | Unit testing |
| Moq | 4.20 | Mocking |

---

## Çalıştırma

### Ana Uygulama

```bash
dotnet run --project src/DistributedStorage.ConsoleApp
```

### LogConsumer

```bash
dotnet run --project src/DistributedStorage.LogConsumer
```

### Veritabanı Migration

```bash
dotnet ef database update --project src/DistributedStorage.Persistence --startup-project src/DistributedStorage.ConsoleApp
```
