# Skálázható szoftverek - Nagy házi feladat

## Követelmény specifikáció

Egy online jegyértékesítő rendszert  megvalósítok meg, amely lehetővé teszi a látogatók számára, hogy  események között böngésszenek, valamint jegyeket vásároljanak. Az adminisztrátorok számára biztosítani kell az események és a készletek kezelését.

### Funkcionális követelmények:
* **A vásárlók** böngészhetnek az elérhető események (koncertek) listájában.
* **A vásárlók** megtekinthetik egy adott esemény részleteit (időpont, helyszín, ár).
* **A vásárlók** jegyeket foglalhatnak a kiválasztott eseményre.
* **A rendszer** kezeli a készletet, megakadályozva a túlfoglalást tranzakcionális működéssel.
* **A rendszer** sikeres foglalás esetén aszinkron módon értesítést (szimulált email) küld a felhasználónak.
* **Az adminisztrátorok** új eseményeket hozhatnak létre és módosíthatják a meglévőket.

## Architektúra és technológiák

### Architektúrális alapelvek
A szolgáltatások **konténerizált mikroszolgáltatásokként** lesznek megvalósítva Kubernetes (AKS) környezetben.

**Szolgáltatások  elve:**
A komponensekre bontás az **üzleti funkciók** szétválasztásán alapul:
1.  **Catalog:** Az események adatainak kezelése (böngészés, adminisztráció).
2.  **Booking:** A jegyvásárlási folyamat és a tranzakciók kezelése.
3.  **Notification:** Az értesítések kiküldése aszinkron módon, háttérfolyamatként.

**Technológia választás indoklása:**
* **.NET 8:** Modern keretrendszer mikroszolgáltatásokhoz.
* **PostgreSQL:** Relációs adatbázis a strukturált adatok (események) megbízható tárolására. Konténerben futtatva.
* **Redis:** In-memory kulcs-érték tár a foglalások gyors kezeléséhez és készletnyilvántartáshoz. Konténerben futtatva.
* **RabbitMQ & MassTransit:** Aszinkron, üzenetsor alapú kommunikáció a szolgáltatások laza csatolásához. Konténerben futtatva.
* **Traefik:** Ingress Controller a bejövő forgalom routingolásához a Kubernetes klaszteren belül.
* **REST API & Polly:** Szinkron kommunikáció a szolgáltatások között, hibatűrő (Retry) mechanizmussal kiegészítve.
* **MongoDB**analytics-service (.NET 8 Worker): ÚJ KOMPONENS. Háttérszolgáltatás, amely feliratkozik a TicketPurchased eseményre (ugyanarra, amire a Notification is), és statisztikai céllal elmenti a vásárlást egy MongoDB adatbázisba.

### Komponensek listája

A rendszer elemei, rövid leírása és a választott technológiák:

| Szolgáltatás | Technológia | Leírás |
| :--- | :--- | :--- |
| **ticket-front** | Vue.js / SPA | (Opcionális) Webalkalmazás a vásárlóknak. |
| **ticket-admin** | Vue.js / SPA | (Opcionális) Webalkalmazás az adminisztrátoroknak. |
| **catalog-service** | .NET 8 WebAPI | Események kezelése és listázása. Saját adatbázist (PostgreSQL) használ. REST API-t nyújt. |
| **booking-service** | .NET 8 WebAPI | Jegyvásárlás kezelése. A készletet Redisben kezeli, tranzakciót Postgresben, majd eseményt publikál (MassTransit). REST API-t nyújt. Hibatűrő HTTP hívást alkalmaz a Catalog felé (Polly). |
| **notification-worker** | .NET 8 Worker | Háttérszolgáltatás, amely feliratkozik (MassTransit) a foglalási eseményekre, és szimulálja az értesítést. |
| **catalog-db** | PostgreSQL | Relációs adatbázis (konténer) az események perzisztens tárolásához. |
| **booking-cache** | Redis | Elosztott cache (konténer) a foglalások gyors kezeléséhez és a készletnyilvántartáshoz. |
| **message-broker** | RabbitMQ | Aszinkron üzenetsor (konténer) a `booking-service` és a `notification-worker` közötti kommunikációhoz. |
| **API Gateway** | Traefik | Belépési pont és routing (`/api/catalog`, `/api/booking`). |

### Logikai architektúra ábra

A komponensek közötti kapcsolatok, az adatfolyam és a telepítési környezet (Azure) az alábbi ábrán látható:


![architektúra](architecture.png)
