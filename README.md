# 🅿️ Parking Access Control

Avtomobil raqamini avtomatik tanish (ANPR) asosida ishlaydigan, turar-joy majmualari uchun mo'ljallangan parkovka nazorati tizimi. C# / WPF desktop dasturi bo'lib, USB kamera orqali mashina raqamini real vaqtda o'qiydi, ro'yxatdagi rezidentlar bilan solishtiradi va kirish-chiqishni avtomatik nazorat qiladi.

---

## 📸 Loyiha haqida

Kichik va o'rta turar-joy majmualarida (dom, kottej qishlog'i va h.k.) ruxsatsiz mashinalarning hovliga kirishining oldini olish — bu loyihaning asosiy maqsadi. Tizim ikkita mustaqil kamera oqimi (kirish va chiqish) orqali ishlaydi, har bir aniqlangan raqamni bazadagi rezidentlar ro'yxati bilan solishtiradi va natijaga qarab ruxsat beradi yoki rad etadi.

## ✨ Asosiy imkoniyatlar

- **Real vaqtda ANPR** — klassik computer vision (Canny edge detection + contour filtri) orqali raqamni topish, Tesseract OCR orqali matnga aylantirish
- **Ikkita mustaqil kamera oqimi** — kirish va chiqish uchun alohida, parallel ishlovchi kameralar
- **Rol asosida kirish** — Admin (parol bilan himoyalangan) va Rezident rollari
- **Rezidentlarni boshqarish** — F.I.Sh, xonadon raqami va mashina raqami bo'yicha CRUD amallar, jadval (DataGrid) ko'rinishida
- **Kirish-chiqish tarixi (Access Log)** — har bir voqeaning vaqti, natijasi va kimga tegishli ekanligi bilan to'liq tarix
- **SQLite baza** — fayl asosida, server o'rnatishni talab qilmaydi
- **Arduino integratsiyasi** — Serial Port orqali shlagbaumni avtomatik boshqarish (relay orqali)
- **Thread-safe arxitektura** — ikkita kamera bir vaqtda ishlaganda ham barqaror ishlaydi

## 🛠️ Texnologiyalar

| Sohasi | Texnologiya |
|---|---|
| Til / Framework | C#, .NET 8, WPF |
| Computer Vision | OpenCvSharp4 |
| OCR | Tesseract |
| Ma'lumotlar bazasi | SQLite (Microsoft.Data.Sqlite) |
| Apparat integratsiyasi | Arduino (Serial Port orqali) |

## 🖥️ Ekran ko'rinishlari

<img width="1913" height="885" alt="image" src="https://github.com/user-attachments/assets/eb6ceb75-1ee0-4860-8642-fdac3daba612" />

## 🏗️ Arxitektura

```
Kamera (Kirish)  ──┐
                    ├──► Plate Detection (OpenCV) ──► OCR (Tesseract) ──► Baza bilan solishtirish
Kamera (Chiqish) ──┘                                                            │
                                                                                 ▼
                                                                    Ruxsat / Rad etish qarori
                                                                                 │
                                                              ┌──────────────────┼──────────────────┐
                                                              ▼                  ▼                  ▼
                                                        Access Log         UI yangilanishi    Arduino → Shlagbaum
                                                         (SQLite)            (real vaqt)          (Serial Port)
```

## 📂 Loyiha tuzilishi

```
ParkingApp/
├── LoginWindow.xaml(.cs)       — Admin/Rezident rolini tanlash, parol tekshiruvi
├── AdminWindow.xaml(.cs)       — Asosiy boshqaruv paneli: kameralar, natija, loglar
├── ResidentsWindow.xaml(.cs)   — Rezidentlarni qo'shish/o'chirish/ko'rish
├── AccessLogWindow.xaml(.cs)   — Kirish-chiqish tarixini ko'rish
├── DatabaseService.cs          — SQLite bilan barcha ma'lumotlar bazasi amallari
├── PlateDetector.cs            — Klassik CV asosida raqam hududini aniqlash
├── OcrService.cs                — Tesseract orqali matn tanish
├── Resident.cs / AccessLog.cs  — Entity (model) klasslari
├── settings.txt                — Arduino COM port sozlamasi
└── tessdata/                   — Tesseract til modeli
```

## 🚀 O'rnatish va ishga tushirish

### Talablar
- Windows (WPF faqat Windows'da ishlaydi)
- .NET 8 SDK
- USB veb-kamera(lar)
- *(ixtiyoriy)* Arduino + relay moduli — shlagbaum integratsiyasi uchun

### Qadamlar

1. Repositoriyani klonlang:
   ```bash
   git clone https://github.com/SohibCore/ParkingAccessControl.git
   ```

2. NuGet paketlarini tiklang:
   ```bash
   dotnet restore
   ```

3. `tessdata/eng.traineddata` faylini yuklab, loyiha ichidagi `tessdata/` papkasiga joylang:
   [tesseract-ocr/tessdata](https://github.com/tesseract-ocr/tessdata)

4. *(Ixtiyoriy, Arduino uchun)* `settings.txt` faylida Arduino ulangan COM portni ko'rsating:
   ```
   COM3
   ```

5. Dasturni ishga tushiring:
   ```bash
   dotnet run
   ```

6. Login oynasida **Admin** tugmasini bosing, parolni kiriting, kameralarni ishga tushiring.

## 🔌 Arduino integratsiyasi (ixtiyoriy)

Tizim, "RUXSAT berildi" qarori chiqqanda, Arduino'ga Serial Port orqali signal yuborib, relay vositasida shlagbaumni avtomatik ko'taradi.

**Arduino sketch (soddalashtirilgan misol):**
```cpp
const int relayPin = 7;

void setup() {
  Serial.begin(9600);
  pinMode(relayPin, OUTPUT);
}

void loop() {
  if (Serial.available() > 0) {
    char command = Serial.read();
    if (command == 'O') {
      digitalWrite(relayPin, HIGH);
      delay(5000);
      digitalWrite(relayPin, LOW);
    }
  }
}
```

## 🧠 Texnik jihatdan qiziqarli qarorlar

- **Klassik CV vs Deep Learning:** raqam hududini topish uchun ML modeli o'rgatish o'rniga, Canny edge detection + aspect ratio filtri qo'llanildi — bu, resurs talab qilmaydigan, tez va tushunarli yechim
- **Thread-safety:** ikkita kamera bir vaqtda OCR mexanizmiga murojaat qilganda yuzaga keladigan `AccessViolationException`ning oldi `lock` orqali olindi
- **Duplicate filtri:** bir xil mashinaning bir necha soniya ichida bir necha marta loglanishining oldini olish uchun vaqt-oynasi (time window) mexanizmi qo'llanildi

## 🔮 Kelajakdagi rejalar

- [ ] Rezident uchun shaxsiy kabinet (login/parol bilan)
- [ ] Bir necha parking obyektini markazlashgan boshqarish (bulutli arxitektura)
- [ ] Deep learning asosidagi plate detection (aniqlikni oshirish uchun)
- [ ] "Necha vaqt turgani" hisoboti (kirish/chiqish vaqtlari solishtirmasi)

## 👤 Muallif

**Sohib** — [GitHub](https://github.com/SohibCore)

Ushbu loyiha C#/.NET va computer vision sohasidagi amaliy ko'nikmalarni oshirish maqsadida, noldan, qadam-baqadam o'rganish jarayonida qurilgan.
