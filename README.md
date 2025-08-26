<!-- LOGO -->
<p align="center">
  <img src="docs/logo.png" alt="Mevora Logo" width="150"/>
</p>

# Mevora
Mevora enables you to quickly and easily perform certain request/response and event operations using the CQRS and Mediator design patterns. You can review the [documentation](#hakkında) to learn how to use Mevora.

## How does Mevora work?

Mevora identifies all your Request Processor, Pipeline Action, and Message Processor classes at the beginning of the project and creates a main MevoraDispatcher class. It creates the necessary methods for each Processor class within the class and makes them available for your use. Since these operations are performed at compile-time, it offers high-performance usage.

---

## 📖 İçindekiler
1. [Hakkında](#hakkında)
2. [Başlarken](#başlarken)
3. [Kurulum](#kurulum)
4. [Hızlı Başlangıç](#hızlı-başlangıç)
5. [API Referansı](#api-referansı)
   - [MevoraDispatcher](#mevoradispatcher)
   - [IMevoraDispatcher](#imevoradispatcher)
   - [IRequest](#irequest)
   - [IMessage](#imessage)
6. [Örnek Kullanımlar](#örnek-kullanımlar)
7. [Katkıda Bulunma](#katkıda-bulunma)
8. [Lisans](#lisans)

---

## Hakkında
Kütüphanenin amacı, kullanım senaryoları ve genel özelliklerini buraya yazabilirsin.

---

## Başlarken
Kütüphaneyi projene eklemek için gerekli ön bilgiler ve gereksinimler.

---

## Kurulum
NuGet üzerinden veya manuel olarak kurulum talimatlarını buraya ekleyebilirsin.

```bash
dotnet add package Mevora
