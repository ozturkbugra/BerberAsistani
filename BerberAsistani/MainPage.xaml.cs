using BerberAsistani.Models;
using BerberAsistani.Services;
using BerberAsistani.ViewModels;

namespace BerberAsistani;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly RandevuService _service;

    public MainPage(RandevuService service)
    {
        InitializeComponent();
        _service = service;
        _viewModel = new MainViewModel(_service);
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ListeyiYukle();
    }

    // "+" Butonuna basınca çalışır: Paneli açar veya kapatır
    private void OnPanelAcKapatClicked(object sender, EventArgs e)
    {
        // Panel görünürse gizle, gizliyse göster (Tersi yap)
        PnlYeniKayit.IsVisible = !PnlYeniKayit.IsVisible;

        if (PnlYeniKayit.IsVisible)
        {
            // Panel açıldığında saati şimdiki saate ayarla ki kolaylık olsun
            TmrBaslangic.Time = DateTime.Now.TimeOfDay;
            EntAd.Focus(); // İmleci direkt isme odakla
        }
    }

    private void OnIptalClicked(object sender, EventArgs e)
    {
        FormuTemizleVeKapat();
    }

    private async void OnKaydetClicked(object sender, EventArgs e)
    {
        // 1. Basit Kontroller (Ad veya işlem seçili değilse uyar)
        if (string.IsNullOrWhiteSpace(EntAd.Text) || PickIslem.SelectedIndex == -1)
        {
            await DisplayAlert("Eksik Bilgi", "Lütfen müşteri adı ve yapılacak işlemi seçin.", "Tamam");
            return;
        }

        // 2. SAAT VE TARİH HESAPLAMA (Hatanın Çıktığı Yer Burasıydı)

        // TimePicker'dan saati alıyoruz (Örn: 14:30)
        // Eğer Time null gelirse (imkansız ama derleyici korkuyor) 00:00 al diyoruz.
        TimeSpan baslangicSaati = TmrBaslangic.Time ?? TimeSpan.Zero;


        // Picker'dan süreyi alıyoruz (Örn: 30 dk)
        int sure = (int)PickSure.SelectedItem;

        // ViewModel'deki tarihi garantiye alıyoruz. 
        // Derleyiciye diyoruz ki: "Korkma, bu tarih DateTime türünde, null değil."
        DateTime islemGunu = _viewModel.SecilenTarih;

        // Şimdi toplama işlemini yapıyoruz (Tarih + Saat)
        DateTime baslangicTarihi = islemGunu.Date + baslangicSaati;

        // Bitiş saatini hesaplıyoruz
        DateTime bitisTarihi = baslangicTarihi.AddMinutes(sure);

        // 3. ÇAKIŞMA KONTROLÜ
        bool cakismaVar = await _service.CakismaVarMi(baslangicTarihi, bitisTarihi);
        if (cakismaVar)
        {
            await DisplayAlert("Dolu!", "Seçtiğin saat aralığında koltuk dolu. Lütfen başka bir saat seç.", "Tamam");
            return;
        }

        // 4. KAYIT İŞLEMİ
        var yeni = new Randevu
        {
            AdSoyad = EntAd.Text,
            Islem = PickIslem.SelectedItem.ToString(),
            Baslangic = baslangicTarihi,
            Bitis = bitisTarihi
        };

        await _service.AddRandevu(yeni);

        // 5. TEMİZLİK
        _viewModel.ListeyiYukle(); // Listeyi yenile
        FormuTemizleVeKapat(); // Paneli kapat
    }

    private async void OnSilClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var randevu = btn.CommandParameter as Randevu;

        bool cevap = await DisplayAlert("Sil", $"{randevu.AdSoyad} silinsin mi?", "Evet", "Hayır");
        if (cevap)
        {
            await _service.DeleteRandevu(randevu);
            _viewModel.ListeyiYukle();
        }
    }

    private void FormuTemizleVeKapat()
    {
        EntAd.Text = string.Empty;
        PickIslem.SelectedIndex = -1;
        PnlYeniKayit.IsVisible = false; // Paneli gizle
    }
}