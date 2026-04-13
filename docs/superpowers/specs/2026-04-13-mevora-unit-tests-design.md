# Mevora Unit Test Projesi — Tasarım Dokümanı

**Tarih:** 2026-04-13
**Durum:** Onaylandı
**Mock Stratejisi:** A) Sadece Moq
**Test Data Yönetimi:** A) Inline Anonymous Objects
**Test Öncelik Sırası:** A) Behavior → Validation → Configuration

---

## 1. Proje Yapısı

```
D:\Arda\software\Mevora\Mevora.UnitTests/
├── Mevora.UnitTests.csproj
├── Behavior/
│   ├── PipelineExecutionTests.cs
│   ├── ValidationBehaviorTests.cs
│   └── EventPublisherTests.cs
├── Core/
│   ├── ValidationContextTests.cs
│   └── ValidationResultTests.cs
├── Configuration/
│   └── ConfigurationModelTests.cs
├── Helpers/
│   └── TestDispatcherBuilder.cs
└── Usings.cs
```

**Bağımlılıklar:**
- xUnit 2.9.0
- FluentAssertions 6.12.0
- Moq 4.20.70
- Mevora.csproj (core library)
- Mevora.Generators.csproj (source generator — test projesi de kendi dispatcher'ini uretecek)

---

## 2. Test Kategorileri ve Oncelik Sırası

### Oncelik 1: Behavior Tests (Is mantigi uçtan uca testler)

#### PipelineExecutionTests.cs

**SinglePipeline_Should_Execute**
- Tek bir Pipeline eklendiğinde Handler'dan once devreye girer
- Mock pipeline ve handler ile dogrulanir
- Sonuc Handler'dan donen deger ile eslesir

**MultiplePipelines_Should_ExecuteInCorrectOrder**
- Birden fazla Pipeline oldugunda Matruşka/Onion sırası dogrulanir
- ExecutionOrder listesi ile kanitlanir:
  - OuterPipeline_Enter → InnerPipeline_Enter → Handler → InnerPipeline_Exit → OuterPipeline_Exit
- TestDispatcherBuilder ile dıs → ıc sıralama kurulur

#### ValidationBehaviorTests.cs

**FailingValidator_Should_ShortCircuit_Pipeline**
- Validasyon hatali istek Pipeline ve Handler'i short-circuit eder
- ValidationException firlatilir
- Handler ve Pipeline'in Times.Never cagrildigi dogrulanir

#### EventPublisherTests.cs

**PublishAsync_Should_Trigger_AllHandlers**
- Aynı IMessage tipi icin kayitli tum Handler'lar tetiklenir
- Her handler'in tam bir kez cagrildigi dogrulanir

---

### Oncelik 2: Core Tests (Validation Altyapısı)

#### ValidationContextTests.cs

**CheckNotEmpty_Should_AddError_WhenValueIsNull**
- Null deger icin hata eklenir
**CheckNotEmpty_Should_NotAddError_WhenValueIsProvided**
- Gecerli deger icin hata eklenmez
**CheckMinLength_Should_AddError_WhenTooShort**
- MinLength kuralina takilan degerler icin hata eklenir
**CheckRange_Should_AddError_WhenOutOfBounds**
- Sinir disi degerler icin hata eklenir
**CheckRegex_Should_AddError_WhenNotMatching**
- Regex'e uymayan degerler icin hata eklenir
**Reset_Should_ClearErrors_FromPreviousRequest**
- Reset() cagrildiktan sonra _errors.Clear() oldugu dogrulanir
- Pooling performans stratejisi icin kritik

#### ValidationResultTests.cs

**Success_Should_ReturnValidResult_WithNoErrors**
- ValidationResult.Success() gecerli sonuc dondurur
**Failure_Should_ReturnInvalidResult_WithSingleError**
- ValidationResult.Failure("msg") hatali sonuc dondurur
**Failure_Should_ReturnInvalidResult_WithMultipleErrors**
- Birden fazla hata ile cagrilabilir
**Success_And_Failure_Should_BeReusedSafely**
- Her cagrida yeni instance dondurulur (singleton degil)

---

### Oncelik 3: Configuration Tests

#### ConfigurationModelTests.cs

**AddPipelineAction_Should_Throw_WhenTypeIsNotGeneric**
- Generic olmayan tip eklendiginde InvalidOperationException firlatilir
**AddPipelineAction_Should_Accept_OpenGenericType**
- Acik generic tip (LoggingPipelineAction<,>) kabul edilir

---

## 3. TestDispatcherBuilder Helper

Fluent API ile her test icin izole dispatcher olusturur:

```csharp
var dispatcher = TestDispatcherBuilder.Create()
    .WithHandler<SimpleRequest, string>(mockHandler.Object)
    .WithPipeline<SimpleRequest, string>(mockPipeline.Object)
    .WithValidator<ValidatableRequest>(mockValidator.Object)
    .Build();
```

**Yetenekleri:**
- IRequestProcessorAsync<T> ve IRequestProcessorAsync<T,R> handler ekleme
- IMessageProcessor<T> handler ekleme (coklu)
- IRequestValidator<T> ekleme
- IPipelineAction<T,R> ekleme (coklu, sira korunur)
- Assembly ekleme (processor discovery icin)
- ServiceLifetime ayarlama

---

## 4. Test Data Yonetimi

Her test kendi inline test class'ini yaratir:
- `SimpleRequest` — tek string field
- `RequestWithMultiplePipelines` — birden fazla pipeline icin
- `ValidatableRequest` — validation testleri icin
- `RequestWithRange` — CheckRange testleri icin
- `UserRegisteredEvent` — event testleri icin

---

## 5. Calistirma

```bash
dotnet test D:/Arda/software/Mevora/Mevora.UnitTests
```

Tum testler gecerilmeli (Passed). Ilk calistirmada Source Generator test dispatcher'ini uretecek.