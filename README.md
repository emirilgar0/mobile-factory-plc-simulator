Mini Akıllı Fabrika - PLC Simülatörü
Bu proje, Bilgisayar Organizasyonu ve Mimarisi dersi kapsamında; bir fabrikanın otomasyon sürecini yöneten PLC (Programlanabilir Mantık Denetleyicisi) sisteminin yazılımsal olarak simüle edilmesidir. Proje, düşük seviyeli donanım yönetimi ile yüksek seviyeli kontrol algoritmalarını birleştirir.

🛠️ Mimari Bileşenler
Sistem, bir bilgisayarın temel çalışma prensiplerini şu modüllerle taklit eder:

Bellek (Memory): Adreslenebilir bir yapıda (Dictionary üzerinden) sensör ve aktüatör verilerini saklar.

Register Sistemi: CPU içindeki hızlı veri depolama birimlerini simüle eder.

Ladder Logic: Endüstriyel standart olan basamaklı mantık diyagramlarını yazılımsal olarak koşturur.

PID Kontrolörü: Sıcaklık ve basınç gibi değişkenleri dengelemek için Oransal-İntegral-Türevsel kontrol algoritmasını kullanır.

 Öne Çıkan Özellikler
Çoklu İş Parçacığı (Multi-threading): Simülasyon döngüsü, kullanıcı arayüzünden (Console) bağımsız olarak Thread yapısıyla arka planda kesintisiz çalışır.

Kesme (Interrupt) Simülasyonu: Kritik sıcaklık veya düşük basınç durumlarında sistem otomatik olarak "Kesme" üreterek makineleri durdurur ve alarm verir.

Gerçek Zamanlı İzleme: Konsol ekranı üzerinden sensör verileri (Giriş) ve aktüatör durumları (Çıkış) anlık olarak takip edilebilir.

 Teknolojiler
Dil: C# (.NET)

Konseptler: Multithreading, Memory Mapping, Control Theory (PID)
