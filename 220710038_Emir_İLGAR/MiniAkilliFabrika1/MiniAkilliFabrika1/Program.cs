using System;
using System.Threading;
using System.Collections.Generic;

namespace MiniAkilliFabrika
{
    // BELLEK
    class Bellek
    {
        private Dictionary<int, double> veri = new Dictionary<int, double>();

        public double Oku(int adres)
        {
            return veri.ContainsKey(adres) ? veri[adres] : 0;
        }

        public void Yaz(int adres, double deger)
        {
            veri[adres] = deger;
        }
    }

    // REGISTERLAR(Yazmaçlar plc yi temsilen buraya yazıldı.kodda registerların işini bellek üzerinden hallettik)
    class Registerlar
    {
        private double[] kayitlar = new double[8];

        public double this[int indeks]
        {
            get { return kayitlar[indeks]; }
            set { kayitlar[indeks] = value; }
        }
    }

    // PID KONTROLOR (Sıcaklığı sabitlemek için motorun gücünü ayarlar))
    class PIDKontrol
    {
        public double HedefDeger = 30.0;
        private double integral = 0;
        private double oncekiHata = 0;
        private double OranliKatsayi = 0.5;
        private double IntegralKatsayi = 0.1;
        private double TurevKatsayi = 0.05;

        public double Hesapla(double suankiDeger)
        {
            double hata = HedefDeger - suankiDeger;
            integral += hata;
            double turev = hata - oncekiHata;
            double cikti = OranliKatsayi * hata + IntegralKatsayi * integral + TurevKatsayi * turev;
            oncekiHata = hata;
            return Math.Max(0, Math.Min(100, cikti + 50));
        }
    }

    // LADDER BASAMAGI
    class LadderBasamagi
    {
        public int Numara { get; set; }
        public string Kosul { get; set; }
        public string Aksiyon { get; set; }
        public bool Aktif { get; set; }
    }

    // PLC SISTEMI
    class PLCSistemi
    {
        // Adresler
        private const int SENSOR_HAMMADDE = 1000;
        private const int SENSOR_ISCI = 1004;
        private const int SENSOR_SICAKLIK = 1008;
        private const int SENSOR_BASINC = 1012;
        private const int ACTUATOR_MAKINE = 2000;
        private const int ACTUATOR_MOTOR = 2004;
        private const int ACTUATOR_ALARM = 2008;

        private Bellek bellek = new Bellek();
        private Registerlar registerlar = new Registerlar();
        private PIDKontrol pidKontrol = new PIDKontrol();
        private List<LadderBasamagi> ladderBasamaklari = new List<LadderBasamagi>();

        private int donguSayisi = 0;

        // DURUM DEĞİŞKENLERİ
        private bool sistemAcik = false; // Thread çalışıyor mu? (Elektrik var mı?)
        private bool uretimModu = false; // Start verildi mi? (Ladder çalışsın mı?)

        private Random rastgeleSayi = new Random();

        // SENARYO YÖNETİMİ
        private int senaryoSayaci = 0;
        private int aktifSenaryo = 0;
        private string senaryoMesaji = "SISTEM KAPALI (Beklemede)";
        private bool sonSenaryoArizaliydi = false;

        public PLCSistemi()
        {
            Sifirla(); // Başlangıç ayarları

            // Ladder Logic
            ladderBasamaklari.Add(new LadderBasamagi { Numara = 1, Kosul = "Hammadde>=5 VE Isci=1", Aksiyon = "Makine Baslat" });
            ladderBasamaklari.Add(new LadderBasamagi { Numara = 2, Kosul = "Sicaklik>35", Aksiyon = "Alarm Ac" });
            ladderBasamaklari.Add(new LadderBasamagi { Numara = 3, Kosul = "Basinc<700", Aksiyon = "Makine Durdur" });
        }

        private void Sifirla()
        {
            bellek.Yaz(SENSOR_HAMMADDE, 8);
            bellek.Yaz(SENSOR_ISCI, 1);
            bellek.Yaz(SENSOR_SICAKLIK, 25);
            bellek.Yaz(SENSOR_BASINC, 750);
            bellek.Yaz(ACTUATOR_MAKINE, 0);
            bellek.Yaz(ACTUATOR_MOTOR, 0);
            bellek.Yaz(ACTUATOR_ALARM, 0);
            senaryoSayaci = 0;
            aktifSenaryo = 0;
            senaryoMesaji = "SISTEM KAPALI (Beklemede)";
            sonSenaryoArizaliydi = false;
            foreach (var basamak in ladderBasamaklari)
            {
                basamak.Aktif = false;
            }
        }

        // --- SENSOR SIMULASYONU ---
        private void SensorSimulasyonu()
        {
            double sicaklik = bellek.Oku(SENSOR_SICAKLIK);
            double basinc = bellek.Oku(SENSOR_BASINC);

            // EĞER PAUSE MODUNDAYSA (Sistem Açık ama Üretim Yok)
            if (!uretimModu)
            {
                senaryoMesaji = "PAUSE (Sistem Duraklatildi)     ";

                // Fiziksel Soğuma ve Basınç Dengelenmesi
                if (sicaklik > 22) sicaklik -= 0.2;
                if (basinc > 0) basinc -= 1;

                bellek.Yaz(SENSOR_SICAKLIK, sicaklik);
                bellek.Yaz(SENSOR_BASINC, basinc);
                return;
            }

            // --- NORMAL ÇALIŞMA (START MODU) ---

            senaryoSayaci++;
            if (senaryoSayaci >= 10)
            {
                senaryoSayaci = 0;

                if (sonSenaryoArizaliydi)
                {
                    aktifSenaryo = 0;
                    sonSenaryoArizaliydi = false;
                }
                else
                {
                    int zar = rastgeleSayi.Next(0, 100);
                    if (zar < 40) { aktifSenaryo = 0; sonSenaryoArizaliydi = false; }
                    else if (zar < 70) { aktifSenaryo = 1; sonSenaryoArizaliydi = true; }
                    else { aktifSenaryo = 2; sonSenaryoArizaliydi = true; }
                }

                if (aktifSenaryo == 0) senaryoMesaji = "NORMAL MOD (Stabil Calisma)       ";
                if (aktifSenaryo == 1) senaryoMesaji = "ARIZA SIMULASYONU: Asiri Isinma!";
                if (aktifSenaryo == 2) senaryoMesaji = "ARIZA SIMULASYONU: Basinc Kaybi!";
            }

            switch (aktifSenaryo)
            {
                case 0: // NORMAL
                    if (sicaklik > 35) sicaklik -= 3.0;
                    else if (sicaklik > 25) sicaklik -= 0.5;
                    else sicaklik += 0.5;

                    if (basinc < 700) basinc += 30;
                    else if (basinc < 750) basinc += 5;
                    else basinc -= 5;

                    sicaklik += (rastgeleSayi.NextDouble() - 0.5) * 0.5;
                    break;

                case 1: // ISINMA
                    sicaklik += 1.5;
                    basinc += (rastgeleSayi.NextDouble() - 0.5) * 2;
                    break;

                case 2: // BASINÇ DÜŞÜŞ
                    basinc -= 15;
                    if (sicaklik > 25) sicaklik -= 0.5;
                    break;
            }

            bellek.Yaz(SENSOR_SICAKLIK, sicaklik);
            bellek.Yaz(SENSOR_BASINC, basinc);
        }

        // LADDER LOGIC
        private void LadderCalistir()
        {
           
            if (!uretimModu)
            {
                foreach (var basamak in ladderBasamaklari) basamak.Aktif = false;
                return;
            }

            double hammadde = bellek.Oku(SENSOR_HAMMADDE);
            double isci = bellek.Oku(SENSOR_ISCI);
            double alarm = bellek.Oku(ACTUATOR_ALARM);

            // RUNG 1
            if (hammadde >= 5 && isci == 1 && alarm == 0)
            {
                bellek.Yaz(ACTUATOR_MAKINE, 1);
                ladderBasamaklari[0].Aktif = true;
            }
            else
            {
                ladderBasamaklari[0].Aktif = false;
            }

            // RUNG 2
            double sicaklik = bellek.Oku(SENSOR_SICAKLIK);
            if (sicaklik > 35)
            {
                bellek.Yaz(ACTUATOR_ALARM, 1);
                bellek.Yaz(ACTUATOR_MAKINE, 0);
                ladderBasamaklari[1].Aktif = true;
            }
            else
            {
                bellek.Yaz(ACTUATOR_ALARM, 0);
                ladderBasamaklari[1].Aktif = false;
            }

            // RUNG 3
            double basinc = bellek.Oku(SENSOR_BASINC);
            if (basinc < 700)
            {
                bellek.Yaz(ACTUATOR_MAKINE, 0);
                bellek.Yaz(ACTUATOR_ALARM, 1);
                ladderBasamaklari[2].Aktif = true;
            }
            else
            {
                ladderBasamaklari[2].Aktif = false;
            }
        }

        // PID
        private void PIDCalistir()
        {
            if (!uretimModu) return;

            double sicaklik = bellek.Oku(SENSOR_SICAKLIK);
            double motorPWM = pidKontrol.Hesapla(sicaklik);
            bellek.Yaz(ACTUATOR_MOTOR, motorPWM);
        }

        // EKRAN
        private void EkraniGoster()
        {
            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("   MINI AKILLI FABRIKA - PLC SIMULATOR");
            Console.WriteLine("========================================");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("MOD: " + senaryoMesaji.PadRight(40));
            Console.ResetColor();
            Console.WriteLine();

            string durumMetni;
            ConsoleColor durumRenk;

            if (!sistemAcik) { durumMetni = "KAPALI (OFF)"; durumRenk = ConsoleColor.Red; }
            else if (uretimModu) { durumMetni = "URETIM (RUN)"; durumRenk = ConsoleColor.Green; }
            else { durumMetni = "PAUSE (IDLE)"; durumRenk = ConsoleColor.Yellow; }

            Console.Write("Dongu: {0}  |  Durum: ", donguSayisi.ToString().PadRight(5));
            Console.ForegroundColor = durumRenk;
            Console.WriteLine(durumMetni.PadRight(15));
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- SENSORLER (Giris) ---                ");
            Console.ResetColor();
            Console.WriteLine("[1000] Hammadde  : {0} birim             ", bellek.Oku(SENSOR_HAMMADDE));
            Console.WriteLine("[1004] Isci      : {0}                   ", bellek.Oku(SENSOR_ISCI) == 1 ? "VAR" : "YOK");
            Console.WriteLine("[1008] Sicaklik  : {0:F1} C              ", bellek.Oku(SENSOR_SICAKLIK));
            Console.WriteLine("[1012] Basinc    : {0:F0} Pa             ", bellek.Oku(SENSOR_BASINC));
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- AKTUATORLER (Cikis) ---              ");
            Console.ResetColor();
            string makineDurumu = bellek.Oku(ACTUATOR_MAKINE) == 1 ? "ACIK" : "KAPALI";
            Console.WriteLine("[2000] Makine    : {0}                   ", makineDurumu.PadRight(10));
            Console.WriteLine("[2004] Motor PWM : {0:F0}%                ", bellek.Oku(ACTUATOR_MOTOR));
            string alarmDurumu = bellek.Oku(ACTUATOR_ALARM) == 1 ? "ACIK" : "KAPALI";
            Console.WriteLine("[2008] Alarm     : {0}                   ", alarmDurumu.PadRight(10));
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- PID KONTROLOR ---                    ");
            Console.ResetColor();
            Console.WriteLine("Setpoint: {0} C                          ", pidKontrol.HedefDeger);
            Console.WriteLine("Hata    : {0:F2}                         ", pidKontrol.HedefDeger - bellek.Oku(SENSOR_SICAKLIK));
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- LADDER LOGIC ---                     ");
            Console.ResetColor();
            foreach (var basamak in ladderBasamaklari)
            {
                string aktiflikDurumu = basamak.Aktif ? "[AKTIF]" : "[PASIF]";
                string satir = string.Format("RUNG {0}: {1} -> {2} {3}", basamak.Numara, basamak.Kosul, basamak.Aksiyon, aktiflikDurumu);
                Console.WriteLine(satir.PadRight(80));
            }
            Console.WriteLine(new string(' ', 80));

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Komutlar: [S]tart [P]ause [R]eset [Q]uit");
            Console.ResetColor();
            Console.Write("Komut giriniz: ");
        }

        // ANA DONGU (MAIN LOOP)
        public void Calistir()
        {
            bool programCikis = false;
            Thread sistemIslemi = null;

            Console.CursorVisible = false;
            Console.Clear();

            while (!programCikis)
            {
                EkraniGoster();

                if (Console.KeyAvailable)
                {
                    var basilanTus = Console.ReadKey(true).Key;

                    switch (basilanTus)
                    {
                        case ConsoleKey.S:
                            // Eğer sistem kapalıysa (ilk açılış veya reset sonrası), Thread'i BAŞLAT.
                            if (!sistemAcik)
                            {
                                sistemAcik = true;
                                uretimModu = true;
                                senaryoMesaji = "NORMAL MOD (Stabil Calisma)       "; // DÜZELTME: Mesajı hemen güncelle

                                sistemIslemi = new Thread(() =>
                                {
                                    while (sistemAcik)
                                    {
                                        donguSayisi++;
                                        SensorSimulasyonu();
                                        LadderCalistir();
                                        PIDCalistir();
                                        Thread.Sleep(1000);
                                    }
                                });
                                sistemIslemi.Start();
                            }
                            else
                            {
                                // Pause'dan dönüyorsak mesajı ve modu güncelle
                                uretimModu = true;
                                senaryoMesaji = "NORMAL MOD (Stabil Calisma)       "; 
                            }
                            break;

                        case ConsoleKey.P:
                            // Sadece üretim modunu durdur, Thread (Elektrik) devam etsin
                            if (sistemAcik)
                            {
                                uretimModu = false;

                                bellek.Yaz(ACTUATOR_MAKINE, 0);
                                bellek.Yaz(ACTUATOR_MOTOR, 0);
                            }
                            break;

                        case ConsoleKey.R:
                            sistemAcik = false;
                            uretimModu = false;
                            donguSayisi = 0;
                            Sifirla();
                            Console.Clear();
                            break;

                        case ConsoleKey.Q:
                            sistemAcik = false;
                            programCikis = true;
                            break;
                    }
                }
                Thread.Sleep(100);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "PLC Simulator";
            PLCSistemi plcSistemi = new PLCSistemi();
            plcSistemi.Calistir();
        }
    }
}